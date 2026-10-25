# UpstreamMetrics

`UpstreamMetrics.cs` contains the types that record and summarize request performance for a single upstream server. It defines three public types:

- `RequestSample` is a single request observation: response time, success flag, and timestamp.
- `UpstreamMetricsWindow` is a thread-safe, bounded sliding window of `RequestSample` values for one upstream. It produces summaries on demand.
- `UpstreamMetricsSummary` is an immutable snapshot of aggregated latency, error-rate, and throughput figures. The adaptive load balancer uses it for scoring.

All latency values are in milliseconds. The error rate is a fraction in the range `[0.0, 1.0]`.

## API

### `UpstreamMetricsSummary`

`public record UpstreamMetricsSummary(string UpstreamId, int SampleCount, double P50LatencyMs, double P95LatencyMs, double P99LatencyMs, double MeanLatencyMs, double ErrorRate, double ThroughputRps, DateTime WindowStartUtc, DateTime WindowEndUtc)`

An immutable point-in-time summary for one upstream. Because it is a positional record, it supports value equality and `with` expressions.

| Member | Type | Purpose | Parameters | Return Value | Exceptions |
|--------|------|---------|------------|--------------|------------|
| `UpstreamId` | `string` | Identifier of the upstream server this summary describes. | — | The upstream identifier. | None. |
| `SampleCount` | `int` | Number of observations that contributed to the summary. | — | The sample count. | None. |
| `P50LatencyMs` | `double` | 50th-percentile (median) response latency. | — | Latency in milliseconds. | None. |
| `P95LatencyMs` | `double` | 95th-percentile response latency. | — | Latency in milliseconds. | None. |
| `P99LatencyMs` | `double` | 99th-percentile response latency. The adaptive scoring model uses this as its primary latency signal because it reflects tail behaviour. | — | Latency in milliseconds. | None. |
| `MeanLatencyMs` | `double` | Arithmetic mean response latency. | — | Latency in milliseconds. | None. |
| `ErrorRate` | `double` | Fraction of samples that failed. | — | Value in `[0.0, 1.0]`. | None. |
| `ThroughputRps` | `double` | Estimated throughput in requests per second over the window duration. | — | Requests per second. | None. |
| `WindowStartUtc` | `DateTime` | UTC time at which the observation window opened. | — | UTC timestamp. | None. |
| `WindowEndUtc` | `DateTime` | UTC time at which the summary was computed. | — | UTC timestamp. | None. |
| `IsStatisticallySignificant` | `bool` | Whether the summary rests on enough samples to be trusted. | — | `true` when `SampleCount >= 5`. Smaller summaries are provisional, and the scoring model weights their percentiles less. | None. |
| `ToString` | `string` | Produces a single-line description for logs and status output. | — | A string of the form `"[UpstreamId] n=... p50=...ms p99=...ms err=...% rps=..."`. Latencies use one decimal place, the error rate is a percentage with one decimal place, and throughput uses two decimal places. | None. |

### `RequestSample`

`public record RequestSample(int ResponseTimeMs, bool Succeeded, DateTime RecordedAt)`

A single request observation. Instances are created internally by `UpstreamMetricsWindow.Add`, so callers normally do not construct them directly.

| Member | Type | Purpose | Parameters | Return Value | Exceptions |
|--------|------|---------|------------|--------------|------------|
| `ResponseTimeMs` | `int` | Round-trip time of the request in milliseconds. | — | Response time. | None. |
| `Succeeded` | `bool` | Whether the request completed without an error. | — | `true` on success. | None. |
| `RecordedAt` | `DateTime` | UTC time when the observation was captured. | — | UTC timestamp. | None. |

### `UpstreamMetricsWindow`

`public sealed class UpstreamMetricsWindow`

A bounded sliding window for one upstream. When the window is full, `Add` evicts the oldest sample. All members are guarded by an internal lock, so the window can be shared across threads.

#### `UpstreamMetricsWindow(string upstreamId, int maxSamples = 200)`

Creates a window for the given upstream.

- **Parameters**:
  - `upstreamId`: The upstream identifier. Must not be null or whitespace.
  - `maxSamples`: Maximum number of samples retained. Must be at least `1`. Defaults to `200`.
- **Exceptions**:
  - `ArgumentNullException` if `upstreamId` is `null`.
  - `ArgumentException` if `upstreamId` is empty or whitespace.
  - `ArgumentOutOfRangeException` if `maxSamples` is less than `1`.

#### `string UpstreamId { get; }`

Gets the upstream identifier this window tracks.

- **Type**: `string`

#### `DateTime WindowStartUtc { get; }`

Gets the UTC time when the current window started. It is set when the window is constructed and reset by `Clear`. It is not updated when the first sample arrives. Throughput is calculated from this value.

