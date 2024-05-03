# MetricsCollector

The `MetricsCollector` type provides a lightweight, in‑memory facility for gathering and reporting numeric metrics such as counters, gauges, and histograms. It is intended for use in scenarios where minimal overhead and simple aggregation are sufficient, such as tooling, tests, or lightweight services.

## API

### `public void IncrementCounter(string name, long value = 1)`
Increments the counter identified by `name` by `value`. If the counter does not exist it is created with an initial value of `value`.  
- **Parameters**  
  - `name`: The identifier of the counter; must not be `null` or whitespace.  
  - `value`: The amount to add; must be non‑negative.  
- **Return value**: None.  
- **Exceptions**:  
  - `ArgumentNullException` if `name` is `null`.  
  - `ArgumentException` if `name` consists only of whitespace.  
  - `ArgumentOutOfRangeException` if `value` is negative.

### `public void SetGauge(string name, double value)`
Sets the gauge identified by `name` to the supplied `value`. If the gauge does not exist it is created.  
- **Parameters**  
  - `name`: The identifier of the gauge; must not be `null` or whitespace.  
  - `value`: The gauge value.  
- **Return value**: None.  
- **Exceptions**:  
  - `ArgumentNullException` if `name` is `null`.  
  - `ArgumentException` if `name` consists only of whitespace.

### `public void RecordHistogram(string name, double value)`
Records a sample `value` in the histogram identified by `name`. If the histogram does not exist it is created.  
- **Parameters**  
  - `name`: The identifier of the histogram; must not be `null` or whitespace.  
  - `value`: The sample to record; must be a finite number.  
- **Return value**: None.  
- **Exceptions**:  
  - `ArgumentNullException` if `name` is `null`.  
  - `ArgumentException` if `name` consists only of whitespace.  
  - `ArgumentException` if `value` is `NaN`, `PositiveInfinity`, or `NegativeInfinity`.

### `public long GetCounter(string name)`
Retrieves the current value of the counter identified by `name`.  
- **Parameters**  
  - `name`: The identifier of the counter; must not be `null` or whitespace.  
- **Return value**: The current counter value as a `long`. Returns `0` if the counter has never been incremented.  
- **Exceptions**:  
  - `ArgumentNullException` if `name` is `null`.  
  - `ArgumentException` if `name` consists only of whitespace.

### `public double GetGauge(string name)`
Retrieves the current value of the gauge identified by `name`.  
- **Parameters**  
  - `name`: The identifier of the gauge; must not be `null` or whitespace.  
- **Return value**: The current gauge value as a `double`. Returns `0` if the gauge has never been set.  
- **Exceptions**:  
  - `ArgumentNullException` if `name` is `null`.  
  - `ArgumentException` if `name` consists only of whitespace.

### `public HistogramStats GetHistogramStats(string name)`
Returns statistical information for the histogram identified by `name`.  
- **Parameters**  
  - `name`: The identifier of the histogram; must not be `null` or whitespace.  
- **Return value**: A `HistogramStats` instance containing `Count`, `Min`, `Max`, `Average`, and `Median` for the recorded samples. If no samples have been recorded, all properties return default values (`0` for numeric fields).  
- **Exceptions**:  
  - `ArgumentNullException` if `name` is `null`.  
  - `ArgumentException` if `name` consists only of whitespace.

### `public string GenerateReport()`
Produces a formatted text report of all currently tracked metrics (counters, gauges, and histograms).  
- **Parameters**: None.  
- **Return value**: A multi‑line string suitable for logging or display. Returns an empty string if no metrics have been recorded.  
- **Exceptions**: None.

### `public void Increment(string name)`
Convenience overload that increments the counter `name` by `1`.  
- **Parameters**  
  - `name`: The identifier of the counter; must not be `null` or whitespace.  
- **Return value**: None.  
- **Exceptions**: Same as `IncrementCounter`.

### `public void Reset()`
Clears all stored metric data, returning the collector to its initial empty state.  
- **Parameters**: None.  
- **Return value**: None.  
- **Exceptions**: None.

