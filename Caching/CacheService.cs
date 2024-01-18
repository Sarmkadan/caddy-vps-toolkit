// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaddyVpsToolkit.Caching
{
    /// <summary>
    /// Cache service interface for abstraction
    /// </summary>
    public interface ICacheService
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task ClearAsync();
        Task<bool> ExistsAsync(string key);
    }

    /// <summary>
    /// In-memory cache implementation with expiration support.
    /// Thread-safe implementation suitable for single-server deployments.
    /// For distributed systems, replace with Redis or similar.
    /// </summary>
    public class MemoryCache : ICacheService
    {
        private class CacheEntry
        {
            public object Value { get; set; }
            public DateTime? ExpiresAt { get; set; }
        }

        private readonly Dictionary<string, CacheEntry> _cache = new();
        private readonly object _lockObject = new();

        public async Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default;

            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    // Check expiration
                    if (entry.ExpiresAt.HasValue && DateTime.UtcNow > entry.ExpiresAt)
                    {
                        _cache.Remove(key);
                        return default;
                    }

                    return (T)entry.Value;
                }

                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key))
                return;

            lock (_lockObject)
            {
                _cache[key] = new CacheEntry
                {
                    Value = value,
                    ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : (DateTime?)null
                };
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            lock (_lockObject)
            {
                _cache.Remove(key);
            }
        }

        public async Task ClearAsync()
        {
            lock (_lockObject)
            {
                _cache.Clear();
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await GetAsync<object>(key) != null;
        }

        /// <summary>
        /// Remove expired entries to prevent memory bloat
        /// </summary>
        public void CleanExpiredEntries()
        {
            lock (_lockObject)
            {
                var keysToRemove = new List<string>();
                var now = DateTime.UtcNow;

                foreach (var kvp in _cache)
                {
                    if (kvp.Value.ExpiresAt.HasValue && now > kvp.Value.ExpiresAt)
                        keysToRemove.Add(kvp.Key);
                }

                foreach (var key in keysToRemove)
                    _cache.Remove(key);
            }
        }

        public int GetCacheSize()
        {
            lock (_lockObject)
            {
                return _cache.Count;
            }
        }
    }

    /// <summary>
    /// Extension methods for cache operations
    /// </summary>
    public static class CacheExtensions
    {
        /// <summary>
        /// Get or set cache value using factory function
        /// </summary>
        public static async Task<T> GetOrSetAsync<T>(
            this ICacheService cache,
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiration = null)
        {
            var cached = await cache.GetAsync<T>(key);
            if (cached != null)
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
