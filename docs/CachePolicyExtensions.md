# CachePolicyExtensions

`Caching/CacheService.cs` declares the public extension methods for the cache abstraction, but they are not in a file called `CachePolicyExtensions.cs`. They are the members of the static class `CacheExtensions` in the `CaddyVpsToolkit.Caching` namespace, next to `ICacheService` and `MemoryCache`. This page covers those methods from the point of view of caching policy: which method to choose, how expiration is set, and how keys are built.

The class defines three public methods:

| Method | Extends | Purpose |
|--------|---------|---------|
| `GetOrSetAsync` | `ICacheService` | Returns a cached value or runs a factory and stores the result. No per-key lock. |
| `GetOrCreateAsync` | `ICacheService` | Same as `GetOrSetAsync`, but runs the factory at most once at a time per key. |
| `MakeCacheKey` | none (static) | Joins key segments with `:`. |

The class also holds a private, process-wide dictionary of per-key semaphores that `GetOrCreateAsync` uses. Callers do not interact with it directly.

For the full reference of `ICacheService`, `MemoryCache`, and the method signatures, see [CacheService](CacheService.md). This page does not repeat that reference. It explains how to use the extensions correctly.

## Choosing between the two get-or-create methods

| Situation | Use |
|--------|-----|
| The factory is cheap, or running it twice is harmless | `GetOrSetAsync` |
| The factory is expensive or has side effects, such as a network probe or a certificate lookup | `GetOrCreateAsync` |
| Callers must never see a partly built value | `GetOrCreateAsync` |

`GetOrSetAsync` does not lock. If several callers miss on the same key at once, each one runs the factory and the last write wins. `GetOrCreateAsync` takes a per-key semaphore after the first cache check fails. The waiting callers then read the value the first caller stored.

Both methods use the same signature:

```csharp
public static ValueTask<T> GetOrSetAsync<T>(
    this ICacheService cache,
    string key,
    Func<Task<T>> factory,
    TimeSpan? expiration = null)

public static ValueTask<T> GetOrCreateAsync<T>(
    this ICacheService cache,
    string key,
    Func<Task<T>> factory,
    TimeSpan? expiration = null)
```

The factory is a `Func<Task<T>>`, not a `Func<T>`. A synchronous value must be wrapped with `Task.FromResult`.

## API

### `GetOrSetAsync`

Returns the value stored under `key`. On a miss, it awaits `factory`, stores the result with `expiration`, and returns it.

- **Parameters**
  - `cache`: The cache to read from and write to. Must not be `null`.
  - `key`: The cache key. Must not be `null` or empty.
  - `factory`: Produces the value. It runs only on a miss. Must not be `null`.
  - `expiration`: Lifetime of the stored value, relative to the current UTC time. `null` (the default) means the value never expires.
- **Return value**: The cached value, or the value just produced by `factory`.
- **Exceptions**
  - `ArgumentNullException` if `cache` or `factory` is `null`.
  - `ArgumentException` if `key` is `null` or empty.
  - Any exception thrown by `factory` propagates to the caller, and nothing is stored.

The method does not lock. Use it when a duplicate factory call is acceptable.

### `GetOrCreateAsync`

Works like `GetOrSetAsync`, but guarantees that `factory` runs at most once at a time for a given `key`. The method reads the cache once without locking. On a miss, it acquires the per-key semaphore, reads the cache again, and runs `factory` only if the second read also misses. Callers that were waiting on the semaphore receive the value that the first caller stored.

- **Parameters**: The same as `GetOrSetAsync`.
- **Return value**: The cached value, or the value produced by `factory`.
- **Exceptions**: The same as `GetOrSetAsync`. The semaphore is released in a `finally` block, so a factory that throws does not leave the key locked. The next caller runs the factory again.

The wait for the semaphore is not cancellable. A slow factory therefore delays every caller that is waiting on the same key.

### `MakeCacheKey`

```csharp
public static string MakeCacheKey(params string[] parts)
```

Joins `parts` with `:` to build a key. It is the recommended way to build keys, because it keeps the segment order consistent across the code base.

- **Parameters**
  - `parts`: The key segments, in order. May be empty.