- **Type**: `DateTime`

#### `void Add(int responseTimeMs, bool succeeded)`

Records a new observation, stamped with the current UTC time. If the window already holds `maxSamples` entries, the oldest one is removed first. Negative response times are clamped to `0` so they cannot distort the percentiles.

- **Parameters**:
  - `responseTimeMs`: Request round-trip time in milliseconds. Negative values become `0`.
  - `succeeded`: Whether the request completed without an error.

#### `void Clear()`

Removes all samples and sets `WindowStartUtc` to the current UTC time. After `Clear`, `Summarize` returns `null` until new samples are added.

#### `UpstreamMetricsSummary? Summarize()`

Computes a summary from the samples currently in the window. The result is a snapshot, so later calls to `Add` do not change it.

- **Return value**: An `UpstreamMetricsSummary`, or `null` when the window is empty.
- **How the values are computed**:
  - Percentiles use linear interpolation over the sorted latencies, with rank `p × (n − 1)`. With a single sample, every percentile equals that sample.
  - `MeanLatencyMs` is the arithmetic mean of the latencies.
  - `ErrorRate` is the number of failed samples divided by the total count.
  - `ThroughputRps` is `SampleCount ÷ seconds elapsed since WindowStartUtc`. It is `0` when no time has elapsed.
  - `WindowEndUtc` is the time of the `Summarize` call.

## Usage

### Example 1: Recording observations and reading a summary

```csharp
using System;
using CaddyVpsToolkit.Domain.Models;

var window = new UpstreamMetricsWindow("api-1", maxSamples: 100);

window.Add(120, succeeded: true);
window.Add(95, succeeded: true);
window.Add(480, succeeded: false);
window.Add(110, succeeded: true);
window.Add(130, succeeded: true);

UpstreamMetricsSummary? summary = window.Summarize();

if (summary is not null)
{
    Console.WriteLine(summary.SampleCount);       // 5
    Console.WriteLine(summary.P50LatencyMs);      // 120
    Console.WriteLine(summary.ErrorRate);         // 0.2
    Console.WriteLine(summary.ToString());        // e.g. [api-1] n=5 p50=120.0ms p99=... err=20.0% rps=...
}
```

### Example 2: Empty windows and the significance threshold

```csharp
var window = new UpstreamMetricsWindow("api-2");

Console.WriteLine(window.Summarize() is null); // True: no samples yet

for (var i = 0; i < 4; i++)
    window.Add(50, succeeded: true);

var provisional = window.Summarize();
Console.WriteLine(provisional!.IsStatisticallySignificant); // False: only 4 samples

window.Add(50, succeeded: true);

Console.WriteLine(window.Summarize()!.IsStatisticallySignificant); // True: 5 samples
```

### Example 3: Sliding-window eviction

```csharp
var window = new UpstreamMetricsWindow("api-3", maxSamples: 3);

window.Add(1000, succeeded: false); // evicted when the fourth sample arrives
window.Add(100, succeeded: true);
window.Add(100, succeeded: true);
window.Add(100, succeeded: true);

var summary = window.Summarize()!;
Console.WriteLine(summary.SampleCount); // 3
Console.WriteLine(summary.ErrorRate);   // 0
```

### Example 4: Resetting a window

```csharp
window.Clear();

Console.WriteLine(window.Summarize() is null); // True
```

### Example 5: Feeding the adaptive load balancer

`SlidingWindowMetricsAggregator` keeps one `UpstreamMetricsWindow` per upstream. You can use the same pattern directly:

```csharp
using System.Collections.Generic;
using CaddyVpsToolkit.Domain.Models;

var windows = new Dictionary<string, UpstreamMetricsWindow>();

void RecordRequest(string upstreamId, int responseTimeMs, bool succeeded)
{
    if (!windows.TryGetValue(upstreamId, out var window))
    {
        window = new UpstreamMetricsWindow(upstreamId);
        windows[upstreamId] = window;
    }

    window.Add(responseTimeMs, succeeded);
}
```

Call `Summarize()` on each window when you need current figures, and call `Clear()` to start a fresh observation period.

### Example 6: Working with the record directly

Because `UpstreamMetricsSummary` is a record, you can build one by hand for tests or for replaying stored results:

```csharp
var expected = new UpstreamMetricsSummary(
    UpstreamId: "api-1",
    SampleCount: 10,
    P50LatencyMs: 100.0,
    P95LatencyMs: 250.0,
    P99LatencyMs: 400.0,
    MeanLatencyMs: 120.0,
    ErrorRate: 0.0,
    ThroughputRps: 5.0,
    WindowStartUtc: DateTime.UtcNow.AddMinutes(-1),
    WindowEndUtc: DateTime.UtcNow);

var degraded = expected with { ErrorRate = 0.1 };
```
