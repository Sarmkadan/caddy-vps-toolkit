#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CaddyVpsToolkit.Caching;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Caching
{
    public static class MemoryCacheTestsExtensions
    {
        private static readonly FieldInfo _cacheField = typeof(MemoryCacheTests)
            .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Failed to find _cache field in MemoryCacheTests");

        /// <summary>
        /// Creates a cache entry with the specified key and value, then immediately verifies it was stored correctly.
        /// </summary>
        /// <typeparam name="T">Type of the value to store</typeparam>
        /// <param name="tests">The test instance containing the cache</param>
        /// <param name="key">The cache key</param>
        /// <param name="value">The value to store</param>
        /// <returns>The stored value for fluent assertions</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> or <paramref name="key"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is empty or whitespace</exception>
        public static async Task<T> SetAndVerifyAsync<T>(this MemoryCacheTests tests, string key, T value)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentNullException.ThrowIfNull(key);

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Cache key cannot be empty or whitespace.", nameof(key));
            }

            var cache = GetCache(tests);
            await cache.SetAsync(key, value);

            var retrieved = await cache.GetAsync<T>(key);
            retrieved.Should().Be(value, "Value should be retrievable immediately after storage");

            return value;
        }

        /// <summary>
        /// Verifies that a key exists in the cache and returns the cached value.
        /// </summary>
        /// <typeparam name="T">Type of the expected value</typeparam>
        /// <param name="tests">Test instance</param>
        /// <param name="key">The cache key to check</param>
        /// <returns>The cached value</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> or <paramref name="key"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is empty or whitespace</exception>
        /// <exception cref="Xunit.Sdk.XunitException">Key does not exist in cache</exception>
        public static async Task<T> GetAndVerifyAsync<T>(this MemoryCacheTests tests, string key)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentNullException.ThrowIfNull(key);

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Cache key cannot be empty or whitespace.", nameof(key));
            }

            var cache = GetCache(tests);
            var value = await cache.GetAsync<T>(key);
            value.Should().NotBeNull($"Expected key '{key}' to exist in cache");

            return value!;
        }

        /// <summary>
        /// Verifies that multiple keys exist in the cache and returns their values.
        /// </summary>
        /// <typeparam name="T">Type of the values</typeparam>
        /// <param name="tests">Test instance</param>
        /// <param name="keys">Collection of cache keys to verify</param>
        /// <returns>Dictionary mapping keys to their cached values</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> or <paramref name="keys"/> is null</exception>
        public static async Task<Dictionary<string, T>> GetMultipleAsync<T>(this MemoryCacheTests tests, IEnumerable<string> keys)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentNullException.ThrowIfNull(keys);

            var cache = GetCache(tests);
            var result = new Dictionary<string, T>(StringComparer.Ordinal);

            foreach (var key in keys)
            {
                ArgumentNullException.ThrowIfNull(key);

                var value = await cache.GetAsync<T>(key);
                if (value is not null)
                {
                    result[key] = value;
                }
            }

            return result;
        }

        /// <summary>
        /// Verifies that a cache entry with expiration expires at the expected time.
        /// </summary>
        /// <param name="tests">Test instance</param>
        /// <param name="key">The cache key</param>
        /// <param name="initialDelayMs">Initial delay before checking expiration</param>
        /// <param name="expectedRemainingMs">Expected remaining time before expiration (with tolerance)</param>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is empty or whitespace</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialDelayMs"/> or <paramref name="expectedRemainingMs"/> is negative</exception>
        public static async Task VerifyExpirationAsync(this MemoryCacheTests tests, string key, int initialDelayMs, int expectedRemainingMs)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentNullException.ThrowIfNull(key);

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Cache key cannot be empty or whitespace.", nameof(key));
            }

            if (initialDelayMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialDelayMs), "Delay cannot be negative");
            }

            if (expectedRemainingMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedRemainingMs), "Expected remaining time cannot be negative");
            }

            var cache = GetCache(tests);

            // Set with expiration
            await cache.SetAsync(key, "expiring-value", TimeSpan.FromMilliseconds(expectedRemainingMs + 50));

            // Wait for initial delay
            await Task.Delay(initialDelayMs);

            // Verify it's still accessible
            var value = await cache.GetAsync<string>(key);
            value.Should().NotBeNull("Value should still be accessible before expiration");

            // Wait for expiration
            await Task.Delay(expectedRemainingMs + 100);

            // Verify it's expired
            value = await cache.GetAsync<string>(key);
            value.Should().BeNull("Value should be expired after the expected time");
        }

        /// <summary>
        /// Creates a cache key using the same logic as the cache implementation.
        /// </summary>
        /// <param name="tests">Test instance</param>
        /// <param name="parts">Key parts to join</param>
        /// <returns>Joined cache key</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> or <paramref name="parts"/> is null</exception>
        public static string CreateCacheKey(this MemoryCacheTests tests, params string[] parts)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentNullException.ThrowIfNull(parts);

            return CacheExtensions.MakeCacheKey(parts);
        }

        /// <summary>
        /// Verifies that all entries are removed from the cache.
        /// </summary>
        /// <param name="tests">Test instance</param>
        /// <param name="expectedCount">Expected number of entries that should be removed</param>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> is null</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="expectedCount"/> is negative</exception>
        public static async Task VerifyClearAsync(this MemoryCacheTests tests, int expectedCount = 0)
        {
            ArgumentNullException.ThrowIfNull(tests);

            if (expectedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedCount), "Count cannot be negative");
            }

            var cache = GetCache(tests);

            // Store expected number of entries
            for (int i = 0; i < expectedCount; i++)
            {
                await cache.SetAsync($"test-key-{i}", i);
            }

            var sizeBefore = cache.GetCacheSize();
            sizeBefore.Should().Be(expectedCount, $"Should have {expectedCount} entries before clear");

            await cache.ClearAsync();

            var sizeAfter = cache.GetCacheSize();
            sizeAfter.Should().Be(0, "Cache should be empty after ClearAsync");
        }

        private static MemoryCache GetCache(MemoryCacheTests tests)
        {
            return (_cacheField.GetValue(tests) as MemoryCache)
                ?? throw new InvalidOperationException("Failed to retrieve cache instance from test");
        }
    }
}