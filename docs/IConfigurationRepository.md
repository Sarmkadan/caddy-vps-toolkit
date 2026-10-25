# IConfigurationRepository

`IConfigurationRepository` is the storage abstraction for application configuration in the Caddy VPS toolkit. It defines a small asynchronous key-value contract (read, write, delete, enumerate) in the `CaddyVpsToolkit.Data` namespace. Consumers such as `ConfigurationService` and `BackupService` depend on this interface rather than on a concrete store, so the storage backend can be swapped or replaced with a test double.

The interface is implemented by `ConfigurationRepository`, which is registered as a singleton for `IConfigurationRepository` in `Data/ServiceCollectionExtensions.cs` and `Program.cs`.

## API

### `public interface IConfigurationRepository`
A repository for configuration entries, where each entry is a string key mapped to a string value. All members are asynchronous.

---

### `Task<string> GetValueAsync(string key)`
Retrieves the value stored under a key.

- **Parameters**:
  - `key` (`string`): The configuration key to look up.
- **Returns**: A `Task<string>` whose result is the stored value, or `null` if no entry exists for the key.
- **Throws**: Implementations may throw `ArgumentNullException` or `ArgumentException` for an invalid key. The bundled `ConfigurationRepository` throws `ArgumentException` for a null or empty key.

---

### `Task SetValueAsync(string key, string value)`
Creates or overwrites the entry for a key.

- **Parameters**:
  - `key` (`string`): The configuration key to write.
  - `value` (`string`): The value to store for the key.
- **Returns**: A `Task` that completes when the write has been persisted.
- **Throws**: Implementations may throw `ArgumentNullException` or `ArgumentException` for an invalid key or value. The bundled `ConfigurationRepository` rejects a null or empty `value` with `ArgumentException` (see [Notes](#notes)).

---

### `Task<bool> DeleteAsync(string key)`
Removes the entry for a key.

- **Parameters**:
  - `key` (`string`): The configuration key to remove.
- **Returns**: A `Task<bool>` whose result is `true` if an entry was removed, or `false` if no entry existed for the key.
- **Throws**: Implementations may throw `ArgumentNullException` for a null key.

---

### `Task<Dictionary<string, string>> GetAllAsync()`
Returns every stored entry.

- **Parameters**: None.
- **Returns**: A `Task<Dictionary<string, string>>` containing all key-value pairs. The result is empty when nothing is stored. The bundled implementation returns entries ordered by key.
- **Throws**: No exceptions are part of the contract.

## Usage

### Example 1: Constructor Injection

`ConfigurationService` receives the repository through its constructor and delegates all reads and writes to it. Callers never create the repository directly.

```csharp
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Services;

// The DI container supplies the registered implementation
var repository = serviceProvider.GetRequiredService<IConfigurationRepository>();
var configuration = new ConfigurationService(repository);

await configuration.SetValueAsync("backup:retentionDays", "14");
string retention = await configuration.GetValueAsync("backup:retentionDays", "7");
```

### Example 2: Read, Write, and Delete

Use `GetValueAsync` for a single entry, `SetValueAsync` to create or overwrite it, and `DeleteAsync` to remove it. The boolean from `DeleteAsync` reports whether anything was actually removed.

```csharp
public sealed class NotificationSettings
{
    private readonly IConfigurationRepository _repository;

    public NotificationSettings(IConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> GetWebhookUrlAsync()
    {
        // Null means the key is absent, not that the value is empty
        return await _repository.GetValueAsync("notify:webhookUrl");
    }

    public async Task SetWebhookUrlAsync(string url)
    {
        await _repository.SetValueAsync("notify:webhookUrl", url);
    }

    public async Task<bool> ClearWebhookUrlAsync()
    {
        return await _repository.DeleteAsync("notify:webhookUrl");
    }
}
```

### Example 3: Enumerating Entries for Export

`GetAllAsync` returns a dictionary snapshot that is convenient for exporting or displaying settings. The backup flow uses this pattern to capture configuration.

```csharp
Dictionary<string, string> settings = await repository.GetAllAsync();

foreach (var (key, value) in settings)
{
    Console.WriteLine($"{key} = {value}");
}

// Restoring a snapshot writes each entry back through the same interface
foreach (var (key, value) in settings)
{
    await repository.SetValueAsync(key, value);
}
```

### Example 4: Test Double

Because the contract is an interface, unit tests can substitute an in-memory fake or an NSubstitute mock, as `BackupServiceTests` does.

```csharp
var repository = Substitute.For<IConfigurationRepository>();
repository.GetValueAsync("backup:retentionDays").Returns(Task.FromResult("14"));

var result = await repository.GetValueAsync("backup:retentionDays");
result.Should().Be("14");
```

## Notes

- **Missing key versus empty value:** `GetValueAsync` returns `null` only when the key is absent. Callers that need to distinguish "unset" from "set to an empty string" should check for `null` explicitly and must not treat `null` and `""` as equivalent.
- **Null and empty values:** The bundled `ConfigurationRepository` rejects a null or empty `value` in `SetValueAsync`. Callers should not rely on null being stored as an empty string. Use `DeleteAsync` to remove an entry instead.
- **Overwrite semantics:** `SetValueAsync` is an upsert. Writing an existing key replaces its value and updates its timestamp.
- **Consistency across calls:** The interface does not provide transactions. Multiple calls are independent, and concurrent writes to the same key resolve in the order the backend applies them.
- **Snapshot semantics:** `GetAllAsync` returns a copy. Changes made to the store afterwards are not reflected in the returned dictionary, and modifying the dictionary does not change the store.
- **Caching:** `IConfigurationRepository` does not cache. Caching is the responsibility of the consumer, as in `ConfigurationService`, which caches non-empty values it reads.