### `public void Set(string name, double value)`
Convenience overload that sets the gauge `name` to `value`.  
- **Parameters**  
  - `name`: The identifier of the gauge; must not be `null` or whitespace.  
  - `value`: The gauge value.  
- **Return value**: None.  
- **Exceptions**: Same as `SetGauge`.

### `public void Record(string name, double value)`
Convenience overload that records a sample `value` in the histogram `name`.  
- **Parameters**  
  - `name`: The identifier of the histogram; must not be `null` or whitespace.  
  - `value`: The sample to record; must be a finite number.  
- **Return value**: None.  
- **Exceptions**: Same as `RecordHistogram`.

### `public HistogramStats GetStats(string name)`
Alias for `GetHistogramStats`. Returns statistical information for the histogram identified by `name`.  
- **Parameters**  
  - `name`: The identifier of the histogram; must not be `null` or whitespace.  
- **Return value**: A `HistogramStats` instance.  
- **Exceptions**: Same as `GetHistogramStats`.

### `public int Count { get; }`
Read‑only property that returns the number of samples recorded in the most recently accessed histogram (via `GetHistogramStats` or `GetStats`). If no histogram has been accessed, returns `0`.

### `public double Min { get; }`
Read‑only property that returns the minimum sample value observed in the most recently accessed histogram. Returns `0` if no samples exist.

### `public double Max { get; }`
Read‑only property that returns the maximum sample value observed in the most recently accessed histogram. Returns `0` if no samples exist.

### `public double Average { get; }`
Read‑only property that returns the arithmetic mean of the samples in the most recently accessed histogram. Returns `0` if no samples exist.

### `public double Median { get; }`
Read‑only property that returns the median sample value of the most recently accessed histogram. Returns `0` if no samples exist.

## Usage

```csharp
using CaddyVpsToolkit.Metrics;

// Create a collector instance
var metrics = new MetricsCollector();

// Track a request counter
metrics.IncrementCounter("http.requests", 1);

// Record response time histogram
metrics.RecordHistogram("http.latency_ms", 42.5);

// Later, retrieve and report
long requestCount = metrics.GetCounter("http.requests");
var latencyStats = metrics.GetHistogramStats("http.latency_ms");
Console.WriteLine($"Requests: {requestCount}");
Console.WriteLine($"Latency avg: {latencyStats.Average} ms");

// Generate a full report for logging
string report = metrics.GenerateReport();
File.AppendAllText("metrics.log", report + Environment.NewLine);
```

```csharp
using CaddyVpsToolkit.Metrics;

var metrics = new MetricsCollector();

// Set a gauge representing current pool size
metrics.SetGauge("threadpool.size", Environment.ProcessorCount);

// Increment a simple counter without specifying amount
metrics.Increment("jobs.processed");

// Reset all metrics (e.g., between test runs)
metrics.Reset();

// After reset, gauges and counters return default values
double gauge = metrics.GetGauge("threadpool.size"); // 0
long count   = metrics.GetCounter("jobs.processed"); // 0
```

## Notes

- All metric identifiers (`name` parameters) are case‑sensitive and must not be `null` or consist solely of whitespace; violating this contract throws an `ArgumentException`.
- Numeric values supplied to `IncrementCounter`, `SetGauge`, and `RecordHistogram` are validated: counters accept only non‑negative integers, gauges accept any finite `double`, and histograms reject `NaN` or infinite values.
- The collector is **not** thread‑safe by default. Concurrent calls from multiple threads may lead to race conditions. If thread safety is required, wrap access with a lock or use a concurrent wrapper.
- The `Count`, `Min`, `Max`, `Average`, and `Median` properties reflect the statistics of the **last** histogram accessed via `GetHistogramStats`, `GetStats`, or after a `Record`/`RecordHistogram` call. They do not represent aggregated values across all histograms.
- Calling `Reset()` clears all internal state; any previously obtained `HistogramStats` instances remain unchanged but are no longer reflective of the collector's current data.
- `GenerateReport()` enumerates metrics in the order they were first created; the exact format is subject to change in future versions but will always be a plain‑text, human‑readable representation.
