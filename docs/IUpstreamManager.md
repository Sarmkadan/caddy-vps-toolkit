# IUpstreamManager

`LoadBalancing/IUpstreamManager.cs` is the contract file for upstream load balancing. It declares three interfaces that cover server selection, health tracking, and pool persistence, along with two record types that carry selection context and health reports between them.

The file name suggests a single `IUpstreamManager` interface, but no type with that name exists in the repository. The public API of the file is the set of types listed below. Each interface has its own implementation, documented separately.

The file declares the following public types, all in the `CaddyVpsToolkit.LoadBalancing` namespace:

| Type | Kind | Implemented by | Purpose |
|------|------|----------------|---------|
| `UpstreamSelectionContext` | `record` | n/a | Per-request input for server selection |
| `UpstreamHealthSnapshot` | `record` | n/a | Point-in-time health state of one upstream |
| `UpstreamPoolHealthReport` | `record` | n/a | Aggregated health of a whole pool |
| `IUpstreamSelector` | `interface` | `UpstreamSelector` | Picks one upstream from a candidate list |
| `IUpstreamHealthTracker` | `interface` | `UpstreamHealthTracker` | Records probe results, reports snapshots, drains servers |
| `IUpstreamPoolRepository` | `interface` | `UpstreamPoolRepository` | Persists `UpstreamPool` instances |

The `LoadBalancingStrategy` enum and the `UpstreamPool` and `UpstreamServer` models are referenced by these types but are defined in `Domain/Models/`. They are not repeated here.

## Records

### UpstreamSelectionContext

```csharp
public record UpstreamSelectionContext(
    string PoolId,
    string? ClientIp = null,
    string? SessionToken = null,
    LoadBalancingStrategy? Strategy = null
)
```

Carries the per-request data that a selector needs. Build one for each selection.

**Properties:**
- `PoolId` (`string`): The pool to select from.
- `ClientIp` (`string?`): The client address. Required by `LoadBalancingStrategy.IpHash` for stable pinning. Ignored by other strategies.
- `SessionToken` (`string?`): An opaque session token for sticky-session logic that re-pins a session to its earlier upstream.
- `Strategy` (`LoadBalancingStrategy?`): The strategy to apply. `null` means `RoundRobin`.

### UpstreamHealthSnapshot

```csharp
public record UpstreamHealthSnapshot(
    string UpstreamId,
    string Address,
    bool IsHealthy,
    UpstreamServerStatus Status,
    int ConsecutiveFailures,
    int AverageResponseTimeMs,
    int ActiveConnections,
    DateTime? LastCheckedAt
)
```

An immutable copy of one server's health and connection metrics, taken at a single moment. Changing the live server does not change a snapshot that was already taken.

**Properties:**
- `UpstreamId` (`string`): The server's identifier.
- `Address` (`string`): The address in `host:port` form.
- `IsHealthy` (`bool`): Whether the server was considered healthy when the snapshot was taken.
- `Status` (`UpstreamServerStatus`): The operational status: `Active`, `Draining`, `Disabled`, or `Unhealthy`.
- `ConsecutiveFailures` (`int`): The number of failed probes in a row leading up to the snapshot.
- `AverageResponseTimeMs` (`int`): The rolling average probe round-trip time in milliseconds.
- `ActiveConnections` (`int`): The number of in-flight connections.
- `LastCheckedAt` (`DateTime?`): The UTC time of the most recent probe, or `null` if the server has never been probed.

### UpstreamPoolHealthReport

```csharp
public record UpstreamPoolHealthReport(
    string PoolId,
    string PoolName,
    LoadBalancingStrategy Strategy,
    int TotalUpstreams,
    int HealthyUpstreams,
    int AvailableUpstreams,
    int TotalActiveConnections,
    IReadOnlyList<UpstreamHealthSnapshot> Upstreams,
    DateTime GeneratedAt
)
```

A report for an entire pool, suitable for dashboards, alerts, and remediation logic. It includes two computed properties.

**Properties:**
- `PoolId`, `PoolName` (`string`): The pool's identifier and display name.
- `Strategy` (`LoadBalancingStrategy`): The strategy the pool currently uses.
- `TotalUpstreams` (`int`): The number of configured servers.
- `HealthyUpstreams` (`int`): The number of servers reporting as healthy.
- `AvailableUpstreams` (`int`): The number of servers that can receive traffic now.
- `TotalActiveConnections` (`int`): The sum of in-flight connections across the pool.
- `Upstreams` (`IReadOnlyList<UpstreamHealthSnapshot>`): One snapshot per server, in pool order.
- `GeneratedAt` (`DateTime`): The UTC time the report was produced.

