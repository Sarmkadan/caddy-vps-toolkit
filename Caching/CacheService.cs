#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            var (found, value) = TryGet<T>(key);
            return ValueTask.FromResult(found ? value : default);
        }

        public ValueTask<(bool Found, T Value)> TryGetAsync<T>(string key)
        {
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
        /// <summary>
        /// Get or set cache value using factory function
        /// </summary>
        public static async ValueTask<T> GetOrSetAsync<T>(
            this ICacheService cache,
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiration = null)
        {
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
        /// Create cache key from multiple parts
        /// </summary>
        public static string MakeCacheKey(params string[] parts)
        {
            return string.Join(":", parts);
        }
    }
}
