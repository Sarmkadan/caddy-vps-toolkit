# UpstreamHealthTrackerExtensions

The `UpstreamHealthTrackerExtensions` class provides a set of static extension methods and data contracts designed to facilitate the monitoring, reporting, and management of upstream server health within the Caddy VPS Toolkit. It enables developers to record probe results, retrieve detailed snapshots of upstream states, aggregate pool-level health statistics, and orchestrate graceful draining or waiting strategies for healthy availability.

## API

### Methods

#### `RecordProbeResultsAsync`
Records the outcome of a health probe for a specific upstream server.
*   **Parameters**: Accepts context regarding the probe execution, including the target upstream identifier, success status, and response latency.
*   **Return Value**: Returns a `Task` that completes when the result has been persisted to the internal tracking state.
*   **Exceptions**: May throw if the internal storage mechanism is unavailable or if the provided upstream identifier is invalid.

#### `GetAllSnapshotsAsync`
Retrieves a comprehensive list of health snapshots for all tracked upstreams.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<IReadOnlyList<UpstreamHealthSnapshot>>` containing the current state of every upstream.
*   **Exceptions**: Throws if the health tracker has not been initialized.

#### `GetUnhealthyUpstreamsAsync`
Filters and returns only the upstreams currently marked as unhealthy.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<IReadOnlyList<UpstreamHealthSnapshot>>` containing snapshots where `ProbeSucceeded` is false.
*   **Exceptions**: Throws if the health tracker is in an inconsistent state.

#### `GetHealthyUpstreamsAsync`
Filters and returns only the upstreams currently marked as healthy.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<IReadOnlyList<UpstreamHealthSnapshot>>` containing snapshots where `ProbeSucceeded` is true.
*   **Exceptions**: Throws if the health tracker is in an inconsistent state.

#### `DrainAsync`
Initiates a graceful draining process for upstreams, typically marking them to stop receiving new connections while allowing existing ones to complete.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task` that completes when the drain signal has been propagated.
*   **Exceptions**: May throw if the draining operation fails due to underlying resource constraints.

#### `GetPoolHealthSummariesAsync`
Aggregates health data to provide a summary per upstream pool.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<IReadOnlyList<PoolHealthSummary>>` containing counts of active, unhealthy, draining, disabled, and total servers for each pool.
*   **Exceptions**: Throws if pool metadata is missing or corrupted.

#### `GetSystemHealthSummaryAsync`
Provides a high-level overview of the entire system's health status.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<SystemHealthSummary>` representing the global health state.
*   **Exceptions**: Throws if the system-wide aggregation logic encounters an error.

#### `WaitForHealthyAsync`
Asynchronously waits until at least one upstream in the specified context becomes healthy.
*   **Parameters**: May accept cancellation tokens or timeout configurations depending on implementation context.
*   **Return Value**: Returns a `Task<bool>` indicating whether a healthy state was achieved before timeout or cancellation.
*   **Exceptions**: Throws `OperationCanceledException` if the operation is cancelled.

### Properties and Data Contracts

The following properties represent the data structure of the snapshots and summaries returned by the methods above:

*   **`UpstreamId`** (`string`): The unique identifier for a specific upstream server.
*   **`PoolId`** (`string`): The identifier of the pool to which the upstream belongs.
*   **`PoolName`** (`string`): The human-readable name of the upstream pool.
*   **`ProbeSucceeded`** (`bool`): Indicates whether the last health check probe was successful.
*   **`ResponseTimeMs`** (`int`): The latency of the last probe in milliseconds.
*   **`UpstreamProbeResult`**: An enumeration or object representing the detailed outcome of the probe.
*   **`ActiveServers`** (`int`): The count of servers currently active and serving traffic in a pool.
*   **`UnhealthyServers`** (`int`): The count of servers failing health checks in a pool.
*   **`DrainingServers`** (`int`): The count of servers currently in the draining state.
*   **`DisabledServers`** (`int`): The count of servers explicitly disabled.
*   **`TotalServers`** (`int`): The total number of servers registered in the pool.

## Usage

### Example 1: Monitoring and Alerting on Unhealthy Upstreams
This example demonstrates how to periodically check for unhealthy upstreams and log their details for alerting purposes.

```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Health;

public class HealthMonitor
{
    public async Task CheckAndAlertAsync()
    {
        // Retrieve all currently unhealthy upstreams
        var unhealthyUpstreams = await UpstreamHealthTrackerExtensions.GetUnhealthyUpstreamsAsync();

        if (unhealthyUpstreams.Count > 0)
        {
            Console.WriteLine($"ALERT: {unhealthyUpstreams.Count} upstreams are unhealthy.");
            
            foreach (var snapshot in unhealthyUpstreams)
            {
                Console.WriteLine(
                    $"Upstream {snapshot.UpstreamId} in pool '{snapshot.PoolName}' " +
                    $"failed probe. Last response time: {snapshot.ResponseTimeMs}ms."
                );
            }
        }
        else
        {
            Console.WriteLine("All upstreams are healthy.");
        }
    }
}
```

### Example 2: Waiting for Availability and Retrieving Pool Statistics
This example shows how to block execution until the system is healthy, then retrieve aggregated pool statistics.

```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Health;

public class DeploymentValidator
{
    public async Task ValidateDeploymentAsync()
    {
        Console.WriteLine("Waiting for upstreams to become healthy...");
        
        // Wait until at least one upstream is healthy
        bool isHealthy = await UpstreamHealthTrackerExtensions.WaitForHealthyAsync();

        if (!isHealthy)
        {
            throw new Exception("Deployment validation failed: No healthy upstreams found within timeout.");
        }

        // Get aggregated health summaries for all pools
        var summaries = await UpstreamHealthTrackerExtensions.GetPoolHealthSummariesAsync();

        foreach (var summary in summaries)
        {
            Console.WriteLine($"Pool: {summary.PoolName}");
            Console.WriteLine($"  Active: {summary.ActiveServers}");
            Console.WriteLine($"  Unhealthy: {summary.UnhealthyServers}");
            Console.WriteLine($"  Draining: {summary.DrainingServers}");
            Console.WriteLine($"  Total: {summary.TotalServers}");
        }
    }
}
```

## Notes

*   **Thread Safety**: As the methods are static and asynchronous, the underlying implementation is expected to handle concurrent access to the health state safely. However, callers should treat the returned `IReadOnlyList` instances as snapshots in time; the data may change immediately after the task completes.
*   **Empty Results**: Methods returning lists (`GetAllSnapshotsAsync`, `GetHealthyUpstreamsAsync`, etc.) will return an empty list rather than `null` if no items match the criteria. Callers should check `.Count` rather than performing null checks.
*   **Draining Behavior**: Invoking `DrainAsync` does not immediately remove servers from the pool but marks them to cease accepting new requests. The `DrainingServers` count in `PoolHealthSummary` will reflect this state until the servers are fully removed or re-enabled.
*   **Probe Latency**: The `ResponseTimeMs` property reflects the duration of the last successful or failed probe. If a probe times out, this value may represent the timeout threshold rather than an actual server response time.
*   **Initialization**: These extensions rely on the `UpstreamHealthTracker` being initialized within the application host. Calling these methods prior to service initialization will result in exceptions.
