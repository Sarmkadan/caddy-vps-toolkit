# IAdaptiveLoadBalancer

`IAdaptiveLoadBalancer` is the contract for health-aware upstream selection. It ranks every server in an upstream pool by a composite score, reports whether each server is eligible for traffic, records the outcome of each routed request, exposes the current adaptive weight of an upstream, and resets adaptive state for a pool. Consumers depend on this interface rather than on `AdaptiveLoadBalancer`, the shipped implementation described in [AdaptiveLoadBalancer](AdaptiveLoadBalancer.md).

The interface is declared in `LoadBalancing/IAdaptiveLoadBalancer.cs` under the `CaddyVpsToolkit.LoadBalancing` namespace. The same file also declares the supporting types `UpstreamRoutingScore`, `PoolRoutingEvaluation`, and the sample-window contract `IMetricsAggregator`. Those are documented in the API section below because the interface's return values use them.

## Types in the File

| Type | Kind | Purpose |
|------|------|---------|
| `IAdaptiveLoadBalancer` | interface | Scores pools, records outcomes, reports weights, and recalibrates state. |
| `UpstreamRoutingScore` | record | Per-upstream score, effective weight, and eligibility for one evaluation pass. |
| `PoolRoutingEvaluation` | record | Full ranking for one pool plus the selected upstream identifier. |
| `IMetricsAggregator` | interface | Sliding-window sample store that feeds the scoring model. |

All three records are positional `record` types, so they are immutable and compare by value. `PoolRoutingEvaluation` adds one computed member, `HasEligibleUpstream`.

## API

### IAdaptiveLoadBalancer

```csharp
public interface IAdaptiveLoadBalancer
```

Every member returns a `Task` or `Task<T>`, so callers `await` them. `GetEffectiveWeightAsync` returns an already-completed task in the shipped implementation, because it only reads in-memory state. `AdaptiveLoadBalancer` guards its adaptive-weight and penalty updates with locks, so one instance can be shared across concurrent requests.

### EvaluatePoolAsync

```csharp
Task<PoolRoutingEvaluation> EvaluatePoolAsync(
    UpstreamSelectionContext context,
    CancellationToken cancellationToken = default)
```

Evaluates every candidate in the pool named by `context.PoolId` and returns the full ranking together with the upstream chosen for this request. Ineligible candidates are included in the result with `IsEligible` set to `false`, so operators can see why a server receives no traffic.

**Parameters:**
- `context` (`UpstreamSelectionContext`): Carries `PoolId`, which selects the pool. `ClientIp`, `SessionToken`, and `Strategy` are per-request hints. Must not be `null`.
- `cancellationToken` (`CancellationToken`): Aborts the evaluation. Defaults to `default`.

**Returns:**
- `Task<PoolRoutingEvaluation>`: The ranking and the selected upstream. `SelectedUpstreamId` is `null` when no candidate is eligible.

**Exceptions:**
- `ArgumentNullException`: Thrown by `AdaptiveLoadBalancer` when `context` is `null`.
- `OperationCanceledException`: Thrown when `cancellationToken` is cancelled.

### RecordOutcomeAsync

```csharp
Task RecordOutcomeAsync(string poolId, string upstreamId, int responseTimeMs, bool succeeded)
```

Records one completed request for the upstream. The observation feeds the sliding-window metrics, and the adaptive weight of the upstream moves toward a success or a penalty target. A failure also restarts the time-decaying penalty for that upstream. Call this for every routed request. Without it the balancer never learns about the upstream's performance.

**Parameters:**
- `poolId` (`string`): The pool that owns the upstream. Must not be null, empty, or whitespace.
- `upstreamId` (`string`): The upstream that handled the request. Must not be null, empty, or whitespace.
- `responseTimeMs` (`int`): End-to-end response time in milliseconds.
- `succeeded` (`bool`): `true` if the request completed without error; otherwise `false`.

**Returns:**
- `Task`: Completes when the outcome has been recorded.

**Exceptions:**
- `ArgumentException`: Thrown by `AdaptiveLoadBalancer` when `poolId` or `upstreamId` is null, empty, or whitespace.

### GetEffectiveWeightAsync

```csharp
Task<int> GetEffectiveWeightAsync(string upstreamId)
```

