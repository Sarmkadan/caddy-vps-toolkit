# UpstreamSelector

UpstreamSelector is the stateless-looking strategy engine that picks one upstream server from a list of candidates. It implements `IUpstreamSelector` and supports five strategies: round-robin, least-connections, random, weighted-random, and IP-hash. You pass it the candidate servers and a `UpstreamSelectionContext` that names the pool, the client IP, and the desired strategy. It returns the chosen `UpstreamServer`.

The source lives in `LoadBalancing/UpstreamSelector.cs`. It contains one public type:

- `UpstreamSelector` is a `sealed` class that implements `IUpstreamSelector`. It holds only the per-pool round-robin cursors. Every other strategy is computed from the arguments of each call.

The interface and the context record are defined in `LoadBalancing/IUpstreamManager.cs`. The strategy enum is `LoadBalancingStrategy` in `Domain/Models/UpstreamPool.cs`. Those types are not repeated here.

## Registration

`UpstreamSelector` is registered as a singleton by the upstream services extensions:

```csharp
services.AddSingleton<IUpstreamSelector, UpstreamSelector>();
```

Keep it a singleton. The round-robin cursors live on the instance, so a new instance per request would reset the rotation every time.

## Strategies

| Strategy | Chosen by | Notes |
|----------|-----------|-------|
| `RoundRobin` (default) | Per-pool atomic cursor | Even rotation across the candidate list. Used when `Strategy` is `null` or not a known value. |
| `LeastConnections` | Lowest `UpstreamServer.ActiveConnections` | Ties go to the first server in the list. |
| `Random` | `Random.Shared` | Uniform over the candidate list. |
| `WeightedRandom` | `UpstreamServer.Weight` | Probability is proportional to `Weight`. Falls back to uniform when the total weight is `0` or less. |
| `IpHash` | FNV-1a hash of `ClientIp` | Stable mapping from client to server. Falls back to round-robin when `ClientIp` is empty. |

## API

### `UpstreamSelector`

#### `UpstreamSelector()`

Creates a selector with no round-robin state. Each pool gets its cursor the first time it is used.

- **Parameters**: None.
- **Exceptions**: None.

#### `UpstreamServer? Select(IReadOnlyList<UpstreamServer> servers, UpstreamSelectionContext context)`

Picks one server from `servers` using the strategy in `context`.

- **Parameters**
  - `servers`: Candidate servers. The selector does not filter by availability, so pass only servers that are eligible. Use `UpstreamServer.IsAvailable()` to pre-filter.
  - `context`: Pool identifier, optional client IP, optional session token, and optional strategy. `SessionToken` is not read by this selector. See the behaviour notes below.
- **Return value**: The selected `UpstreamServer`. Returns `null` when `servers` is `null` or empty. When `servers` has exactly one element, that element is returned without consulting the strategy.
- **Exceptions**
  - `ArgumentNullException` if `context` is `null` and `servers` is not `null`.
  - `ArgumentNullException` if any element of `servers` is `null`.
  - `ArgumentException` if `context.ClientIp` is whitespace only and the effective strategy is `IpHash`.
  - `ArgumentNullException` if `context.PoolId` is `null` on the round-robin path, because the cursor dictionary does not accept `null` keys.

The XML comments on the method describe `ArgumentNullException` for a `null` `servers`. The code returns `null` in that case instead, so this document follows the code.

## Usage Examples

### Resolving the selector

Inject the interface. The singleton from the registration above is shared across the application.

```csharp
public sealed class UpstreamRouter
{
    private readonly IUpstreamSelector _selector;

    public UpstreamRouter(IUpstreamSelector selector)
    {
        _selector = selector;
    }
}
```

### Round-robin across a pool

Leaving `Strategy` as `null` gives round-robin. Each call advances the cursor for `PoolId`.

```csharp
var servers = new List<UpstreamServer>
{
    new() { Address = "10.0.0.11", Port = 8080 },
    new() { Address = "10.0.0.12", Port = 8080 },
    new() { Address = "10.0.0.13", Port = 8080 }
};

var selector = new UpstreamSelector();
var context = new UpstreamSelectionContext("web-pool");

for (var i = 0; i < 4; i++)
{
    var server = selector.Select(servers, context);
    Console.WriteLine(server!.GetUpstreamAddress());
}
// 10.0.0.11:8080, 10.0.0.12:8080, 10.0.0.13:8080, 10.0.0.11:8080
```

