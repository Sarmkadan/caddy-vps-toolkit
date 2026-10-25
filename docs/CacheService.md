# CacheService

`Caching/CacheService.cs` provides the in-memory cache used by the Caddy VPS Toolkit. It defines the asynchronous `ICacheService` abstraction, the `MemoryCache` implementation with per-entry expiration, and the `CacheExtensions` helpers for get-or-create workflows and cache key construction. The implementation is intended for single-server deployments. For a multi-server setup, implement `ICacheService` on top of a distributed store such as Redis.

The file lives in the `CaddyVpsToolkit.Caching` namespace and contains three public types:

- `ICacheService` is the cache abstraction. All members are asynchronous and return `ValueTask` or `ValueTask<T>`.
- `MemoryCache` is a `sealed` implementation of `ICacheService` backed by a `ConcurrentDictionary`.
- `CacheExtensions` is a `static` class with the extension methods `GetOrSetAsync`, `GetOrCreateAsync`, and the helper `MakeCacheKey`.

## API

### `ICacheService`

#### `ValueTask<T> GetAsync<T>(string key)`

Returns the value stored under `key`, cast to `T`.

- **Parameters**
  - `key`: Cache key.
- **Return value**: The cached value, or `default(T)` when the key is missing, expired, null or empty, or holds a value that is not of type `T`.
- **Exceptions**: None for the key. `MemoryCache` returns `default(T)` for a null or empty key instead of throwing.

#### `ValueTask<(bool Found, T Value)> TryGetAsync<T>(string key)`

Returns whether a value was found together with the value itself. Use it when `default(T)` is a valid cached value, for example `0`, `false`, or `null`.

- **Parameters**
  - `key`: Cache key.
- **Return value**: `Found` is `true` when a non-expired entry of type `T` exists. `Value` is that entry, or `default(T)` otherwise.
- **Exceptions**: Throws `ArgumentNullException` if `key` is `null`, and `ArgumentException` if it is empty.

#### `ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null)`

Stores `value` under `key`, replacing any existing entry without warning.

- **Parameters**
  - `key`: Cache key.
  - `value`: Value to store. It may be `null`.
  - `expiration`: Lifetime relative to the current UTC time. `null` (the default) means the entry never expires.
- **Return value**: A completed `ValueTask`.
- **Exceptions**: None. A null or empty `key` is silently ignored.

#### `ValueTask RemoveAsync(string key)`

Removes the entry for `key`. Does nothing if the key does not exist.

- **Parameters**
  - `key`: Cache key.
- **Return value**: A completed `ValueTask`.
- **Exceptions**: Throws `ArgumentNullException` if `key` is `null`, and `ArgumentException` if it is empty.

#### `ValueTask ClearAsync()`

Removes every entry in the cache, including entries that have expired but have not yet been cleaned up.

- **Return value**: A completed `ValueTask`.
- **Exceptions**: None.

#### `ValueTask<bool> ExistsAsync(string key)`

Checks whether a non-expired entry exists for `key`. The check does not depend on the type of the stored value.

- **Parameters**
  - `key`: Cache key.
- **Return value**: `true` if a non-expired entry exists, otherwise `false`.
- **Exceptions**: Throws `ArgumentNullException` if `key` is `null`, and `ArgumentException` if it is empty.

### `MemoryCache`

#### `MemoryCache()`

Creates an empty cache. The class has no constructor parameters, and no options are available. Each instance has its own storage, so create one instance and share it across consumers that need the same data.

#### `ValueTask<T> GetAsync<T>(string key)`, `ValueTask<(bool Found, T Value)> TryGetAsync<T>(string key)`, `ValueTask SetAsync<T>(...)`, `ValueTask RemoveAsync(string key)`, `ValueTask ClearAsync()`, `ValueTask<bool> ExistsAsync(string key)`

These implement the `ICacheService` members described above, with the behaviour noted for each. Expired entries are removed lazily: `TryGetAsync` removes an expired entry when it reads it.

#### `void CleanExpiredEntries()`

Scans the whole cache synchronously and removes every entry whose expiration has passed.

- **Return value**: None.
- **Exceptions**: None.

The cache also runs this cleanup on its own every 128 operations. The operations that count are `GetAsync`, `TryGetAsync`, `SetAsync`, and `ExistsAsync`. Call `CleanExpiredEntries` directly only when you need to reclaim memory at a specific moment.

#### `int GetCacheSize()`

Returns the number of entries in the cache.

- **Return value**: The entry count. This includes expired entries that have not been removed yet, so it can be higher than the number of live entries.
- **Exceptions**: None.

### `CacheExtensions`

#### `static MakeCacheKey(params string[] parts)`

Builds a cache key by joining `parts` with `:`.

- **Parameters**
  - `parts`: Key segments. Passing no segments returns an empty string.
- **Return value**: The joined key, for example `MakeCacheKey("service", "health", "123")` returns `"service:health:123"`.
- **Exceptions**: Throws `ArgumentNullException` if `parts` is `null`.

The join does not escape the separator. `("a:b", "c")` and `("a", "b:c")` produce the same key, so make sure your segments cannot contain `:` or that collisions do not matter for your data.

#### `static ValueTask<T> GetOrSetAsync<T>(this ICacheService cache, string key, Func<Task<T>> factory, TimeSpan? expiration = null)`