Returns the current effective weight for the upstream. The value reflects the adaptive multiplier that has accumulated from recorded outcomes.

**Parameters:**
- `upstreamId` (`string`): The upstream's identifier. Must not be null, empty, or whitespace.

**Returns:**
- `Task<int>`: A positive integer, never less than `1`. An upstream with no recorded outcomes reports the neutral value.

**Exceptions:**
- `ArgumentException`: Thrown by `AdaptiveLoadBalancer` when `upstreamId` is null, empty, or whitespace.

> **Note:** The interface comment says the weight is derived from the configured base weight. `AdaptiveLoadBalancer` does not do that. It returns `max(1, round(multiplier * 100))`, so a neutral upstream reports `100`, and the value does not include `UpstreamServer.Weight`. Use `EvaluatePoolAsync` and `UpstreamRoutingScore.EffectiveWeight` if you need the weight that includes the configured base weight.

### RecalibratePoolAsync

```csharp
Task RecalibratePoolAsync(string poolId, CancellationToken cancellationToken = default)
```

Discards all adaptive state for every server in the pool: sample windows, adaptive multipliers, and failure penalties. Afterwards every server is treated as equal until new outcomes arrive. Call this after you add or remove backend servers, because the old observations no longer describe the current topology.

**Parameters:**
- `poolId` (`string`): The pool to recalibrate. Must not be null, empty, or whitespace.
- `cancellationToken` (`CancellationToken`): Aborts the operation. Defaults to `default`.

**Returns:**
- `Task`: Completes when the state has been cleared. If the pool does not exist, the implementation returns without error.

**Exceptions:**
- `ArgumentException`: Thrown by `AdaptiveLoadBalancer` when `poolId` is null, empty, or whitespace.
- `OperationCanceledException`: Thrown when `cancellationToken` is cancelled.

### UpstreamRoutingScore

```csharp
public record UpstreamRoutingScore(
    string UpstreamId,
    double NormalizedScore,
    int    EffectiveWeight,
    double LatencyScore,
    double ErrorRateScore,
    double ConnectionScore,
    bool   IsEligible)
```

The scoring result for one upstream in one evaluation pass.

| Property | Type | Meaning |
|----------|------|---------|
| `UpstreamId` | `string` | The upstream's unique identifier. |
| `NormalizedScore` | `double` | Composite score in `[0.0, 1.0]`. `1.0` is the best possible value. |
| `EffectiveWeight` | `int` | Configured weight combined with the adaptive adjustments. Breaks ties between equal scores. |
| `LatencyScore` | `double` | Latency contribution in `[0.0, 1.0]`. Higher means lower observed latency. |
| `ErrorRateScore` | `double` | Error-rate contribution in `[0.0, 1.0]`. Higher means fewer errors. |
| `ConnectionScore` | `double` | Connection-pressure contribution in `[0.0, 1.0]`. Higher means fewer in-flight connections. |
| `IsEligible` | `bool` | `true` if the upstream passed the health and circuit-breaker gates. Ineligible scores are never selected. |

### PoolRoutingEvaluation

```csharp
public record PoolRoutingEvaluation(
    string PoolId,
    IReadOnlyList<UpstreamRoutingScore> Scores,
    string? SelectedUpstreamId,
    DateTime EvaluatedAt)
```

The result of one call to `EvaluatePoolAsync`.

| Property | Type | Meaning |
|----------|------|---------|
| `PoolId` | `string` | The pool that was evaluated. |
| `Scores` | `IReadOnlyList<UpstreamRoutingScore>` | Every candidate, ordered by `NormalizedScore` descending. Includes ineligible candidates. |
| `SelectedUpstreamId` | `string?` | The first eligible candidate in the ranking, or `null` when no candidate is eligible. |
| `EvaluatedAt` | `DateTime` | UTC timestamp of the evaluation. |

#### HasEligibleUpstream

```csharp
public bool HasEligibleUpstream { get; }
```

Returns `true` when `SelectedUpstreamId` is not `null`. Check this before you forward a request.

### IMetricsAggregator

```csharp
public interface IMetricsAggregator
```

