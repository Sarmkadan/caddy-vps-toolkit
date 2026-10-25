# AdaptiveLoadBalancer

AdaptiveLoadBalancer is the scoring engine behind health-aware upstream selection in the Caddy VPS Toolkit. For each pool it ranks every upstream server by a composite score built from three live signals: p99 latency, rolling error rate, and active-connection pressure. It also keeps a per-upstream adaptive weight multiplier and a time-decaying failure penalty, so a server that starts failing loses traffic quickly and regains it gradually once it recovers.

The source lives in `LoadBalancing/AdaptiveLoadBalancer.cs` and contains two public types:

- `AdaptiveLoadBalancer` implements `IAdaptiveLoadBalancer`. It scores pools and tracks adaptive state.
- `SlidingWindowMetricsAggregator` implements `IMetricsAggregator`. It keeps a bounded sample window per upstream. See also [SlidingWindowMetricsAggregator](SlidingWindowMetricsAggregator.md) for the aggregator on its own.

Both types are `sealed`. Their public members are documented below.

## Scoring Model

For each server in a pool, `EvaluatePoolAsync` computes:

1. **Latency score** in `[0.0, 1.0]`. If the upstream has no statistically significant metrics (fewer than five samples), the score is `1.0`. Otherwise, with `ratio = P99 / TargetLatencyMs`, the score is `clamp(1 - (ratio - 1) / 9, 0, 1)`. A p99 at or below the target scores `1.0`, and the score reaches `0.0` at ten times the target.
2. **Error-rate score** in `[0.0, 1.0]`: `1 - ErrorRate`. With no samples at all, the score is `1.0`.
3. **Connection score** in `[0.0, 1.0]`: `1 - ActiveConnections / MaxExpectedConnections`, floored at `0.0`. Zero or fewer active connections score `1.0`.

These are combined into a single value:

```
raw        = LatencyWeight * latencyScore + ErrorRateWeight * errorRateScore + ConnectionWeight * connectionScore
normalised = clamp(raw * penaltyFactor * halfOpenFactor, 0.0, 1.0)
```

- `penaltyFactor` is the time-decaying failure penalty (see below). It is `1.0` when no penalty is active.
- `halfOpenFactor` is `HalfOpenPenaltyMultiplier` for servers in `UpstreamServerStatus.HalfOpen`, and `1.0` otherwise.

A candidate is **eligible** when `server.IsAvailable()` is `true` and `normalised > 0.0`. Ineligible candidates are still returned in the ranking so operators can see why a server is not receiving traffic.

Each candidate also gets an **effective weight**: `max(1, round(server.Weight * adaptiveMultiplier))`. Scores are sorted by `NormalizedScore` descending, with `EffectiveWeight` descending as the tie-breaker. The first eligible candidate in that order becomes `SelectedUpstreamId`.

### Adaptive Weight Multiplier

Each upstream has a multiplier that starts at `1.0` (neutral) and is updated on every recorded outcome:

- On the **first** outcome for an upstream, the multiplier is set directly to `1.0` for a success or `PenaltyMultiplier` for a failure.
- On later outcomes, it moves by an exponential moving average: `current * (1 - WeightAdaptationAlpha) + target * WeightAdaptationAlpha`, where `target` is `1.0` for a success and `PenaltyMultiplier` for a failure.

### Failure Penalty

Every failure stamps the upstream with the current UTC time. Until `PenaltyDecaySeconds` have passed since that stamp, the normalised score is multiplied by a factor that rises linearly from `PenaltyMultiplier` back to `1.0`. Each new failure restarts the decay. Expired penalty entries are removed the next time the upstream is scored.

## API

### `SlidingWindowMetricsAggregator`

#### `SlidingWindowMetricsAggregator(int windowSize = 200)`

Creates an aggregator that keeps up to `windowSize` request samples per upstream. Older samples are evicted once the limit is reached.

- **Parameters**
  - `windowSize`: Maximum number of samples retained per upstream. Defaults to `200`.
- **Exceptions**: None. The value is not validated here, but each per-upstream window rejects values below `1` when it is first created, so a `windowSize` below `1` causes the first `Record` call to throw `ArgumentOutOfRangeException`.

#### `void Record(string upstreamId, int responseTimeMs, bool succeeded)`

Adds one observation to the upstream's window, creating the window on first use.

- **Parameters**
  - `upstreamId`: Upstream identifier.
  - `responseTimeMs`: Observed response time in milliseconds. Negative values are clamped to `0` by the window.
  - `succeeded`: Whether the request succeeded.
- **Return value**: None.
- **Exceptions**: Throws `ArgumentException` if `upstreamId` is null, empty, or whitespace.

#### `UpstreamMetricsSummary? GetSummary(string upstreamId)`

Returns a snapshot of the upstream's current window: sample count, p50/p95/p99 latency, mean latency, error rate, and throughput.