**Computed properties:**
- `HealthRatio` (`double`): `HealthyUpstreams / TotalUpstreams`, in the range `[0.0, 1.0]`. Returns `0.0` when the pool is empty.
- `IsOperational` (`bool`): `true` when `AvailableUpstreams > 0`.
- `IsFullyHealthy` (`bool`): `true` when every server is healthy and there is at least one server.

## Interfaces

### IUpstreamSelector

```csharp
public interface IUpstreamSelector
```

Chooses the next upstream from a list of candidates. Implementations should not keep state about the candidate list. State such as a round-robin cursor is held by the implementation and keyed by pool, as `UpstreamSelector` does.

#### Select

```csharp
UpstreamServer? Select(IReadOnlyList<UpstreamServer> servers, UpstreamSelectionContext context)
```

Returns one server from `servers` using the strategy in `context`.

**Parameters:**
- `servers` (`IReadOnlyList<UpstreamServer>`): The candidate servers. Filter this list to servers where `IsAvailable()` returns `true` before calling.
- `context` (`UpstreamSelectionContext`): The per-request data.

**Returns:**
- `UpstreamServer?`: The chosen server, or `null` when `servers` is empty.

### IUpstreamHealthTracker

```csharp
public interface IUpstreamHealthTracker
```

Applies probe results to the health state of servers. A server becomes `Unhealthy` after `UnhealthyThreshold` consecutive failures. It returns to `Active` after `HealthyThreshold` consecutive successes. Both thresholds are set on the `UpstreamPool`.

#### RecordProbeResultAsync

```csharp
Task RecordProbeResultAsync(string upstreamId, string poolId, bool probeSucceeded, int responseTimeMs = 0)
```

Records one probe result and updates the server's status if a threshold is crossed. The pool is loaded and saved through `IUpstreamPoolRepository`. If the pool or server does not exist, the call returns without error.

**Parameters:**
- `upstreamId` (`string`): The server that was probed.
- `poolId` (`string`): The pool that owns the server. It supplies the thresholds.
- `probeSucceeded` (`bool`): Whether the probe succeeded.
- `responseTimeMs` (`int`): The probe round-trip time. Use `0` for failed probes. Defaults to `0`.

#### GetSnapshotAsync

```csharp
Task<UpstreamHealthSnapshot?> GetSnapshotAsync(string upstreamId)
```

Returns the current health of one server, searching all pools.

**Parameters:**
- `upstreamId` (`string`): The server to look up.

**Returns:**
- `Task<UpstreamHealthSnapshot?>`: The snapshot, or `null` if no pool contains the server.

#### DrainAsync

```csharp
Task DrainAsync(string upstreamId, TimeSpan drainTimeout, CancellationToken cancellationToken = default)
```

Stops new traffic to a server and waits for its in-flight connections to finish. The server is set to `Draining` first. The method then waits, polling every 250 ms, until `ActiveConnections` reaches zero or `drainTimeout` passes. When the wait ends, the server is set to `Disabled` and the pool is saved.

**Parameters:**
- `upstreamId` (`string`): The server to drain.
- `drainTimeout` (`TimeSpan`): The longest time to wait for connections to finish.
- `cancellationToken` (`CancellationToken`): Stops the wait early. Defaults to `default`.

**Exceptions:**
- `OperationCanceledException`: Thrown when `cancellationToken` is cancelled during the wait.

### IUpstreamPoolRepository

```csharp
public interface IUpstreamPoolRepository
```

Persistence for `UpstreamPool` instances. This lets pool configuration survive restarts.

#### GetByIdAsync

```csharp
Task<UpstreamPool?> GetByIdAsync(string poolId)
```

Returns one pool, or `null` if no pool has that identifier.

#### GetByServiceIdAsync

```csharp
Task<List<UpstreamPool>> GetByServiceIdAsync(string serviceId)
```

Returns all pools linked to a service. Returns an empty list when there are none.

#### GetAllAsync

```csharp
Task<List<UpstreamPool>> GetAllAsync()
```

Returns every stored pool. Returns an empty list when there are none.

#### AddAsync

```csharp
Task<string> AddAsync(UpstreamPool pool)
```

Stores a new pool and returns its identifier.

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(UpstreamPool pool)
```

Overwrites an existing pool. Returns `true` when the pool was found and updated, and `false` when no pool matched.

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(string poolId)
```

Removes a pool. Returns `true` on success.

#### ExistsAsync

```csharp
Task<bool> ExistsAsync(string poolId)
```

Returns `true` when a pool with that identifier is stored.

## Usage Examples

### Example 1: Selecting an Upstream for a Request

Filter the pool to available servers, then ask the selector for one. `IpHash` keeps a given client on the same server.

