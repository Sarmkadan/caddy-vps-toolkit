# UpstreamPoolRepository
The `UpstreamPoolRepository` class is designed to manage upstream pools, providing a range of methods for retrieving, adding, updating, and deleting upstream pool entities. It serves as a crucial component in the `caddy-vps-toolkit` project, enabling efficient interaction with upstream pool data.

## API
The `UpstreamPoolRepository` class exposes the following public members:
* `public UpstreamPoolRepository`: The constructor for the `UpstreamPoolRepository` class.
* `public async Task<UpstreamPool?> GetByIdAsync`: Retrieves an upstream pool by its ID. Returns the upstream pool entity if found, or `null` if not found. Throws an exception if an error occurs during the retrieval process.
* `public async Task<List<UpstreamPool>> GetByServiceIdAsync`: Retrieves a list of upstream pools associated with a given service ID. Returns an empty list if no upstream pools are found. Throws an exception if an error occurs during the retrieval process.
* `public async Task<List<UpstreamPool>> GetAllAsync`: Retrieves all available upstream pools. Returns an empty list if no upstream pools are found. Throws an exception if an error occurs during the retrieval process.
* `public async Task<string> AddAsync`: Adds a new upstream pool. Returns the ID of the newly added upstream pool. Throws an exception if an error occurs during the addition process.
* `public async Task<bool> UpdateAsync`: Updates an existing upstream pool. Returns `true` if the update is successful, or `false` otherwise. Throws an exception if an error occurs during the update process.
* `public async Task<bool> DeleteAsync`: Deletes an upstream pool by its ID. Returns `true` if the deletion is successful, or `false` otherwise. Throws an exception if an error occurs during the deletion process.
* `public async Task<bool> ExistsAsync`: Checks if an upstream pool with a given ID exists. Returns `true` if the upstream pool exists, or `false` otherwise. Throws an exception if an error occurs during the existence check.

## Usage
Here are two examples of using the `UpstreamPoolRepository` class:
```csharp
// Example 1: Retrieving an upstream pool by ID
var repository = new UpstreamPoolRepository();
var upstreamPool = await repository.GetByIdAsync("pool-123");
if (upstreamPool != null)
{
    Console.WriteLine($"Upstream pool name: {upstreamPool.Name}");
}
else
{
    Console.WriteLine("Upstream pool not found");
}

// Example 2: Adding a new upstream pool
var newUpstreamPool = new UpstreamPool { Name = "New Pool", ServiceId = "service-123" };
var repository = new UpstreamPoolRepository();
var newPoolId = await repository.AddAsync(newUpstreamPool);
Console.WriteLine($"New upstream pool ID: {newPoolId}");
```

## Notes
When using the `UpstreamPoolRepository` class, consider the following edge cases and thread-safety remarks:
* The `GetByIdAsync` and `ExistsAsync` methods may return `null` or `false` if the upstream pool is not found, so proper null checks and existence checks should be performed.
* The `AddAsync`, `UpdateAsync`, and `DeleteAsync` methods may throw exceptions if errors occur during the respective operations, so proper error handling should be implemented.
* The `UpstreamPoolRepository` class is designed to be thread-safe, but concurrent access to the same upstream pool entity may still result in unexpected behavior. Synchronization mechanisms, such as locks or semaphores, may be necessary to ensure data consistency in multi-threaded environments.