- **Parameters**
  - `upstreamId`: Upstream identifier.
- **Return value**: An `UpstreamMetricsSummary`, or `null` if nothing has been recorded for this upstream or its window is empty.
- **Exceptions**: Throws `ArgumentException` if `upstreamId` is null, empty, or whitespace.

#### `void Reset(string upstreamId)`

Clears the upstream's window. The window object itself stays registered, and later `GetSummary` calls return `null` until new samples arrive.

- **Parameters**
  - `upstreamId`: Upstream identifier.
- **Return value**: None.
- **Exceptions**: Throws `ArgumentException` if `upstreamId` is null, empty, or whitespace. Does nothing for an unknown upstream.

### `AdaptiveLoadBalancer`

#### `AdaptiveLoadBalancer(UpstreamManagerService upstreamManager, IMetricsAggregator metrics, UpstreamManagementOptions options)`

Creates a load balancer from its three collaborators.

- **Parameters**
  - `upstreamManager`: Source of pool and server state used for candidate enumeration.
  - `metrics`: Source of per-upstream samples used for latency and error-rate scoring. Use the same instance for every consumer so that all of them see the same data.
  - `options`: Scoring weights, latency target, adaptation speed, and penalty settings. See [UpstreamManagementOptions](UpstreamManagementOptions.md) for the properties.
- **Exceptions**: Throws `ArgumentNullException` if any argument is `null`.

The options object is read on every call, not copied, so changes made to it after construction take effect on the next evaluation.

#### `Task<PoolRoutingEvaluation> EvaluatePoolAsync(UpstreamSelectionContext context, CancellationToken cancellationToken = default)`

Scores every server in `context.PoolId` and returns the ranking together with the chosen upstream.

- **Parameters**
  - `context`: Selection context. Only `PoolId` is used by the balancer. `ClientIp`, `SessionToken`, and `Strategy` are ignored here.
  - `cancellationToken`: Checked once, after the pool lookup.
- **Return value**: A `PoolRoutingEvaluation` with `PoolId`, `Scores` (sorted best first, including ineligible servers), `SelectedUpstreamId`, and `EvaluatedAt` (UTC). `SelectedUpstreamId` is `null` when the pool is missing, the pool has no servers, or no server is eligible. `HasEligibleUpstream` reports whether a selection was made.
- **Exceptions**: Throws `ArgumentNullException` if `context` is `null`. Throws `OperationCanceledException` if the token is cancelled. A missing pool is not an error; it returns an empty evaluation.

#### `Task RecordOutcomeAsync(string poolId, string upstreamId, int responseTimeMs, bool succeeded)`

Records the result of a request routed to an upstream. Call it once per routed request. It feeds the metrics window, updates the adaptive multiplier, and, on failure, starts a new penalty.

- **Parameters**
  - `poolId`: Pool that owns the upstream. Only validated, not looked up.
  - `upstreamId`: Upstream that handled the request.
  - `responseTimeMs`: End-to-end response time in milliseconds.
  - `succeeded`: Whether the request completed without an error.
- **Return value**: A completed `Task`.
- **Exceptions**: Throws `ArgumentException` if `poolId` or `upstreamId` is null, empty, or whitespace.

#### `Task<int> GetEffectiveWeightAsync(string upstreamId)`

Returns the upstream's adaptive multiplier scaled to an integer: `max(1, round(multiplier * 100))`. A value of `100` means neutral.

- **Parameters**
  - `upstreamId`: Upstream identifier.
- **Return value**: A positive integer. `100` is returned for an upstream with no adaptive state.
- **Exceptions**: Throws `ArgumentException` if `upstreamId` is null, empty, or whitespace.

This value does not include the configured `UpstreamServer.Weight`. `EvaluatePoolAsync` uses `Weight * multiplier` for `EffectiveWeight`. Use `PoolRoutingEvaluation.Scores` when you need the weight that the selector actually applies.

#### `Task RecalibratePoolAsync(string poolId, CancellationToken cancellationToken = default)`

Discards the metrics windows, adaptive multipliers, and failure penalties for every server in a pool, so all servers start neutral again. Call it after topology changes such as adding or removing backends.

- **Parameters**
  - `poolId`: Pool to recalibrate.
  - `cancellationToken`: Checked once, after the pool lookup.
- **Return value**: A `Task` that completes when the state has been cleared.
- **Exceptions**: Throws `ArgumentException` if `poolId` is null, empty, or whitespace. Throws `OperationCanceledException` if the token is cancelled. An unknown pool is a silent no-op.

## Behaviour Notes

