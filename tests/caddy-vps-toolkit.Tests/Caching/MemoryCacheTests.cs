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

/// <summary>
/// Tests for the MemoryCache class.
/// </summary>
public sealed class MemoryCacheTests
{
    private readonly MemoryCache _cache = new();

    /// <summary>
    /// Tests that setting a value and then getting it returns the stored value.
    /// </summary>
    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsStoredValue()
    {
        await _cache.SetAsync("key1", "hello");

        var result = await _cache.GetAsync<string>("key1");

        result.Should().Be("hello");
    }

    /// <summary>
    /// Tests that getting a missing key returns the default value.
    /// </summary>
    [Fact]
    public async Task GetAsync_MissingKey_ReturnsDefault()
    {
        var result = await _cache.GetAsync<int>("missing");

        result.Should().Be(default(int));
    }

    /// <summary>
    /// Tests that getting an empty key returns the default value.
    /// </summary>
    [Fact]
    public async Task GetAsync_EmptyKey_ReturnsDefault()
    {
        var result = await _cache.GetAsync<string>(string.Empty);

        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that setting an empty key does not store the value.
    /// </summary>
    [Fact]
    public async Task SetAsync_EmptyKey_DoesNotStore()
    {
        await _cache.SetAsync(string.Empty, "value");

        _cache.GetCacheSize().Should().Be(0);
    }

    /// <summary>
    /// Tests that checking for an existing key returns true.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_ExistingKey_ReturnsTrue()
    {
        await _cache.SetAsync("exists-key", 42);

        var exists = await _cache.ExistsAsync("exists-key");

        exists.Should().BeTrue();
    }

    /// <summary>
    /// Tests that checking for a missing key returns false.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_MissingKey_ReturnsFalse()
    {
        var exists = await _cache.ExistsAsync("no-such-key");

        exists.Should().BeFalse();
    }

    /// <summary>
    /// Tests that removing an existing key removes the entry.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_ExistingKey_RemovesEntry()
    {
        await _cache.SetAsync("to-remove", "value");

        await _cache.RemoveAsync("to-remove");

        var result = await _cache.GetAsync<string>("to-remove");
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that removing a missing key does not throw an exception.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_MissingKey_DoesNotThrow()
    {
        Func<Task> act = () => _cache.RemoveAsync("ghost").AsTask();

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Tests that clearing the cache removes all entries.
    /// </summary>
    [Fact]
    public async Task ClearAsync_RemovesAllEntries()
    {
        await _cache.SetAsync("a", 1);
        await _cache.SetAsync("b", 2);
        await _cache.SetAsync("c", 3);

        await _cache.ClearAsync();

        _cache.GetCacheSize().Should().Be(0);
    }

    /// <summary>
    /// Tests that setting a value with expiration and then waiting for it to expire returns null.
    /// </summary>
    [Fact]
    public async Task SetAsync_WithExpiration_EntryExpires()
    {
        await _cache.SetAsync("expiring", "val", TimeSpan.FromMilliseconds(50));

        await Task.Delay(100);

        var result = await _cache.GetAsync<string>("expiring");
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that setting a value with a future expiration and then getting it returns the value.
    /// </summary>
    [Fact]
    public async Task SetAsync_WithFutureExpiration_EntryIsStillAccessible()
    {
        await _cache.SetAsync("fresh", "fresh-val", TimeSpan.FromMinutes(10));

        var result = await _cache.GetAsync<string>("fresh");

        result.Should().Be("fresh-val");
    }

    /// <summary>
    /// Tests that cleaning expired entries removes only expired entries.
    /// </summary>
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

    /// <summary>
    /// Tests that overwriting an existing key with a new value stores the new value.
    /// </summary>
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
    /// <summary>
    /// Tests the GetOrSetAsync method when the key is missing.
    /// </summary>
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

    /// <summary>
    /// Tests the GetOrSetAsync method when the key is existing.
    /// </summary>
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

    /// <summary>
    /// Tests the MakeCacheKey method with multiple parts.
    /// </summary>
    [Fact]
    public void MakeCacheKey_Multipleparts_JoinsWithColon()
    {
        var key = CacheExtensions.MakeCacheKey("service", "health", "123");

        key.Should().Be("service:health:123");
    }

    /// <summary>
    /// Tests the MakeCacheKey method with a single part.
    /// </summary>
    [Fact]
    public void MakeCacheKey_SinglePart_ReturnsPart()
    {
        var key = CacheExtensions.MakeCacheKey("only");

        key.Should().Be("only");
    }
}
