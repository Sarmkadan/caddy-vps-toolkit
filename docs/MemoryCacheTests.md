# MemoryCacheTests

Unit tests for the `MemoryCache` wrapper, verifying asynchronous cache operations, key management, expiration policies, and thread-safe behaviors.

## API

### `SetAsync_ThenGetAsync_ReturnsStoredValue`
Stores a value under a given key and verifies it can be retrieved immediately afterward.

- **Parameters**
  - `key`: The cache key.
  - `value`: The value to store.
- **Return value**: `Task` completing when the value is stored and retrieved.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `GetAsync_MissingKey_ReturnsDefault`
Attempts to retrieve a value using a non-existent key and confirms the default value is returned.

- **Parameters**
  - `key`: The cache key to look up.
- **Return value**: `Task<T>` completing with the default value for type `T`.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `GetAsync_EmptyKey_ReturnsDefault`
Attempts to retrieve a value using an empty key and confirms the default value is returned.

- **Parameters**
  - `key`: The cache key to look up.
- **Return value**: `Task<T>` completing with the default value for type `T`.

### `SetAsync_EmptyKey_DoesNotStore`
Attempts to store a value under an empty key and verifies the cache remains unchanged.

- **Parameters**
  - `key`: The cache key to use.
  - `value`: The value to store.
- **Return value**: `Task` completing when the operation finishes.
- **Throws**: Does not throw; silently ignores empty keys.

### `ExistsAsync_ExistingKey_ReturnsTrue`
Checks whether a key exists in the cache after storing a value under it.

- **Parameters**
  - `key`: The cache key to check.
- **Return value**: `Task<bool>` completing with `true` if the key exists.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `ExistsAsync_MissingKey_ReturnsFalse`
Checks whether a key exists in the cache when it has not been stored.

- **Parameters**
  - `key`: The cache key to check.
- **Return value**: `Task<bool>` completing with `false`.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `RemoveAsync_ExistingKey_RemovesEntry`
Removes a key-value pair from the cache and verifies it can no longer be retrieved.

- **Parameters**
  - `key`: The cache key to remove.
- **Return value**: `Task` completing when the entry is removed.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `RemoveAsync_MissingKey_DoesNotThrow`
Attempts to remove a non-existent key and confirms no exception is thrown.

- **Parameters**
  - `key`: The cache key to remove.
- **Return value**: `Task` completing without throwing.
- **Throws**: Does not throw.

### `ClearAsync_RemovesAllEntries`
Empties the entire cache and verifies all previously stored keys are no longer present.

- **Return value**: `Task` completing when the cache is cleared.

### `SetAsync_WithExpiration_EntryExpires`
Stores a value with an expiration time in the past and verifies it is no longer retrievable.

- **Parameters**
  - `key`: The cache key.
  - `value`: The value to store.
  - `expiration`: A past `DateTimeOffset` for the entry.
- **Return value**: `Task` completing when the value is stored.
- **Throws**: `ArgumentOutOfRangeException` if `expiration` is not in the future.

### `SetAsync_WithFutureExpiration_EntryIsStillAccessible`
Stores a value with a future expiration time and verifies it can still be retrieved before expiration.

- **Parameters**
  - `key`: The cache key.
  - `value`: The value to store.
  - `expiration`: A future `DateTimeOffset` for the entry.
- **Return value**: `Task` completing when the value is stored and retrieved.
- **Throws**: `ArgumentOutOfRangeException` if `expiration` is not in the future.

### `CleanExpiredEntries_RemovesOnlyExpired`
Triggers cleanup of expired entries and verifies only expired entries are removed.

- **Return value**: `Task` completing when cleanup finishes.
- **Throws**: Does not throw.

### `SetAsync_OverwritesExistingKey`
Stores a value under an existing key and verifies the new value is retrieved afterward.

- **Parameters**
  - `key`: The cache key.
  - `oldValue`: The initial value.
  - `newValue`: The value to overwrite with.
- **Return value**: `Task` completing when the overwrite is complete.
- **Throws**: `ArgumentNullException` if `key` is `null`.

### `GetOrSetAsync_MissingKey_CallsFactoryAndStores`
Attempts to retrieve a missing key and, upon missing it, invokes the factory function, stores the result, and returns it.

- **Parameters**
  - `key`: The cache key to look up.
  - `factory`: Synchronous function producing the value if missing.
- **Return value**: `Task<T>` completing with the stored value.
- **Throws**: `ArgumentNullException` if `key` is `null` or `factory` is `null`.

### `GetOrSetAsync_ExistingKey_DoesNotCallFactory`
Attempts to retrieve an existing key and verifies the factory function is not invoked.

- **Parameters**
  - `key`: The cache key to look up.
  - `factory`: Synchronous function producing the value if missing.
- **Return value**: `Task<T>` completing with the cached value.
- **Throws**: `ArgumentNullException` if `key` is `null` or `factory` is `null`.

### `MakeCacheKey_MultipleParts_JoinsWithColon`
Combines multiple parts into a single cache key using colon separators.

- **Parameters**
  - `parts`: The parts to join.
- **Return value**: `string` representing the joined key.
- **Throws**: `ArgumentNullException` if `parts` is `null`.

### `MakeCacheKey_SinglePart_ReturnsPart`
Returns the single part as the cache key without modification.

- **Parameters**
  - `part`: The single part to use as the key.
- **Return value**: `string` equal to `part`.
- **Throws**: Does not throw.
