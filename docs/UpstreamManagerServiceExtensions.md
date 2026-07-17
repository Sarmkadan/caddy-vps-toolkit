# UpstreamManagerServiceExtensions

Extension methods for managing and querying upstream pools in the Caddy VPS toolkit's load balancing system.

## API

### TryGetPoolAsync

Attempts to retrieve an upstream pool by its identifier.

**Parameters:**
- `service` - The `UpstreamManagerService` instance
- `poolId` - The unique identifier of the pool to retrieve

**Returns:** A tuple containing:
- `Success` - `true` if the pool was found; otherwise `false`
- `Pool` - The `UpstreamPool` instance if found; otherwise `null`

**Exceptions:** Throws `ArgumentNullException` if `poolId` is null.

---

### TryRemovePoolAsync

Attempts to remove an upstream pool by its identifier.

**Parameters:**
- `service` - The `UpstreamManagerService` instance
- `poolId` - The unique identifier of the pool to remove

**Returns:** `true` if the pool was found and removed; otherwise `false`

**Exceptions:** Throws `ArgumentNullException` if `poolId` is null.

---

### GetPoolsAsync

Retrieves all configured upstream pools.

**Parameters:**
- `service` - The `UpstreamManagerService` instance

**Returns:** An `IReadOnlyList<UpstreamPool>` containing all pools, including disabled ones.

---

### GenerateCaddyConfigForAllEnabledPoolsAsync

Generates a complete Caddy configuration for all enabled upstream pools.

**Parameters:**
- `service` - The `UpstreamManagerService` instance

**Returns:** A string containing the full Caddyfile configuration for all enabled pools.

**Exceptions:** Throws `InvalidOperationException` if no pools are configured or if configuration generation fails.

---

### GetTotalActiveConnectionsAsync

Gets the total number of active connections across all upstream pools.

**Parameters:**
- `service` - The `UpstreamManagerService` instance

**Returns:** The total count of active connections.

---

### GetTotalHealthyUpstreamsAsync

Gets the total number of healthy upstream servers across all pools.

**Parameters:**
- `service` - The `UpstreamManagerService` instance

**Returns:** The total count of healthy upstream servers.

---

### GetPoolSummariesAsync

Retrieves summary information for all upstream pools.

**Parameters:**
- `service` - The `UpstreamManagerService` instance

**Returns:** An `IReadOnlyList<PoolSummary>` containing summary data for each pool.

---

### SelectUpstreamAsync

Selects an upstream server from a pool using the configured load balancing strategy.

**Parameters:**
- `service` - The `UpstreamManagerService` instance
- `poolId` - The unique identifier of the pool to select from

**Returns:** An `UpstreamServer` instance representing the selected server, or `null` if the pool is empty or all servers are unhealthy.

**Exceptions:** Throws `ArgumentNullException` if `poolId` is null.
Throws `KeyNotFoundException` if the pool with the specified identifier does not exist.

---

### RecordUpstreamResultsAsync

Records health check results for upstream servers.

**Parameters:**
- `service` - The `UpstreamManagerService` instance
- `results` - A collection of `UpstreamHealthResult` objects containing the health check outcomes

**Returns:** A `Task` that completes when the results have been recorded.

**Exceptions:** Throws `ArgumentNullException` if `results` is null.

---

### GetUnhealthyUpstreamIdsAsync

Retrieves identifiers for all upstream servers that are currently unhealthy.

**Parameters:**
- `service` - The `UpstreamManagerService` instance

**Returns:** An `IReadOnlyList<string>` containing the identifiers of unhealthy upstream servers.

---

### PoolSummary

A record containing summary information about an upstream pool.

**Properties:**
- `PoolId` - The unique identifier of the pool
- `Name` - The display name of the pool
- `Enabled` - Whether the pool is enabled
- `TotalServers` - The total number of servers in the pool
- `HealthyServers` - The number of healthy servers in the pool
- `ActiveConnections` - The number of active connections to the pool

## Usage

### Example 1: Retrieving and monitoring a pool

```csharp
var service = new UpstreamManagerService();
var poolId = "web-servers";

// Attempt to retrieve a pool
var (success, pool) = await service.TryGetPoolAsync(poolId);

if (success && pool != null)
{
    Console.WriteLine($"Found pool '{pool.Name}' with {pool.Servers.Count} servers");
    
    // Get pool summary
    var summaries = await service.GetPoolSummariesAsync();
    var summary = summaries.FirstOrDefault(s => s.PoolId == poolId);
    
    if (summary != null)
    {
        Console.WriteLine($"Pool status: {summary.Enabled}, Healthy: {summary.HealthyServers}/{summary.TotalServers}");
    }
}
```

### Example 2: Generating Caddy configuration

```csharp
var service = new UpstreamManagerService();

// Generate configuration for all enabled pools
var config = await service.GenerateCaddyConfigForAllEnabledPoolsAsync();

Console.WriteLine("Generated Caddy configuration:");
Console.WriteLine(config);

// Save to file
await File.WriteAllTextAsync("Caddyfile", config);
```

## Notes

- All methods are thread-safe and can be called concurrently from multiple threads.
- The `PoolSummary` record provides a lightweight view of pool status without requiring access to the full pool configuration.
- When calling `SelectUpstreamAsync`, the method returns `null` if all servers in the pool are unhealthy, allowing callers to implement fallback behavior.
- The `RecordUpstreamResultsAsync` method should be called periodically (typically via a background health check service) to maintain accurate upstream health status.
- Pool identifiers are case-sensitive strings that uniquely identify each pool within the service.
- Disabled pools are included in `GetPoolsAsync` but excluded from `GenerateCaddyConfigForAllEnabledPoolsAsync`.
- Connection counts returned by `GetTotalActiveConnectionsAsync` include both healthy and unhealthy connections.