- **Eligibility uses `IsAvailable()`**, which requires `Status == Active` and `IsHealthy == true`. A server in `HalfOpen` is therefore never eligible, whatever its score. `HalfOpenPenaltyMultiplier` only lowers the reported `NormalizedScore` for such a server.
- **Weights are not divided by their sum.** The composite is the plain weighted sum, then clamped. The default weights (`0.4 + 0.4 + 0.2`) add up to `1.0`, so the result stays in range. If you change the weights so that they sum to more than `1.0`, the composite saturates at `1.0` more often and the ranking flattens.
- **Penalties and adaptive state are keyed by upstream id**, not by pool. A server that appears in two pools shares its state across them.
- **`RecordOutcomeAsync` does not check the pool.** It validates `poolId` but does not confirm the pool exists or that it contains the upstream. Pass the pool that the request was routed through.
- **Concurrency.** Metrics are stored in a `ConcurrentDictionary`, and each window has its own lock. Adaptive weights and penalty timestamps are guarded by separate locks. Concurrent `RecordOutcomeAsync`, `EvaluatePoolAsync`, and `RecalibratePoolAsync` calls are safe. A pass over a pool is not atomic, though, so one evaluation may see some servers updated and others not.

## Usage

### Example 1: Creating the balancer with the default options

The manual setup below mirrors the constructor calls in `Extensions/UpstreamServiceExtensions.cs`. Inside the application, `AddUpstreamManagement` registers the same objects in the DI container.

```csharp
using CaddyVpsToolkit.Configuration;
using CaddyVpsToolkit.LoadBalancing;
using CaddyVpsToolkit.Services;

var options = new UpstreamManagementOptions();
IMetricsAggregator metrics = new SlidingWindowMetricsAggregator(options.MetricsWindowSize);
var balancer = new AdaptiveLoadBalancer(upstreamManager, metrics, options);
```

`upstreamManager` is an existing `UpstreamManagerService`, for example one resolved from the container.

### Example 2: Selecting an upstream and reporting the outcome

```csharp
var evaluation = await balancer.EvaluatePoolAsync(new UpstreamSelectionContext(PoolId: "web-pool"));

if (!evaluation.HasEligibleUpstream)
{
    // No server is eligible. Decide here whether to fail the request or fall back.
    return;
}

var upstreamId = evaluation.SelectedUpstreamId!;
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
bool succeeded = await ForwardRequestAsync(upstreamId);

await balancer.RecordOutcomeAsync(
    poolId: "web-pool",
    upstreamId: upstreamId,
    responseTimeMs: (int)stopwatch.ElapsedMilliseconds,
    succeeded: succeeded);
```

`ForwardRequestAsync` stands for your own code that sends the request to the upstream.

### Example 3: Inspecting the scoring breakdown

`Scores` contains every candidate, including ineligible ones, so you can see why a server is or is not being picked.

```csharp
var evaluation = await balancer.EvaluatePoolAsync(new UpstreamSelectionContext(PoolId: "web-pool"));

foreach (var score in evaluation.Scores)
{
    Console.WriteLine(
        $"{score.UpstreamId,-20} score={score.NormalizedScore:F3} " +
        $"latency={score.LatencyScore:F2} errors={score.ErrorRateScore:F2} " +
        $"conn={score.ConnectionScore:F2} weight={score.EffectiveWeight} " +
        $"eligible={score.IsEligible}");
}
```

### Example 4: Tuning the scoring model

```csharp
var options = new UpstreamManagementOptions
{
    LatencyWeight = 0.6,          // prefer low-latency backends
    ErrorRateWeight = 0.3,
    ConnectionWeight = 0.1,
    TargetLatencyMs = 150,        // p99 target for a full latency score
    MaxExpectedConnections = 250, // connection pressure normalisation
    PenaltyMultiplier = 0.1,      // harsher penalty right after a failure
    PenaltyDecaySeconds = 120     // penalty fades over two minutes
};

var metrics = new SlidingWindowMetricsAggregator(windowSize: 500);
var balancer = new AdaptiveLoadBalancer(upstreamManager, metrics, options);
```

### Example 5: Recalibrating after a topology change

```csharp
// After adding or removing backends, drop the old statistics so that
// servers are compared on fresh observations only.
// (Update the pool's server list through your own pool management first.)
await balancer.RecalibratePoolAsync("web-pool");
```

### Example 6: Reading the effective weight

```csharp
int weight = await balancer.GetEffectiveWeightAsync("10.0.0.11:8080");
// 100 = neutral, below 100 = recently failing or penalised, above 100 = performing well.
```

### Example 7: Querying the metrics window directly

```csharp
metrics.Record("10.0.0.11:8080", responseTimeMs: 42, succeeded: true);

UpstreamMetricsSummary? summary = metrics.GetSummary("10.0.0.11:8080");
if (summary is { IsStatisticallySignificant: true })
{
    Console.WriteLine($"p99={summary.P99LatencyMs:F1} ms, error rate={summary.ErrorRate:P1}");
}

metrics.Reset("10.0.0.11:8080"); // discard the window for this upstream
```
