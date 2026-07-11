# ConfigurationService

`ConfigurationService` provides a persistent, asynchronous key-value store for the `caddy-vps-toolkit` application. It abstracts configuration access behind string-based keys with optional generic deserialization, supports loading and saving state to a file, and exposes convenience methods for well-known settings such as the Caddy admin port, logging level, and health-check toggle.

## API

### Constructors

```csharp
public ConfigurationService()
```
Initializes a new instance with an empty in-memory store. No file is loaded until `LoadFromFileAsync` is called.

---

### GetValueAsync (string overload)

```csharp
public async Task<string> GetValueAsync(string key)
```
**Purpose**: Retrieves the raw string value associated with `key`.  
**Parameters**: `key` – case-sensitive configuration key.  
**Returns**: The stored string, or `null` if the key does not exist.  
**Throws**: `ArgumentNullException` when `key` is `null`.

---

### GetValueAsync\<T\> (generic overload)

```csharp
public async Task<T> GetValueAsync<T>(string key)
```
**Purpose**: Retrieves a value and deserializes it to type `T`.  
**Parameters**: `key` – case-sensitive configuration key.  
**Returns**: The deserialized instance of `T`, or `default(T)` when the key is absent.  
**Throws**: `ArgumentNullException` when `key` is `null`; `JsonException` or similar if the stored string cannot be deserialized to `T`.

---

### SetValueAsync

```csharp
public async Task SetValueAsync(string key, string value)
```
**Purpose**: Creates or updates the entry for `key` with the given `value`.  
**Parameters**: `key` – case-sensitive key; `value` – string to store (may be `null`, which is stored as an empty string).  
**Returns**: A completed task.  
**Throws**: `ArgumentNullException` when `key` is `null`.

---

### DeleteAsync

```csharp
public async Task<bool> DeleteAsync(string key)
```
**Purpose**: Removes the entry identified by `key`.  
**Parameters**: `key` – case-sensitive key.  
**Returns**: `true` if the key existed and was removed; `false` if the key was not present.  
**Throws**: `ArgumentNullException` when `key` is `null`.

---

### GetAllAsync

```csharp
public async Task<Dictionary<string, string>> GetAllAsync()
```
**Purpose**: Returns a snapshot of all key-value pairs currently held in memory.  
**Returns**: A new `Dictionary<string, string>` containing every entry. Modifications to the dictionary do not affect the service.  
**Throws**: No documented exceptions.

---

### LoadFromFileAsync

```csharp
public async Task LoadFromFileAsync(string filePath)
```
**Purpose**: Reads a JSON file and replaces the in-memory store with its contents.  
**Parameters**: `filePath` – path to a JSON file whose root object contains string key-value pairs.  
**Returns**: A completed task.  
**Throws**: `ArgumentNullException` when `filePath` is `null`; `FileNotFoundException` when the path does not exist; `JsonException` when the file content is not a valid flat key-value object.

---

### SaveToFileAsync

```csharp
public async Task SaveToFileAsync(string filePath)
```
**Purpose**: Serializes the current in-memory store as JSON and writes it to `filePath`.  
**Parameters**: `filePath` – destination file path; parent directories are created if missing.  
**Returns**: A completed task.  
**Throws**: `ArgumentNullException` when `filePath` is `null`; `IOException` or `UnauthorizedAccessException` on write failures.

---

### SetCaddyAdminPortAsync

```csharp
public async Task SetCaddyAdminPortAsync(int port)
```
**Purpose**: Persists the Caddy admin API port under a well-known internal key.  
**Parameters**: `port` – integer port number (validated to be in range 1–65535).  
**Returns**: A completed task.  
**Throws**: `ArgumentOutOfRangeException` when `port` is outside 1–65535.

---

### GetCaddyAdminPortAsync

```csharp
public async Task<int> GetCaddyAdminPortAsync()
```
**Purpose**: Retrieves the stored Caddy admin API port.  
**Returns**: The port number, or a default value (typically 2019) when no value has been set.  
**Throws**: No documented exceptions.

