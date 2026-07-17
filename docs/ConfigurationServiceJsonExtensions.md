# ConfigurationServiceJsonExtensions

This class provides JSON serialization and deserialization for `ConfigurationService` objects, along with an in-memory configuration repository that supports asynchronous CRUD operations. It is designed to facilitate configuration management in the caddy-vps-toolkit.

## API

### Static Methods

#### `public static string ToJson(ConfigurationService service)`

- **Purpose**: Serializes a `ConfigurationService` instance to its JSON representation.
- **Parameters**: `service` – The configuration service to serialize.
- **Returns**: A JSON string representing the configuration.
- **Throws**: `ArgumentNullException` if `service` is `null`. May throw `JsonSerializationException` if serialization fails.

#### `public static ConfigurationService? FromJson(string json)`

- **Purpose**: Deserializes a JSON string into a `ConfigurationService` instance.
- **Parameters**: `json` – The JSON string to deserialize.
- **Returns**: A `ConfigurationService` object if deserialization succeeds; otherwise, `null`.
- **Throws**: `ArgumentNullException` if `json` is `null`. May throw `JsonException` if the JSON is malformed.

#### `public static bool TryFromJson(string json, out ConfigurationService? service)`

- **Purpose**: Attempts to deserialize a JSON string into a `ConfigurationService` instance without throwing exceptions.
- **Parameters**: `json` – The JSON string to deserialize. `service` – When this method returns, contains the deserialized `ConfigurationService` if successful, or `null` if not.
- **Returns**: `true` if deserialization succeeded; otherwise, `false`.
- **Throws**: None. Exceptions during deserialization are caught and result in a `false` return.

### Instance Members

#### `public InMemoryConfigurationRepository InMemoryConfigurationRepository { get; }`

- **Purpose**: Gets the underlying in-memory configuration repository used for storing configuration key-value pairs.
- **Type**: `InMemoryConfigurationRepository` – a class that provides in-memory storage.
- **Remarks**: This property provides access to the repository for direct manipulation if needed, but the async methods on this class are the preferred way to interact with it.

#### `public Task<string> GetValueAsync(string key)`

- **Purpose**: Asynchronously retrieves the value associated with the specified key from the in-memory repository.
- **Parameters**: `key` – The configuration key to look up.
- **Returns**: A task that represents the asynchronous operation. The task result contains the value as a string, or `null` if the key does not exist.
- **Throws**: `ArgumentNullException` if `key` is `null`. May throw `InvalidOperationException` if the repository is not initialized.

#### `public Task SetValueAsync(string key, string value)`

- **Purpose**: Asynchronously sets the value for the specified key in the in-memory repository. If the key already exists, its value is overwritten.
- **Parameters**: `key` – The configuration key. `value` – The value to store.
- **Returns**: A task that represents the asynchronous operation.
- **Throws**: `ArgumentNullException` if `key` or `value` is `null`.

#### `public Task<bool> DeleteAsync(string key)`

- **Purpose**: Asynchronously removes the configuration entry with the specified key from the in-memory repository.
- **Parameters**: `key` – The key to delete.
- **Returns**: A task that represents the asynchronous operation. The task result is `true` if the key was found and removed; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `key` is `null`.

#### `public Task<Dictionary<string, string>> GetAllAsync()`

- **Purpose**: Asynchronously retrieves all configuration key-value pairs from the in-memory repository.
- **Parameters**: None.
- **Returns**: A task that represents the asynchronous operation. The task result is a dictionary containing all stored keys and their values.
- **Throws**: None.

## Usage

### Example 1: Serializing and deserializing a ConfigurationService

```csharp
using CaddyVpsToolkit.Configuration;

var service = new ConfigurationService
{
    // ... populate service properties
};

// Serialize to JSON
string json = ConfigurationServiceJsonExtensions.ToJson(service);

// Deserialize back
ConfigurationService? restored = ConfigurationServiceJsonExtensions.FromJson(json);
if (restored != null)
{
    Console.WriteLine("Deserialization succeeded.");
}

// Safe deserialization attempt
if (ConfigurationServiceJsonExtensions.TryFromJson(json, out var safeRestored))
{
    Console.WriteLine("Safe deserialization succeeded.");
}
```

### Example 2: Using the in-memory repository

```csharp
using CaddyVpsToolkit.Configuration;

var extensions = new ConfigurationServiceJsonExtensions();

// Set some configuration values
await extensions.SetValueAsync("host", "example.com");
await extensions.SetValueAsync("port", "443");

// Retrieve a value
string? host = await extensions.GetValueAsync("host");
Console.WriteLine($"Host: {host}");

// Get all values
var allConfig = await extensions.GetAllAsync();
foreach (var kvp in allConfig)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}

// Delete a key
bool deleted = await extensions.DeleteAsync("port");
Console.WriteLine($"Port deleted: {deleted}");
```

## Notes

- **Thread Safety**: The static methods `ToJson`, `FromJson`, and `TryFromJson` are thread-safe as they do not modify any shared state. The instance methods (`GetValueAsync`, `SetValueAsync`, `DeleteAsync`, `GetAllAsync`) operate on the `InMemoryConfigurationRepository` property. The thread-safety of those methods depends on the implementation of `InMemoryConfigurationRepository`. If it is not designed for concurrent access, callers should synchronize access externally.
- **Null Handling**: All methods that accept string parameters (`key`, `value`, `json`) throw `ArgumentNullException` if a null argument is provided. The `FromJson` method returns `null` for invalid JSON rather than throwing, while `TryFromJson` provides a safe alternative without exceptions.
- **Edge Cases**:
  - `GetValueAsync` returns `null` for missing keys; it does not throw.
  - `DeleteAsync` returns `false` if the key does not exist.
  - `SetValueAsync` overwrites existing keys silently.
  - The `InMemoryConfigurationRepository` property is likely initialized in the constructor; ensure the instance is properly constructed before calling instance methods.
- **Serialization**: The JSON serialization format is not specified here; it is assumed to be compatible with the `ConfigurationService` type's structure. Changes to the `ConfigurationService` class may require updating the serialization logic.
