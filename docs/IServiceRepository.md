# IServiceRepository

`IServiceRepository` defines the asynchronous data-access contract for `ManagedService` entities. It covers lookup by id or name, filtered listing by service type or enabled state, free-text search, existence and count checks, and add, update, and delete operations. Consumers depend on this interface rather than on a concrete storage implementation; `ServiceRepository` is the SQLite-backed implementation shipped with the toolkit.

The interface is declared in `Data/IServiceRepository.cs` under the `CaddyVpsToolkit.Data` namespace.

## API

### IServiceRepository

```csharp
public interface IServiceRepository
```

All members are asynchronous and return a `Task` or `Task<T>`. Callers should `await` them and propagate or handle any exception the implementation throws.

### GetByIdAsync

```csharp
Task<ManagedService> GetByIdAsync(string id)
```

Retrieves a single service by its unique identifier.

**Parameters:**
- `id` (`string`): The `ManagedService.Id` to look up. Must not be null or empty.

**Returns:**
- `Task<ManagedService>`: The matching service, or `null` if no service has that id.

**Exceptions:**
- `ArgumentException`: Thrown by `ServiceRepository` when `id` is null or empty.

### GetByNameAsync

```csharp
Task<ManagedService> GetByNameAsync(string name)
```

Retrieves a single service by its name. Names are expected to be unique; if several rows match, the implementation returns the first one it reads.

**Parameters:**
- `name` (`string`): The `ManagedService.Name` to look up. Must not be null or empty.

**Returns:**
- `Task<ManagedService>`: The matching service, or `null` if no service has that name.

**Exceptions:**
- `ArgumentException`: Thrown by `ServiceRepository` when `name` is null or empty.

### GetAllAsync

```csharp
Task<List<ManagedService>> GetAllAsync()
```

Returns every stored service, ordered by `Name`.

**Parameters:** None.

**Returns:**
- `Task<List<ManagedService>>`: All services. Returns an empty list when the store contains no services.

### GetByTypeAsync

```csharp
Task<List<ManagedService>> GetByTypeAsync(ServiceType type)
```

Returns all services of the given `ServiceType`, ordered by `Name`.

**Parameters:**
- `type` (`ServiceType`): The service type to filter by (for example `ServiceType.WebApplication` or `ServiceType.Database`).

**Returns:**
- `Task<List<ManagedService>>`: Matching services. Returns an empty list when none match.

### GetEnabledServicesAsync

```csharp
Task<List<ManagedService>> GetEnabledServicesAsync()
```

Returns all services whose `IsEnabled` flag is `true`, ordered by `Name`. Disabled services are excluded.

**Parameters:** None.

**Returns:**
- `Task<List<ManagedService>>`: Enabled services. Returns an empty list when no service is enabled.

### AddAsync

```csharp
Task<string> AddAsync(ManagedService service)
```

Inserts a new service. If `service.Id` is null or empty, the implementation generates a new GUID and assigns it to `service.Id` before saving. The service is validated before it is written.

**Parameters:**
- `service` (`ManagedService`): The service to store. Must not be null. Required fields (`Name`, `ExecutablePath`, `WorkingDirectory`) must be set and `Port` must be between 1 and 65535, as enforced by `ManagedService.Validate()`.

**Returns:**
- `Task<string>`: The identifier of the inserted service, either the one supplied on `service` or the generated one.

**Exceptions:**
- `ArgumentNullException`: Thrown by `ServiceRepository` when `service` is null.
- `ValidationException`: Thrown by `ServiceRepository` when `service.Validate()` fails.
- Storage-specific exceptions (for example a duplicate `Id` violating the primary key) propagate to the caller.

### UpdateAsync

```csharp
Task<bool> UpdateAsync(ManagedService service)
```

Overwrites the stored row whose `Id` matches `service.Id` with the current property values. The implementation sets `UpdatedAt` to the current UTC time before saving. Note that `CreatedAt` is not written by the update.

**Parameters:**
- `service` (`ManagedService`): The service with the new values. Must not be null and must carry an existing `Id`. Validated in the same way as `AddAsync`.

**Returns:**
- `Task<bool>`: `true` if a row was updated; `false` if no service has the given `Id`.

**Exceptions:**
- `ArgumentNullException`: Thrown by `ServiceRepository` when `service` is null.
- `ValidationException`: Thrown by `ServiceRepository` when `service.Validate()` fails.

### DeleteAsync

```csharp
Task<bool> DeleteAsync(string id)
```

Removes the service with the given identifier.

**Parameters:**
- `id` (`string`): The identifier of the service to remove. Must not be null or empty.

