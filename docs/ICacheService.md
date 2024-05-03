# ICacheService

`ICacheService` provides a generic, asynchronous in-memory caching abstraction with support for time-based expiration, manual eviction, and atomic get-or-set semantics. It is designed for short-lived, non-persistent data that can be safely discarded when expired or when the application no longer requires it.

## API

### `public object Value`

Gets the raw cached object associated with the current instance. This property exposes the underlying stored value without type conversion or expiration checks.

### `public DateTime? ExpiresAt`

Gets the absolute expiration time for the cached entry, if one was specified. Returns `null` when the entry has no expiration and persists until explicitly removed.

### `public ValueTask<T> GetAsync<T>`

Retrieves a cached value and casts it to the specified type `T`. Returns a completed `ValueTask<T>` wrapping the cached value. Throws `InvalidCastException` if the stored object cannot be cast to `T`. If the entry has expired, the behaviour is implementation-defined — typically returning the expired value or throwing, depending on the underlying store’s policy.

### `public ValueTask SetAsync<T>`

Stores a value of type `T` in the cache under the current key. Accepts an optional expiration parameter. Returns a completed `ValueTask` once the value has been placed into the backing store. Overwrites any existing entry for the same key without warning.

### `public ValueTask RemoveAsync`

Removes the entry associated with the current key from the cache. If no entry exists, the call completes silently without error. Returns a completed `ValueTask`.

### `public ValueTask ClearAsync`

Removes all entries from the entire cache. Returns a completed `ValueTask` once the backing store is empty.

### `public async ValueTask<bool> ExistsAsync`

Checks whether an entry exists for the current key and has not yet expired. Returns `true` if a non-expired entry is present; otherwise `false`. Expired entries are treated as non-existent.

### `public void CleanExpiredEntries`

Synchronously scans the entire cache and removes all entries whose `ExpiresAt` timestamp has passed. This is a maintenance operation that does not require async invocation.

### `public int GetCacheSize`

Returns the total number of entries currently held in the cache, including expired entries that have not yet been cleaned.

### `public static async ValueTask<T> GetOrSetAsync<T>`

Atomically retrieves an existing value for a given key, or creates and stores a new value if none exists or it has expired. Accepts a key, a factory delegate that produces a `ValueTask<T>`, and an optional expiration. Returns the existing or newly created value. The factory is invoked at most once per call; concurrent callers for the same key may both receive the same result without duplicate factory execution, depending on the implementation’s locking strategy.

### `public static string MakeCacheKey`

Constructs a deterministic cache key string from a set of input parameters. Typically used to generate compound keys from multiple identifiers (e.g. combining a prefix with an entity ID). The exact signature and parameter list are implementation-specific, but the method always returns a string suitable for use as a cache key.

## Usage

### Example 1: Retrieve or populate a user profile

```csharp
string cacheKey = ICacheService.MakeCacheKey("user:profile", userId);

UserProfile profile = await ICacheService.GetOrSetAsync<UserProfile>(
    cacheKey,
    async () => await _userRepository.GetProfileAsync(userId),
    TimeSpan.FromMinutes(10)
);

// profile is now either the cached value or a freshly fetched one.
```

### Example 2: Manual cache lifecycle with expiration checks

```csharp
string sessionKey = ICacheService.MakeCacheKey("session", sessionId);

if (await _cache.ExistsAsync(sessionKey))
{
    var session = await _cache.GetAsync<SessionData>(sessionKey);
    // Use session data.
}
else
{
    // Session expired or never existed — redirect to login.
}

// Periodically purge stale entries.
_cache.CleanExpiredEntries();
int remaining = _cache.GetCacheSize();
```

## Notes

- **Expiration is lazy by default.** Entries past their `ExpiresAt` may remain in the cache and count toward `GetCacheSize` until `CleanExpiredEntries` is called or they are accessed by a method that enforces expiration checks.
- **Thread safety.** `GetOrSetAsync<T>` is designed to prevent redundant factory invocations under concurrent access, but individual `GetAsync<T>`, `SetAsync<T>`, and `RemoveAsync` calls are not atomic with respect to each other. External synchronisation is required if multiple operations must form a transactional unit.
- **Type casting.** `GetAsync<T>` performs a direct cast. Storing a value of one type and retrieving it as an incompatible type will throw `InvalidCastException`. No coercion or conversion is attempted.
- **`MakeCacheKey` determinism.** The method must produce identical output for identical inputs. Collisions caused by non-unique parameter combinations are the caller’s responsibility.
- **`CleanExpiredEntries` is synchronous.** It may block the calling thread for a duration proportional to cache size. Avoid calling it on hot paths or UI threads.
- **Static vs instance members.** `GetOrSetAsync<T>` and `MakeCacheKey` are static and operate on a default or ambient cache instance. Instance members require a reference to a specific `ICacheService` implementation. Mixing static and instance calls without understanding which backing store they target can lead to cache fragmentation.
