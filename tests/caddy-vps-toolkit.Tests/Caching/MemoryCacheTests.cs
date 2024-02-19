#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Caching;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Caching
{
    public sealed class MemoryCacheTests
    {
        private readonly MemoryCache _cache = new();

        [Fact]
        public async Task SetAsync_ThenGetAsync_ReturnsStoredValue()
        {
            await _cache.SetAsync("key1", "hello");

            var result = await _cache.GetAsync<string>("key1");

            result.Should().Be("hello");
        }

        [Fact]
        public async Task GetAsync_MissingKey_ReturnsDefault()
        {
            var result = await _cache.GetAsync<int>("missing");

            result.Should().Be(default(int));
        }

        [Fact]
        public async Task GetAsync_EmptyKey_ReturnsDefault()
        {
            var result = await _cache.GetAsync<string>(string.Empty);

            result.Should().BeNull();
        }

        [Fact]
        public async Task SetAsync_EmptyKey_DoesNotStore()
        {
            await _cache.SetAsync(string.Empty, "value");

            _cache.GetCacheSize().Should().Be(0);
        }

        [Fact]
        public async Task ExistsAsync_ExistingKey_ReturnsTrue()
        {
            await _cache.SetAsync("exists-key", 42);

            var exists = await _cache.ExistsAsync("exists-key");

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_MissingKey_ReturnsFalse()
        {
            var exists = await _cache.ExistsAsync("no-such-key");

            exists.Should().BeFalse();
        }

        [Fact]
        public async Task RemoveAsync_ExistingKey_RemovesEntry()
        {
            await _cache.SetAsync("to-remove", "value");

            await _cache.RemoveAsync("to-remove");

            var result = await _cache.GetAsync<string>("to-remove");
            result.Should().BeNull();
        }

        [Fact]
        public async Task RemoveAsync_MissingKey_DoesNotThrow()
        {
            Func<Task> act = () => _cache.RemoveAsync("ghost").AsTask();

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ClearAsync_RemovesAllEntries()
        {
            await _cache.SetAsync("a", 1);
            await _cache.SetAsync("b", 2);
            await _cache.SetAsync("c", 3);

            await _cache.ClearAsync();

            _cache.GetCacheSize().Should().Be(0);
        }

        [Fact]
        public async Task SetAsync_WithExpiration_EntryExpires()
        {
            await _cache.SetAsync("expiring", "val", TimeSpan.FromMilliseconds(50));

            await Task.Delay(100);

            var result = await _cache.GetAsync<string>("expiring");
            result.Should().BeNull();
        }

        [Fact]
        public async Task SetAsync_WithFutureExpiration_EntryIsStillAccessible()
        {
            await _cache.SetAsync("fresh", "fresh-val", TimeSpan.FromMinutes(10));

            var result = await _cache.GetAsync<string>("fresh");

            result.Should().Be("fresh-val");
        }

        [Fact]
        public async Task CleanExpiredEntries_RemovesOnlyExpired()
        {
            await _cache.SetAsync("stale", "s", TimeSpan.FromMilliseconds(10));
            await _cache.SetAsync("fresh", "f", TimeSpan.FromMinutes(5));

            await Task.Delay(50);
            _cache.CleanExpiredEntries();

            _cache.GetCacheSize().Should().Be(1);
            var fresh = await _cache.GetAsync<string>("fresh");
            fresh.Should().Be("f");
        }

        [Fact]
        public async Task SetAsync_OverwritesExistingKey()
        {
            await _cache.SetAsync("k", "old");
            await _cache.SetAsync("k", "new");

            var result = await _cache.GetAsync<string>("k");

            result.Should().Be("new");
        }
    }

    public sealed class CacheExtensionsTests
    {
        [Fact]
        public async Task GetOrSetAsync_MissingKey_CallsFactoryAndStores()
        {
            var cache = new MemoryCache();
            int factoryCalls = 0;

            var result = await cache.GetOrSetAsync("key", async () =>
            {
                factoryCalls++;
                await Task.CompletedTask;
                return "computed";
            });

            result.Should().Be("computed");
            factoryCalls.Should().Be(1);
        }

        [Fact]
        public async Task GetOrSetAsync_ExistingKey_DoesNotCallFactory()
        {
            var cache = new MemoryCache();
            await cache.SetAsync("key", "cached");
            int factoryCalls = 0;

            var result = await cache.GetOrSetAsync("key", async () =>
            {
                factoryCalls++;
                await Task.CompletedTask;
                return "new";
            });

            result.Should().Be("cached");
            factoryCalls.Should().Be(0);
        }

        [Fact]
        public void MakeCacheKey_Multipleparts_JoinsWithColon()
        {
            var key = CacheExtensions.MakeCacheKey("service", "health", "123");

            key.Should().Be("service:health:123");
        }

        [Fact]
        public void MakeCacheKey_SinglePart_ReturnsPart()
        {
            var key = CacheExtensions.MakeCacheKey("only");

            key.Should().Be("only");
        }
    }
}
