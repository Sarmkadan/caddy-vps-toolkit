# ConfigurationRepository

`ConfigurationRepository` provides a persistent, asynchronous key-value store for application configuration settings. It abstracts the underlying storage mechanism, allowing consumers to read, write, delete, and enumerate configuration entries without coupling to a specific backend.

## API

### ConfigurationRepository

```csharp
public ConfigurationRepository()
```

Default constructor. Initializes a new instance of the repository, preparing the underlying storage connection. No explicit configuration parameters are required at construction time.

### GetValueAsync

```csharp
public async Task<string> GetValueAsync(string key)
```

Retrieves the value associated with the specified key.

**Parameters:**
- `key` (`string`): The configuration key to look up. Must not be null or empty.

**Returns:**
- `Task<string>`: The stored value if the key exists; otherwise `null`.

**Exceptions:**
- `ArgumentNullException`: Thrown when `key` is null.
- `ArgumentException`: Thrown when `key` is an empty string or consists only of whitespace.
- `StorageException`: Thrown when the underlying storage layer encounters a read failure (e.g., connectivity loss, serialization error).

### SetValueAsync

```csharp
public async Task SetValueAsync(string key, string value)
```

Stores a value under the given key, overwriting any existing entry.

**Parameters:**
- `key` (`string`): The configuration key. Must not be null or empty.
- `value` (`string`): The value to persist. A null value is stored as an empty string.

**Returns:**
- `Task`: A task representing the asynchronous write operation.

**Exceptions:**
- `ArgumentNullException`: Thrown when `key` is null.
- `ArgumentException`: Thrown when `key` is an empty string or consists only of whitespace.
- `StorageException`: Thrown when the underlying storage layer fails to persist the value.

### DeleteAsync

```csharp
public async Task<bool> DeleteAsync(string key)
```

Removes the entry identified by the specified key from the configuration store.

**Parameters:**
- `key` (`string`): The configuration key to delete. Must not be null or empty.

**Returns:**
- `Task<bool>`: `true` if the key existed and was successfully removed; `false` if the key was not found.

**Exceptions:**
- `ArgumentNullException`: Thrown when `key` is null.
- `ArgumentException`: Thrown when `key` is an empty string or consists only of whitespace.
- `StorageException`: Thrown when the underlying storage layer encounters an error during the delete operation.

### GetAllAsync

```csharp
public async Task<Dictionary<string, string>> GetAllAsync()
```

Returns a snapshot of all configuration entries currently stored.

**Parameters:** None.

**Returns:**
- `Task<Dictionary<string, string>>`: A dictionary containing all key-value pairs. Returns an empty dictionary when no entries exist.

**Exceptions:**
- `StorageException`: Thrown when the underlying storage layer fails to enumerate or deserialize entries.

## Usage

### Example 1: Basic Read/Write Cycle

```csharp
var repo = new ConfigurationRepository();

// Persist a setting
await repo.SetValueAsync("app:theme", "dark");

// Retrieve and use the setting
string theme = await repo.GetValueAsync("app:theme");
Console.WriteLine($"Active theme: {theme ?? "not set"}");

// Remove the setting when no longer needed
bool deleted = await repo.DeleteAsync("app:theme");
Console.WriteLine($"Entry removed: {deleted}");
```

### Example 2: Bulk Enumeration and Conditional Update

```csharp
var repo = new ConfigurationRepository();

// Seed multiple values
await repo.SetValueAsync("db:host", "localhost");
await repo.SetValueAsync("db:port", "5432");
await repo.SetValueAsync("db:name", "production");

// Inspect all current configuration
Dictionary<string, string> allSettings = await repo.GetAllAsync();

if (allSettings.TryGetValue("db:host", out string host) && host == "localhost")
{
    // Override host for a staging environment
    await repo.SetValueAsync("db:host", "staging-db.internal");
}

// Verify the change
Console.WriteLine(await repo.GetValueAsync("db:host"));
```

## Notes

- **Null values:** Passing a null value to `SetValueAsync` results in an empty string being stored. `GetValueAsync` returns null only when the key is absent from the store, not when an empty string was explicitly written.
- **Key absence vs. empty value:** Consumers should distinguish between a missing key (`GetValueAsync` returns null) and a key deliberately set to an empty string (`GetValueAsync` returns `""`). Use `GetAllAsync` to confirm presence if this distinction matters.
- **Thread safety:** Instance methods are safe to call concurrently from multiple threads. The underlying storage operations are serialized per invocation, but no transactional guarantees span across separate calls. Concurrent `SetValueAsync` and `DeleteAsync` on the same key resolve on a last-writer-wins basis.
- **`GetAllAsync` snapshot semantics:** The returned dictionary is a point-in-time copy. Modifications made to the store after the call completes are not reflected in the returned dictionary, and mutating the dictionary itself has no effect on the persisted store.
- **Storage failures:** All methods may throw `StorageException` when the backend is unreachable or data corruption is detected. Callers should implement retry or fallback logic appropriate to their resilience requirements.