```csharp
public sealed class ProxyRouter
{
    private readonly IUpstreamPoolRepository _pools;
    private readonly IUpstreamSelector _selector;

    public ProxyRouter(IUpstreamPoolRepository pools, IUpstreamSelector selector)
    {
        _pools = pools;
        _selector = selector;
    }

    public async Task<UpstreamServer?> RouteAsync(string poolId, string clientIp)
    {
        UpstreamPool? pool = await _pools.GetByIdAsync(poolId);
        if (pool is null)
        {
            return null;
        }

        List<UpstreamServer> candidates = pool.Servers.Where(s => s.IsAvailable()).ToList();

        var context = new UpstreamSelectionContext(
            PoolId: poolId,
            ClientIp: clientIp,
            Strategy: LoadBalancingStrategy.IpHash);

        return _selector.Select(candidates, context);
    }
}
```

When `candidates` is empty, `Select` returns `null`. The caller must handle that case, for example by returning a 503 response.

### Example 2: Recording Health Probes

A background probe loop reports each result to the tracker. The tracker decides when a server changes status.

```csharp
public async Task ProbeUpstreamAsync(
    IUpstreamHealthTracker tracker,
    string poolId,
    string upstreamId,
    HttpClient client,
    Uri probeUri)
{
    var stopwatch = Stopwatch.StartNew();
    bool succeeded;

    try
    {
        using HttpResponseMessage response = await client.GetAsync(probeUri);
        succeeded = response.IsSuccessStatusCode;
    }
    catch (HttpRequestException)
    {
        succeeded = false;
    }

    int elapsedMs = succeeded ? (int)stopwatch.ElapsedMilliseconds : 0;
    await tracker.RecordProbeResultAsync(upstreamId, poolId, succeeded, elapsedMs);

    UpstreamHealthSnapshot? snapshot = await tracker.GetSnapshotAsync(upstreamId);
    if (snapshot is { IsHealthy: false })
    {
        Console.WriteLine($"{snapshot.Address} is unhealthy after {snapshot.ConsecutiveFailures} failures");
    }
}
```

### Example 3: Draining a Server Before Maintenance

Drain the server first so that new traffic stops and current requests can finish. Use a cancellation token so that a shutdown signal can end the wait early.

```csharp
public async Task PrepareForMaintenanceAsync(
    IUpstreamHealthTracker tracker,
    string upstreamId,
    CancellationToken shutdownToken)
{
    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);
    timeout.CancelAfter(TimeSpan.FromMinutes(2));

    try
    {
        await tracker.DrainAsync(upstreamId, TimeSpan.FromMinutes(1), timeout.Token);
    }
    catch (OperationCanceledException)
    {
        // The wait was cut short. Connections may still be open.
    }
}
```

### Example 4: Building a Pool Health Report

Combine snapshots into one `UpstreamPoolHealthReport` for a dashboard or alert. The computed properties give quick checks.

```csharp
public async Task<UpstreamPoolHealthReport> BuildReportAsync(
    IUpstreamPoolRepository pools,
    IUpstreamHealthTracker tracker,
    string poolId)
{
    UpstreamPool pool = await pools.GetByIdAsync(poolId)
        ?? throw new InvalidOperationException($"Pool '{poolId}' was not found.");

    var snapshots = new List<UpstreamHealthSnapshot>();
    foreach (UpstreamServer server in pool.Servers)
    {
        UpstreamHealthSnapshot? snapshot = await tracker.GetSnapshotAsync(server.Id);
        if (snapshot is not null)
        {
            snapshots.Add(snapshot);
        }
    }

    return new UpstreamPoolHealthReport(
        PoolId: pool.Id,
        PoolName: pool.Name,
        Strategy: pool.Strategy,
        TotalUpstreams: snapshots.Count,
        HealthyUpstreams: snapshots.Count(s => s.IsHealthy),
        AvailableUpstreams: snapshots.Count(s => s.Status == UpstreamServerStatus.Active && s.IsHealthy),
        TotalActiveConnections: snapshots.Sum(s => s.ActiveConnections),
        Upstreams: snapshots,
        GeneratedAt: DateTime.UtcNow);
}
```

```csharp
UpstreamPoolHealthReport report = await BuildReportAsync(pools, tracker, "api-pool");

if (!report.IsOperational)
{
    Console.WriteLine("No upstreams can take traffic");
}
else if (!report.IsFullyHealthy)
{
    Console.WriteLine($"Pool is degraded: {report.HealthRatio:P0} healthy");
}
```

## Notes

- `UpstreamSelectionContext`, `UpstreamHealthSnapshot`, and `UpstreamPoolHealthReport` are `record` types. Their equality is by value, and they can be used with `with` expressions.
- The selector reads from the candidate list you pass in. It does not check availability. Filter with `IsAvailable()` first, as shown in Example 1.
- The tracker writes changes back through `IUpstreamPoolRepository`, so probe results and drain state are persisted with each call.