The sample store that the scoring model reads. `SlidingWindowMetricsAggregator` is the shipped implementation. The interface is declared in the same file as `IAdaptiveLoadBalancer`.

#### Record

```csharp
void Record(string upstreamId, int responseTimeMs, bool succeeded)
```

Adds one observation to the upstream's sliding window.

#### GetSummary

```csharp
UpstreamMetricsSummary? GetSummary(string upstreamId)
```

Returns an immutable `UpstreamMetricsSummary` for the upstream, or `null` when no samples exist. See [UpstreamMetricsSummary](UpstreamMetricsSummary.md).

#### Reset

```csharp
void Reset(string upstreamId)
```

Clears the upstream's window. After this, `GetSummary` returns `null` until new samples arrive.

## Usage

### Example 1: Select an Upstream for a Request

```csharp
using CaddyVpsToolkit.LoadBalancing;

public sealed class ProxyRouter
{
    private readonly IAdaptiveLoadBalancer _balancer;

    public ProxyRouter(IAdaptiveLoadBalancer balancer)
    {
        _balancer = balancer;
    }

    public async Task<string?> PickUpstreamAsync(string poolId, string clientIp, CancellationToken ct)
    {
        var context = new UpstreamSelectionContext(PoolId: poolId, ClientIp: clientIp);

        PoolRoutingEvaluation evaluation = await _balancer.EvaluatePoolAsync(context, ct);

        if (!evaluation.HasEligibleUpstream)
        {
            // Every candidate is down, degraded, or circuit-broken
            return null;
        }

        return evaluation.SelectedUpstreamId;
    }
}
```

### Example 2: Record the Outcome of Each Request

`RecordOutcomeAsync` must run for every request, including failed ones. A skipped failure leaves the upstream looking healthy.

```csharp
public async Task ForwardAsync(
    IAdaptiveLoadBalancer balancer,
    string poolId,
    string upstreamId,
    Func<Task> sendRequest)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    bool succeeded = false;

    try
    {
        await sendRequest();
        succeeded = true;
    }
    finally
    {
        // Runs on success and on failure
        await balancer.RecordOutcomeAsync(
            poolId,
            upstreamId,
            (int)stopwatch.ElapsedMilliseconds,
            succeeded);
    }
}
```

### Example 3: Inspect the Scoring Rationale

The full ranking is useful for diagnostics, such as an admin endpoint or a log line when a server is excluded.

```csharp
PoolRoutingEvaluation evaluation = await balancer.EvaluatePoolAsync(
    new UpstreamSelectionContext(PoolId: "web-frontends"));

foreach (UpstreamRoutingScore score in evaluation.Scores)
{
    string state = score.IsEligible ? "eligible" : "excluded";
    Console.WriteLine(
        $"{score.UpstreamId,-20} score={score.NormalizedScore:F3} " +
        $"weight={score.EffectiveWeight} {state}");
}

Console.WriteLine($"Selected: {evaluation.SelectedUpstreamId ?? "none"}");
```

### Example 4: Recalibrate After a Topology Change

```csharp
// A backend was added to the pool. Discard the old observations so
// the new server is not compared against stale numbers.
await balancer.RecalibratePoolAsync("web-frontends");

int weight = await balancer.GetEffectiveWeightAsync("web-frontends-03");
Console.WriteLine($"Neutral effective weight: {weight}");
```

### Example 5: Substituting the Interface in Tests

Because consumers depend on `IAdaptiveLoadBalancer`, a test can return a fixed evaluation without a real pool or metrics store.

```csharp
using NSubstitute;

var balancer = Substitute.For<IAdaptiveLoadBalancer>();

var evaluation = new PoolRoutingEvaluation(
    PoolId: "web-frontends",
    Scores: [
        new UpstreamRoutingScore("web-01", 0.9, 100, 1.0, 1.0, 0.8, IsEligible: true),
        new UpstreamRoutingScore("web-02", 0.0, 100, 0.0, 0.0, 0.0, IsEligible: false)
    ],
    SelectedUpstreamId: "web-01",
    EvaluatedAt: DateTime.UtcNow);

balancer
    .EvaluatePoolAsync(Arg.Any<UpstreamSelectionContext>(), Arg.Any<CancellationToken>())
    .Returns(Task.FromResult(evaluation));
```
