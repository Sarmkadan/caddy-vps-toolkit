# ServiceManagementService
The `ServiceManagementService` class provides a comprehensive set of methods for managing services, including creation, updating, deletion, and retrieval of services. It allows for the manipulation of service status, priority, and auto-start settings, as well as searching and counting services. This class is designed to be used in a variety of scenarios where service management is required, such as in system administration or automation tasks.

## API
The `ServiceManagementService` class has the following public members:
* `CreateServiceAsync`: Creates a new service. Returns a `string` representing the created service. Throws if the creation fails.
* `UpdateServiceAsync`: Updates an existing service. Returns a `bool` indicating whether the update was successful. Throws if the update fails.
* `DeleteServiceAsync`: Deletes a service. Returns a `bool` indicating whether the deletion was successful. Throws if the deletion fails.
* `GetServiceAsync`: Retrieves a service by its identifier. Returns a `ManagedService` object representing the retrieved service. Throws if the service is not found.
* `GetAllServicesAsync`: Retrieves all services. Returns a `List<ManagedService>` containing all services.
* `GetServicesByTypeAsync`: Retrieves services of a specific type. Returns a `List<ManagedService>` containing services of the specified type.
* `GetEnabledServicesAsync`: Retrieves all enabled services. Returns a `List<ManagedService>` containing all enabled services.
* `UpdateServiceStatusAsync`: Updates the status of a service. Returns a `bool` indicating whether the update was successful. Throws if the update fails.
* `SetServiceEnabledAsync`: Enables or disables a service. Returns a `bool` indicating whether the operation was successful. Throws if the operation fails.
* `SetAutoStartAsync`: Sets the auto-start setting of a service. Returns a `bool` indicating whether the operation was successful. Throws if the operation fails.
* `SearchServicesAsync`: Searches for services based on a search query. Returns a `List<ManagedService>` containing the search results.
* `GetServiceCountAsync`: Retrieves the total number of services. Returns an `int` representing the service count.
* `ServiceExistsAsync`: Checks if a service exists. Returns a `bool` indicating whether the service exists.
* `GetRunningServicesCountAsync`: Retrieves the number of running services. Returns an `int` representing the running service count.
* `UpdateServicePriorityAsync`: Updates the priority of a service. Returns a `bool` indicating whether the update was successful. Throws if the update fails.

## Usage
Here are two examples of using the `ServiceManagementService` class:
```csharp
// Example 1: Create and start a new service
var serviceManagementService = new ServiceManagementService();
var newService = await serviceManagementService.CreateServiceAsync();
await serviceManagementService.SetServiceEnabledAsync(newService, true);
await serviceManagementService.SetAutoStartAsync(newService, true);

// Example 2: Retrieve and update an existing service
var existingService = await serviceManagementService.GetServiceAsync("MyService");
if (existingService != null)
{
    await serviceManagementService.UpdateServicePriorityAsync(existingService, 1);
    await serviceManagementService.UpdateServiceStatusAsync(existingService, true);
}
```

## Notes
When using the `ServiceManagementService` class, note that some methods may throw exceptions if the underlying service management operations fail. Additionally, some methods may return `null` or empty collections if no services match the specified criteria. The class is designed to be thread-safe, but concurrent access to the same service may still result in unexpected behavior. It is recommended to use the `ServiceManagementService` class in a single-threaded context or to synchronize access to services using external locking mechanisms.