Returns the cached value for `key`. If there is no value, it runs `factory`, stores the result with `expiration`, and returns it.

- **Parameters**
  - `cache`: Cache to read from and write to.
  - `key`: Cache key. Must not be null or empty.
  - `factory`: Delegate that produces the value. It runs only on a miss.
  - `expiration`: Lifetime of the stored value. `null` means the value never expires.
- **Return value**: The cached value or the value just produced by `factory`.
- **Exceptions**: Throws `ArgumentNullException` if `cache` or `factory` is `null`, and `ArgumentException` if `key` is empty. Exceptions thrown by `factory` propagate, and nothing is stored.

This method does **not** lock. Concurrent callers that miss on the same key can each run `factory`, and the last write wins. Use `GetOrCreateAsync` when the factory must run only once.

#### `static ValueTask<T> GetOrCreateAsync<T>(this ICacheService cache, string key, Func<Task<T>> factory, TimeSpan? expiration = null)`

Works like `GetOrSetAsync`, but guarantees that `factory` runs at most once at a time for a given key. Callers that miss while the factory is running wait for it and then receive the same stored value.

- **Parameters**: The same as `GetOrSetAsync`.
- **Return value**: The cached value or the value produced by `factory`.
- **Exceptions**: The same as `GetOrSetAsync`. If `factory` throws, the waiting callers are released and the next caller runs the factory again.

The method first checks the cache without locking. On a miss, it takes a per-key lock, checks the cache a second time, and only then runs `factory`.

## Behaviour Notes

- **Expiration is lazy.** An expired entry is removed when it is read, when `CleanExpiredEntries` runs, or during the periodic cleanup. Until then it still counts toward `GetCacheSize`. `ExistsAsync` and `GetAsync` treat expired entries as absent.
- **Zero or negative expiration.** A `TimeSpan` of zero or less makes the entry expire immediately, so it is not returned by the next read.
- **Null values are cached.** A `null` stored for a reference type is a hit. `GetAsync` and `TryGetAsync` return it as `null` with `Found == true`.
- **Type mismatch is a miss.** If the stored value is not of type `T`, `GetAsync` returns `default(T)` and `TryGetAsync` returns `Found == false`. Nothing is thrown, so a mismatched read looks the same as a missing key.
- **Null and empty keys differ by method.** `SetAsync` ignores them silently, and `GetAsync` returns `default(T)`. `TryGetAsync`, `ExistsAsync`, and `RemoveAsync` throw.
- **Per-key lock is process-wide.** `GetOrCreateAsync` stores its per-key locks in a `static` dictionary keyed only by the key string. Two `MemoryCache` instances in the same process that use the same key share one lock, so their factories run one at a time. The lock is not shared across processes.
- **Factory exceptions are not cached.** Neither extension method stores anything when `factory` throws.
- **Name clash.** `MemoryCache` has the same name as `System.Runtime.Caching.MemoryCache`. If a file imports both namespaces, qualify the type name.

## Usage

### Example 1: Caching a lookup with an expiration

```csharp
using CaddyVpsToolkit.Caching;

ICacheService cache = new MemoryCache();

string key = CacheExtensions.MakeCacheKey("upstream", "health", upstreamId);

HealthStatus status = await cache.GetOrCreateAsync(
    key,
    async () => await _healthProbe.CheckAsync(upstreamId),
    TimeSpan.FromSeconds(30));

// status is either the cached result from the last 30 seconds or a fresh probe.
```

`GetOrCreateAsync` is an extension method, so call it on an `ICacheService` instance. Use `GetOrSetAsync` instead when a duplicate call to the factory is acceptable and you want to avoid the lock.

### Example 2: Distinguishing a cached default from a miss

```csharp
// A cached 0 is a valid hit, so check Found instead of comparing against default.
var (found, retries) = await cache.TryGetAsync<int>("retry:count");

if (!found)
{
    retries = 3; // Not cached yet: start from the configured default.
    await cache.SetAsync("retry:count", retries, TimeSpan.FromMinutes(5));
}
```

### Example 3: Manual lifecycle with explicit eviction

```csharp
string sessionKey = CacheExtensions.MakeCacheKey("session", sessionId);

if (await cache.ExistsAsync(sessionKey))
{
    var session = await cache.GetAsync<SessionData>(sessionKey);
    // Use session data.
}
else
{
    // Session expired or never existed. Redirect to login.
}

// Log out: remove the entry immediately instead of waiting for it to expire.
await cache.RemoveAsync(sessionKey);
```

### Example 4: Reclaiming memory on a schedule

```csharp
// Run from a background timer. The cache also cleans up on its own,
// so this is only needed when you want a specific reclaim point.
var memoryCache = new MemoryCache();

memoryCache.CleanExpiredEntries();
int remaining = memoryCache.GetCacheSize();
```

`CleanExpiredEntries` is synchronous and scans every entry, so run it off the hot path.

### Example 5: Building compound keys

```csharp
string key = CacheExtensions.MakeCacheKey("pool", poolId, "server", serverId);
// "pool:<poolId>:server:<serverId>"

await cache.SetAsync(key, server, expiration: null); // Never expires; remove it explicitly.
await cache.RemoveAsync(key);
```
