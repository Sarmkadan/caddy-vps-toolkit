#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CaddyVpsToolkit.Caching
{
    /// <summary>
    /// Cache service interface for abstraction
    /// </summary>
    public interface ICacheService
    {
        ValueTask<T> GetAsync<T>(string key);
        ValueTask<(bool Found, T Value)> TryGetAsync<T>(string key);
        ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        ValueTask RemoveAsync(string key);
        ValueTask ClearAsync();
        ValueTask<bool> ExistsAsync(string key);
    }

    /// <summary>
    /// In-memory cache implementation with expiration support.
    /// Thread-safe implementation suitable for single-server deployments.
    /// For distributed systems, replace with Redis or similar.
    /// </summary>
    public sealed class MemoryCache : ICacheService
    {
        private sealed class CacheEntry
        {
            public object Value { get; init; }
            public DateTime? ExpiresAt { get; init; }
        }

        // ConcurrentDictionary eliminates the explicit lock; individual bucket-level
        // locking gives better throughput under concurrent reads than a single lock.
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

        public ValueTask<T> GetAsync<T>(string key)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            // Ensure expired entries are purged before attempting to read.
            CleanExpiredEntries();

            var (found, value) = TryGet<T>(key);
            return ValueTask.FromResult(found ? value : default);
        }

        public ValueTask<(bool Found, T Value)> TryGetAsync<T>(string key)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            // Ensure expired entries are purged before attempting to read.
            CleanExpiredEntries();

            return ValueTask.FromResult(TryGet<T>(key));
        }

        private (bool Found, T Value) TryGet<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return (false, default);

            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt.HasValue && DateTime.UtcNow > entry.ExpiresAt)
                {
                    _cache.TryRemove(key, out _);
                    return (false, default);
                }

                // Type-safe unwrap: a mismatched type behaves like a miss instead of
                // throwing InvalidCastException at the call site.
                if (entry.Value is T typed)
                    return (true, typed);

                if (entry.Value is null && default(T) is null)
                    return (true, default);

                return (false, default);
            }

            return (false, default);
        }

        public ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            if (string.IsNullOrEmpty(key))
                return ValueTask.CompletedTask;

            _cache[key] = new CacheEntry
            {
                Value = value,
                ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null,
            };

            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveAsync(string key)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            if (!string.IsNullOrEmpty(key))
                _cache.TryRemove(key, out _);

            return ValueTask.CompletedTask;
        }

        public ValueTask ClearAsync()
        {
            _cache.Clear();
            return ValueTask.CompletedTask;
        }

        public async ValueTask<bool> ExistsAsync(string key)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            // Ensure expired entries are purged before checking existence.
            CleanExpiredEntries();

            var (found, _) = await TryGetAsync<object>(key);
            return found;
        }

        /// <summary>
        /// Remove expired entries to prevent memory bloat
        /// </summary>
        public void CleanExpiredEntries()
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _cache)
            {
                if (kvp.Value.ExpiresAt.HasValue && now > kvp.Value.ExpiresAt)
                    _cache.TryRemove(kvp.Key, out _);
            }
        }

        public int GetCacheSize() => _cache.Count;
    }

    /// <summary>
    /// Extension methods for cache operations
    /// </summary>
    public static class CacheExtensions
    {
        // Holds a per‑key semaphore to guarantee that only one factory execution
        // runs for a given cache key at a time.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

        /// <summary>
        /// Get or set cache value using factory function.
        /// This version does **not** provide any locking – concurrent calls may
        /// invoke the factory multiple times.
        /// </summary>
        public static async ValueTask<T> GetOrSetAsync<T>(
            this ICacheService cache,
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiration = null)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentException.ThrowIfNullOrEmpty(key);
            ArgumentNullException.ThrowIfNull(factory);

            // TryGetAsync distinguishes a genuine miss from a cached default value,
            // which matters for value types (a cached 0/false is a valid hit) and
            // avoids re-invoking the factory on every call for missing value types.
            var (found, cached) = await cache.TryGetAsync<T>(key);
            if (found)
                return cached;

            var value = await factory();
            await cache.SetAsync(key, value, expiration);
            return value;
        }

        /// <summary>
        /// Get or create cache value using factory function with per‑key locking.
        /// Guarantees that the <paramref name="factory"/> is executed at most once
        /// concurrently for the same <paramref name="key"/>. Subsequent callers
        /// will await the first execution and receive the same result.
        /// </summary>
        public static async ValueTask<T> GetOrCreateAsync<T>(
            this ICacheService cache,
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiration = null)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentException.ThrowIfNullOrEmpty(key);
            ArgumentNullException.ThrowIfNull(factory);

            // Fast path – try to get the value without taking a lock.
            var (found, cached) = await cache.TryGetAsync<T>(key);
            if (found)
                return cached;

            // Acquire a semaphore specific to the key.
            var semaphore = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                // Re‑check after acquiring the lock – another thread may have
                // populated the cache while we were waiting.
                var (foundAfterLock, cachedAfterLock) = await cache.TryGetAsync<T>(key);
                if (foundAfterLock)
                    return cachedAfterLock;

                // Execute the factory, store the result and return it.
                var value = await factory();
                await cache.SetAsync(key, value, expiration);
                return value;
            }
            finally
            {
                semaphore.Release();

                // Optional cleanup: remove the semaphore when no one is waiting.
                // This prevents unbounded growth of the dictionary.
                if (semaphore.CurrentCount == 1)
                {
                    _keyLocks.TryRemove(key, out _);
                }
            }
        }

        /// <summary>
        /// Create cache key from multiple parts
        /// </summary>
        public static string MakeCacheKey(params string[] parts)
        {
            ArgumentNullException.ThrowIfNull(parts);

            return string.Join(":", parts);
        }
    }
}
