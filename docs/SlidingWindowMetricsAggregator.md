# SlidingWindowMetricsAggregator

SlidingWindowMetricsAggregator is a component designed to collect, aggregate, and analyze time-series metrics within a sliding time window, typically used for dynamic upstream selection and adaptive load balancing in distributed systems. It maintains statistical summaries of observed outcomes and facilitates recalibration of routing decisions based on recent performance trends.

## API

### `SlidingWindowMetricsAggregator()`

Initializes a new instance of the SlidingWindowMetricsAggregator. The internal state is configured with default or predefined window parameters.

---

### `void Record(double value)`

Records a numeric metric value into the sliding window. Values are timestamped and retained only within the configured time window.

**Parameters:**
- `value` (double): The metric value to record.

**Throws:**
- No exceptions are documented; assumes valid input.

---

### `UpstreamMetricsSummary? GetSummary()`

Retrieves a summary of the metrics recorded within the current sliding window. Returns `null` if no data has been recorded or the window is empty.

**Returns:**
- `UpstreamMetricsSummary?`: A summary object containing aggregated statistics (e.g., average, count, percentiles) or `null`.

---

### `void Reset()`

Clears all recorded metrics and resets the internal state of the aggregator. This operation is destructive and irreversible.

**Throws:**
- No exceptions are documented.

---

### `AdaptiveLoadBalancer`

Exposes the associated AdaptiveLoadBalancer instance responsible for routing decisions based on the aggregated metrics.

**Returns:**
- `AdaptiveLoadBalancer`: The linked load balancer instance.

---

### `async Task<PoolRoutingEvaluation> EvaluatePoolAsync()`

Evaluates the current pool of upstreams asynchronously, returning a routing decision based on the latest metrics summary. This method may trigger recalibration logic if thresholds are met.

**Returns:**
- `Task<PoolRoutingEvaluation>`: A task representing the asynchronous evaluation result.

**Throws:**
- `InvalidOperationException`: If the aggregator is not properly initialized or the pool is in an invalid state.

---

### `Task RecordOutcomeAsync(bool success)`

Records the outcome of a routing decision asynchronously. Used to feedback success/failure results into the metrics system.

**Parameters:**
- `success` (bool): Indicates whether the routing outcome was successful.

**Returns:**
- `Task`: A task representing the completion of the recording operation.

**Throws:**
- No exceptions are documented.

---

### `Task<int> GetEffectiveWeightAsync()`

Calculates and returns the effective weight of the upstream based on the current metrics summary. This weight influences routing probabilities.

**Returns:**
- `Task<int>`: A task representing the computed effective weight.

**Throws:**
- `InvalidOperationException`: If no metrics are available to compute the weight.

---

### `async Task RecalibratePoolAsync()`

Triggers an asynchronous recalibration of the upstream pool based on the latest metrics. This may adjust routing strategies or weights dynamically.

**Returns:**
- `Task`: A task representing the completion of the recalibration process.

**Throws:**
- `InvalidOperationException`: If the aggregator is not properly initialized.

## Usage

### Example 1: Basic Metric Recording and Summary Retrieval

```csharp
var aggregator = new SlidingWindowMetricsAggregator();
aggregator.Record(0.45);
aggregator.Record(0.32);
aggregator.Record(0.67);

var summary = aggregator.GetSummary();
if (summary != null)
{
    Console.WriteLine($"Average latency: {summary.Average}");
    Console.WriteLine($"Request count: {summary.Count}");
}
```

### Example 2: Asynchronous Pool Evaluation and Outcome Recording

```csharp
var aggregator = new SlidingWindowMetricsAggregator();

// Simulate recording outcomes
await aggregator.RecordOutcomeAsync(true);
await aggregator.RecordOutcomeAsync(false);

// Evaluate pool and adjust routing
var evaluation = await aggregator.EvaluatePoolAsync();
Console.WriteLine($"Routing decision: {evaluation.SelectedUpstream}");

// Recalibrate based on updated metrics
await aggregator.RecalibratePoolAsync();
```

## Notes

- **Thread Safety:** The class is designed to handle concurrent access. Methods like `Record` and `RecordOutcomeAsync` are safe for parallel invocation, but `Reset` should be used cautiously in multi-threaded environments to avoid race conditions with ongoing evaluations.
- **Null Summary:** `GetSummary()` may return `null` during initial state or after `Reset()` is called before new data is recorded.
- **Async Operations:** Methods ending with `Async` should be awaited to ensure proper sequencing of metric updates and recalibrations. Ignoring returned tasks may lead to incomplete or inconsistent state transitions.
- **Weight Calculation:** `GetEffectiveWeightAsync()` relies on the presence of recorded metrics. Calling it without prior data will result in an exception.
