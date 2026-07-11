# UpstreamMetricsSummary

A record type that aggregates and summarizes metrics for upstream connections over a defined time window. It is used to track request patterns, response behaviors, and overall performance of upstream services within a specified interval, enabling analysis of upstream health and load distribution.

## API

### `UpstreamMetricsSummary`

A record representing the summarized metrics for an upstream service over a specific time window.

### `public override string ToString()`

Returns a human-readable string representation of the summarized metrics, including key statistics such as request count, error rate, and average latency.

- **Return value**: A `string` containing the formatted metrics summary.

### `public record RequestSample`

A nested record type that captures a single request sample for an upstream service, including metadata about the request and its outcome.

### `public string UpstreamId`

Gets the unique identifier of the upstream service associated with the metrics.

- **Type**: `string`

### `public DateTime WindowStartUtc`

Gets the start time of the metrics aggregation window in UTC.

- **Type**: `DateTime`

### `public UpstreamMetricsWindow`

Gets the time window over which the metrics were aggregated.

- **Type**: `UpstreamMetricsWindow`

### `public void Add(UpstreamMetricsSummary other)`

Merges the metrics from another `UpstreamMetricsSummary` into this instance, combining counts and aggregating values.

- **Parameters**:
  - `other`: The `UpstreamMetricsSummary` instance whose metrics will be merged into this one.
- **Throws**: `ArgumentNullException` if `other` is `null`.

### `public void Clear()`

Resets all aggregated metrics and counters in this instance to zero, effectively clearing the summary.

### `public UpstreamMetricsSummary? Summarize()`

Generates a new summarized metrics record based on the current aggregated data. This may return `null` if no valid data exists (e.g., no requests were recorded).

- **Return value**: A new `UpstreamMetricsSummary` instance with aggregated metrics, or `null` if no data is available.

## Usage

### Example 1: Aggregating Metrics Over Time