### Least-connections

The server with the fewest in-flight connections wins. The caller is responsible for incrementing and decrementing `ActiveConnections` around each proxied request. It is a plain `int` property, so `Interlocked` cannot target it. Update it under your own lock if requests run concurrently.

```csharp
var context = new UpstreamSelectionContext("api-pool", Strategy: LoadBalancingStrategy.LeastConnections);

var target = selector.Select(servers, context);
if (target is not null)
{
    lock (target)
    {
        target.ActiveConnections++;
    }

    try
    {
        // proxy the request to target.GetUpstreamAddress()
    }
    finally
    {
        lock (target)
        {
            target.ActiveConnections--;
        }
    }
}
```

### Weighted random

A server with `Weight = 3` receives about three times the traffic of a server with `Weight = 1`.

```csharp
var servers = new List<UpstreamServer>
{
    new() { Address = "10.0.0.21", Port = 8080, Weight = 3 }, // large node
    new() { Address = "10.0.0.22", Port = 8080, Weight = 1 }  // small node
};

var context = new UpstreamSelectionContext("media-pool", Strategy: LoadBalancingStrategy.WeightedRandom);
var server = selector.Select(servers, context);
```

### Sticky routing by client IP

`IpHash` returns the same server for the same IP as long as the server list keeps the same order and length.

```csharp
var context = new UpstreamSelectionContext(
    PoolId: "app-pool",
    ClientIp: "203.0.113.42",
    Strategy: LoadBalancingStrategy.IpHash);

var first = selector.Select(servers, context);
var second = selector.Select(servers, context);
// first and second refer to the same server
```

### Pool strategy from configuration

`UpstreamSelector` does not read `UpstreamPool.Strategy`. Copy it into the context yourself.

```csharp
var pool = await poolRepository.GetByIdAsync("web-pool");
if (pool is null)
    return null;

var context = new UpstreamSelectionContext(pool.Id, Strategy: pool.Strategy);

var candidates = pool.Servers.Where(s => s.IsAvailable()).ToList();
var server = selector.Select(candidates, context);
```

## Behaviour Notes

- **No availability filter.** `Select` uses every server in `servers`, including unhealthy or disabled ones. Filter with `IsAvailable()` before calling it. If the filtered list is empty, the result is `null`.
- **Single-server shortcut.** With one candidate, the server is returned immediately. The strategy and `ClientIp` are not checked. Null-entry validation still runs first.
- **Null entries are rejected.** The whole list is scanned for `null` entries before any strategy runs, so one `null` in the list throws even when another strategy would have skipped it.
- **Round-robin cursor is per `PoolId`.** Two pools never share a cursor. The cursor is not reset when the server list changes. The index is taken modulo the current list length, so removing or adding servers shifts the rotation but never goes out of range.
- **Round-robin is also the fallback.** `IpHash` without a client IP and any unknown strategy value both use the same cursor as `RoundRobin` for that pool.
- **IP-hash mapping depends on the list.** The server is `hash % servers.Count`. Adding, removing, or reordering servers can move existing clients to a different upstream. Use `AdaptiveLoadBalancer` or your own consistent-hashing scheme if you need stability across topology changes.
- **`ClientIp` checks are not symmetric.** An empty `ClientIp` falls back to round-robin. A whitespace-only `ClientIp` passes the emptiness check and then throws `ArgumentException`.
- **`SessionToken` is ignored.** Sticky-session re-pinning is not implemented in this class.
- **`WeightedRandom` uses `Weight` as given.** Values are not clamped here. Weights below `1` are summed as they are. If the total is `0` or less, selection is uniform. Mixed negative and positive weights can produce a total greater than zero and an unexpected distribution.
- **`UpstreamManagerService` does not use this selector.** It has its own private selection switch over `pool.Strategy`. `IUpstreamSelector` is only registered in DI and is not injected anywhere in the non-test code, so changing `UpstreamSelector` does not change routing in `UpstreamManagerService`.
- **Thread safety.** The round-robin cursor update is guarded by a lock and a `ConcurrentDictionary`. The other strategies read `ActiveConnections` and `Weight` without locking. Those fields are plain `int` properties, so reads are safe but may be stale under concurrent updates.