---

### SetLoggingLevelAsync

```csharp
public async Task SetLoggingLevelAsync(string level)
```
**Purpose**: Stores the application logging level under a reserved key.  
**Parameters**: `level` – a recognised log-level string (e.g., `"Information"`, `"Debug"`, `"Warning"`).  
**Returns**: A completed task.  
**Throws**: `ArgumentNullException` when `level` is `null`; `ArgumentException` when `level` is not a valid `LogLevel` name.

---

### GetLoggingLevelAsync

```csharp
public async Task<string> GetLoggingLevelAsync()
```
**Purpose**: Retrieves the stored logging level.  
**Returns**: The level string, or a sensible default (e.g., `"Information"`) when unset.  
**Throws**: No documented exceptions.

---

### SetHealthCheckEnabledAsync

```csharp
public async Task SetHealthCheckEnabledAsync(bool enabled)
```
**Purpose**: Enables or disables the health-check feature via a well-known key.  
**Parameters**: `enabled` – `true` to enable, `false` to disable.  
**Returns**: A completed task.  
**Throws**: No documented exceptions.

---

### IsHealthCheckEnabledAsync

```csharp
public async Task<bool> IsHealthCheckEnabledAsync()
```
**Purpose**: Queries whether health checks are enabled.  
**Returns**: `true` if enabled; `false` otherwise (defaults to `false` when never set).  
**Throws**: No documented exceptions.

## Usage

### Example 1: Bootstrapping configuration from a file and reading typed values

```csharp
var config = new ConfigurationService();
await config.LoadFromFileAsync("/etc/caddy-vps/settings.json");

// Read a raw string
string domain = await config.GetValueAsync("primary_domain");

// Read a complex object stored as JSON
var endpoints = await config.GetValueAsync<List<EndpointInfo>>("endpoints");

// Use convenience members
int port = await config.GetCaddyAdminPortAsync();
string logLevel = await config.GetLoggingLevelAsync();
```

### Example 2: Programmatic setup and persistence

```csharp
var config = new ConfigurationService();

await config.SetCaddyAdminPortAsync(2020);
await config.SetLoggingLevelAsync("Debug");
await config.SetHealthCheckEnabledAsync(true);
await config.SetValueAsync("backup_retention_days", "14");

// Persist to disk
await config.SaveToFileAsync("/etc/caddy-vps/settings.json");

// Later, toggle health checks off
await config.SetHealthCheckEnabledAsync(false);
bool hc = await config.IsHealthCheckEnabledAsync(); // false
```

## Notes

- **Thread safety**: All public methods that mutate or read the in-memory store are asynchronous and internally synchronised. Concurrent calls to `SetValueAsync`, `DeleteAsync`, `LoadFromFileAsync`, and the convenience setters are safe without external locking.
- **LoadFromFileAsync replacement semantics**: Calling `LoadFromFileAsync` completely replaces the current store. Any unsaved changes made since the last `SaveToFileAsync` are discarded.
- **Default values for convenience members**: `GetCaddyAdminPortAsync`, `GetLoggingLevelAsync`, and `IsHealthCheckEnabledAsync` return documented defaults when their respective keys have never been set. These defaults are hard-coded and independent of any file content that does not contain the corresponding key.
- **Key collisions**: The convenience methods (`SetCaddyAdminPortAsync`, `SetLoggingLevelAsync`, `SetHealthCheckEnabledAsync`) write to reserved internal keys. Manually calling `SetValueAsync` with those same keys will overwrite the convenience values, and vice versa.
- **File format**: `LoadFromFileAsync` expects a flat JSON object of string-to-string mappings. Nested objects or arrays stored via `SetValueAsync` must be pre-serialised to strings by the caller; the generic `GetValueAsync<T>` will deserialize them on read.
- **`null` values**: Passing `null` as a value to `SetValueAsync` stores an empty string. Retrieving that key via `GetValueAsync` returns `""`, not `null`.