**Returns:**
- `Task<bool>`: `true` if a service was deleted; `false` if no service has that id.

**Exceptions:**
- `ArgumentException`: Thrown by `ServiceRepository` when `id` is null or empty.

### ExistsAsync

```csharp
Task<bool> ExistsAsync(string id)
```

Checks whether a service with the given identifier is stored, without loading the full entity.

**Parameters:**
- `id` (`string`): The identifier to check. Must not be null or empty.

**Returns:**
- `Task<bool>`: `true` if a matching service exists; otherwise `false`.

**Exceptions:**
- `ArgumentException`: Thrown by `ServiceRepository` when `id` is null or empty.

### GetCountAsync

```csharp
Task<int> GetCountAsync()
```

Returns the total number of stored services.

**Parameters:** None.

**Returns:**
- `Task<int>`: The number of services. Returns `0` when the store is empty.

### SearchAsync

```csharp
Task<List<ManagedService>> SearchAsync(string query)
```

Returns services whose `Name` or `Description` contains the query text as a substring. The match is a SQL `LIKE` comparison, so matching case-sensitivity follows the storage provider's collation. Results are ordered by `Name`.

**Parameters:**
- `query` (`string`): The text to search for. Must not be null or empty. The value is passed as a bound parameter, so wildcard characters are not an injection risk, but a literal `%` or `_` in the query is still treated as a wildcard by `LIKE`.

**Returns:**
- `Task<List<ManagedService>>`: Matching services. Returns an empty list when nothing matches.

**Exceptions:**
- `ArgumentException`: Thrown by `ServiceRepository` when `query` is null or empty.

## Usage

### Example 1: Register and Look Up a Service

```csharp
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Domain.Models;

IServiceRepository repository = new ServiceRepository();

var service = new ManagedService
{
    Name = "orders-api",
    Description = "Order processing API",
    Type = ServiceType.ApiService,
    ExecutablePath = "/usr/bin/dotnet",
    Arguments = "/opt/orders/Orders.dll",
    WorkingDirectory = "/opt/orders",
    Port = 5100,
    HostBinding = "127.0.0.1",
    IsEnabled = true
};

// Id is generated when left empty
string id = await repository.AddAsync(service);

ManagedService stored = await repository.GetByIdAsync(id);
Console.WriteLine($"Stored {stored?.Name ?? "nothing"} on port {stored?.Port}");
```

### Example 2: Update and Delete

```csharp
IServiceRepository repository = new ServiceRepository();

ManagedService service = await repository.GetByNameAsync("orders-api");
if (service is null)
{
    Console.WriteLine("Service not found");
    return;
}

service.Port = 5200;

// UpdatedAt is refreshed by the repository
bool updated = await repository.UpdateAsync(service);
Console.WriteLine($"Updated: {updated}");

bool deleted = await repository.DeleteAsync(service.Id);
Console.WriteLine($"Deleted: {deleted}");
```

### Example 3: Listing and Filtering

```csharp
IServiceRepository repository = new ServiceRepository();

int total = await repository.GetCountAsync();
Console.WriteLine($"Managed services: {total}");

// Only services the factory should keep running
List<ManagedService> enabled = await repository.GetEnabledServicesAsync();

// All databases, ordered by name
List<ManagedService> databases = await repository.GetByTypeAsync(ServiceType.Database);

foreach (var item in enabled)
{
    Console.WriteLine($"{item.Name} ({item.Type}) -> {item.HostBinding}:{item.Port}");
}
```

### Example 4: Search and Existence Check

```csharp
IServiceRepository repository = new ServiceRepository();

string candidateId = "3f2c9d1e-0000-4000-8000-000000000000";

if (await repository.ExistsAsync(candidateId))
{
    Console.WriteLine("Service already registered");
}
else
{
    // Fall back to a free-text search over names and descriptions
    List<ManagedService> matches = await repository.SearchAsync("api");
    Console.WriteLine($"Found {matches.Count} candidate service(s)");
}
```

### Example 5: Depending on the Interface

Constructor injection keeps consumers testable. A test can substitute an in-memory fake or a mock for `IServiceRepository` without touching SQLite.

```csharp
public sealed class ServiceStatusReporter
{
    private readonly IServiceRepository _repository;

    public ServiceStatusReporter(IServiceRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> BuildSummaryAsync()
    {
        int total = await _repository.GetCountAsync();
        List<ManagedService> enabled = await _repository.GetEnabledServicesAsync();
        return $"{enabled.Count} of {total} services enabled";
    }
}
```
