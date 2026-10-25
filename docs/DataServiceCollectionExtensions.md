# DataServiceCollectionExtensions

`DataServiceCollectionExtensions` (defined in `Data/ServiceCollectionExtensions.cs`) provides one extension method on `IServiceCollection` that registers the SQLite-backed configuration repository from the `CaddyVpsToolkit.Data` namespace. The class is `public static`.

The registered service is `IConfigurationRepository`, the contract described in [IConfigurationRepository](IConfigurationRepository.md). The implementation is [ConfigurationRepository](ConfigurationRepository.md), which stores key-value pairs in a `Configuration` table.

## API

### `AddConfigurationRepository`
```csharp
public static IServiceCollection AddConfigurationRepository(this IServiceCollection services)
```

Registers `ConfigurationRepository` as the singleton implementation of `IConfigurationRepository`. The container creates one instance on first resolution and reuses it for the lifetime of the provider.

**Parameters:**
- `services` (`IServiceCollection`): The service collection that receives the registration.

**Return value:**
- Returns the same `IServiceCollection` instance, so calls can be chained.

**Exceptions:**
- Throws `ArgumentNullException` if `services` is `null`.
- Resolving `IConfigurationRepository` throws `DatabaseException` if the `Configuration` table cannot be created. The exception is raised on first resolution, not when this method is called.

---

## Usage

### Example 1: Registering the repository at startup
```csharp
using CaddyVpsToolkit.Data;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddConfigurationRepository();

using var provider = services.BuildServiceProvider();

var repository = provider.GetRequiredService<IConfigurationRepository>();
await repository.SetValueAsync("caddy:admin-email", "ops@example.com");
string? email = await repository.GetValueAsync("caddy:admin-email");
```

### Example 2: Chaining with other registrations
The method returns the collection, so it fits into a fluent registration block.
```csharp
using CaddyVpsToolkit.Data;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddConfigurationRepository();

using var provider = services.BuildServiceProvider();
```

### Example 3: Replacing the repository in tests
Registrations are applied in order, and the last one wins when a single `IConfigurationRepository` is resolved. A test can register a substitute after calling `AddConfigurationRepository`, which avoids opening the real database.
```csharp
using CaddyVpsToolkit.Data;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

var fake = Substitute.For<IConfigurationRepository>();

var services = new ServiceCollection();
services.AddConfigurationRepository();
services.AddSingleton(fake);

using var provider = services.BuildServiceProvider();

// Resolves the substitute, not ConfigurationRepository
var repository = provider.GetRequiredService<IConfigurationRepository>();
```

---

## Notes

- **Singleton lifetime**: The repository is shared across the provider. It holds only a connection string, and each operation opens its own `SQLiteConnection`, so sharing the instance is safe.
- **Constructor side effect**: `ConfigurationRepository` creates the `Configuration` table in its constructor, against the path in `AppConstants.DatabasePath`. The database file is created or opened the first time the service is resolved. Resolving it on a machine without write access to that path throws `DatabaseException`.
- **Repeated calls**: Each call adds another descriptor. Calling `AddConfigurationRepository` twice does not throw, but it duplicates the registration. Only one instance is created for a single resolution.
- **Program.cs**: The application entry point registers the same mapping inline with `services.AddSingleton<IConfigurationRepository, ConfigurationRepository>()`. It does not call this method. Do not call both in the same collection, or the registrations are duplicated.
- **Validation**: The container can build `ConfigurationRepository` from its parameterless constructor, so `ValidateOnBuild = true` does not fail because of this method.
- **Scope**: The method only adds registrations. It does not open the database or change configuration on its own.