- **Return value**: The joined string. With no segments, the result is `""`. A `null` segment is treated as an empty string by `string.Join`.
- **Exceptions**: `ArgumentNullException` if `parts` is `null`.

The method does not escape `:`. `("a:b", "c")` and `("a", "b:c")` produce the same key. Keep `:` out of the segments, or make sure the segment values are never ambiguous.

## Expiration policy

The `expiration` argument is the only control over how long a value stays in the cache. Pick a value from these rules:

- **`null` means no expiry.** The entry stays until `RemoveAsync`, `ClearAsync`, or process exit. Use it only for data that changes rarely and that you remove explicitly.
- **A positive `TimeSpan` is the lifetime.** The entry is treated as absent once `DateTime.UtcNow` passes the expiry time. Expired entries are removed lazily, so `GetCacheSize` can still count them until they are cleaned up.
- **A zero or negative `TimeSpan` produces an entry that expires at once.** Do not use it to mean "no caching". It stores a value that is never returned.

Choose the lifetime from how stale the data is allowed to be. For a health probe that runs every few seconds, a 30-second lifetime is reasonable. For a configuration lookup that changes only on deploy, a longer lifetime or `null` with explicit removal is better.

Factory exceptions are never cached. If `factory` throws, the next call runs it again, regardless of `expiration`.

## Usage

### Example 1: Caching an expensive probe with a lifetime

```csharp
using CaddyVpsToolkit.Caching;

ICacheService cache = new MemoryCache();

string key = CacheExtensions.MakeCacheKey("upstream", "health", upstreamId);

// Concurrent requests for the same upstream share one probe per 30 seconds.
var status = await cache.GetOrCreateAsync(
    key,
    () => _healthProbe.CheckAsync(upstreamId),
    TimeSpan.FromSeconds(30));
```

`CheckAsync` returns `Task<T>`, so it can be passed directly as the factory. Wrap a synchronous value with `Task.FromResult` instead.

### Example 2: A cheap lookup where a duplicate call is fine

```csharp
// Two simultaneous misses may both read the file. The second write wins,
// and both values are identical, so no lock is needed.
string json = await cache.GetOrSetAsync(
    CacheExtensions.MakeCacheKey("config", "routes"),
    () => File.ReadAllTextAsync(routesPath),
    TimeSpan.FromMinutes(5));
```

### Example 3: Synchronous factory

```csharp
// The factory type is Func<Task<T>>, so wrap a synchronous result.
int port = await cache.GetOrCreateAsync(
    CacheExtensions.MakeCacheKey("service", serviceId, "port"),
    () => Task.FromResult(ResolvePort(serviceId)),
    TimeSpan.FromMinutes(10));
```

### Example 4: Permanent entry removed on change

```csharp
// null means the entry never expires. Remove it explicitly when the source changes.
string key = CacheExtensions.MakeCacheKey("pool", poolId, "server", serverId);

var server = await cache.GetOrCreateAsync(
    key,
    () => _repository.GetServerAsync(poolId, serverId),
    expiration: null);

// After an update to the server record:
await cache.RemoveAsync(key);
```

### Example 5: Keeping one type per key

```csharp
// A key must always be used with the same T. A read with a different T is a miss,
// so GetOrCreateAsync runs its factory and overwrites the stored value.
string key = CacheExtensions.MakeCacheKey("retry", "count");

int retries = await cache.GetOrCreateAsync(key, () => Task.FromResult(3));

// Do not read the same key as a string. The mismatch is treated as a miss,
// and the next GetOrCreateAsync<int> call replaces the value.
```

## Behaviour notes

- **`MakeCacheKey` is the only key helper.** There is no validation of the segments beyond `null` checks on the array.
- **Null results are cached.** A `null` returned by `factory` for a reference type is stored and returned as a hit on later calls.
- **The per-key lock is process-wide.** `GetOrCreateAsync` keeps its semaphores in a `static` dictionary keyed only by the key string. Two cache instances in the same process that use the same key share one semaphore. Keys from different caches that happen to match will serialize on the same lock.
- **The lock does not span processes.** If two processes run the same code, each one runs its own factory on a miss.
- **Type mismatch is a miss.** See Example 5. Keep the type used for a key consistent across the code base.
