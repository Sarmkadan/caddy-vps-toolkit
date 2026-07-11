# UpstreamManagementOptions

Configuration object that tunes the upstream selection algorithm used by the Caddy VPS Toolkit. It allows weighting of latency, error rate, and connection count, defines target latency and adaptation parameters, and controls automatic recalibration behavior.

## API

### LatencyWeight
- **Purpose:** Relative importance of measured latency when computing an upstream score. Higher values increase the influence of latency.
- **Parameters:** None.
- **Return value:** `double` – the weight applied to latency.
- **Exceptions:** None; assigning a negative value is permitted but may produce unintuitive scoring.

### ErrorRateWeight
- **Purpose:** Relative importance of observed error rate when computing an upstream score. Higher values penalize upstreams with more errors.
- **Parameters:** None.
- **Return value:** `double` – the weight applied to error rate.
- **Exceptions:** None; assigning a negative value is permitted but may produce unintuitive scoring.

### ConnectionWeight
- **Purpose:** Relative importance of current connection count when computing an upstream score. Higher values favor upstreams with fewer active connections.
- **Parameters:** None.
- **Return value:** `double` – the weight applied to connection count.
- **Exceptions:** None; assigning a negative value is permitted but may produce unintuitive scoring.

### TargetLatencyMs
- **Purpose:** Desired latency threshold (in milliseconds) used to normalize latency measurements for scoring.
- **Parameters:** None.
- **Return value:** `double` – the target latency in milliseconds.
- **Exceptions:** None; a value of zero will cause a division‑by‑zero in the scoring algorithm if not guarded elsewhere.

### MaxExpectedConnections
- **Purpose:** Upper bound on the number of simultaneous connections an upstream is expected to handle; used to scale the connection‑weight component.
- **Parameters:** None.
- **Return value:** `int` – the maximum expected connections.
- **Exceptions:** None; values less than 1 may lead to undefined scaling.

### WeightAdaptationAlpha
- **Purpose:** Smoothing factor (0 < α ≤ 1) applied when adapting the internal weights based on recent metrics. Larger α gives more weight to the latest observation.
- **Parameters:** None.
- **Return value:** `double` – the adaptation alpha.
- **Exceptions:** None; values outside (0, 1] produce undefined behavior in the adaptation routine.

### PenaltyMultiplier
- **Purpose:** Factor by which a penalty is amplified when an upstream exceeds its error‑rate or latency thresholds.
- **Parameters:** None.
- **Return value:** `double` – the penalty multiplier.
- **Exceptions:** None; negative values invert the penalty effect.

### PenaltyDecaySeconds
- **Purpose:** Time period (in seconds) after which an applied penalty decays exponentially toward zero.
- **Parameters:** None.
- **Return value:** `double` – the decay period in seconds.
- **Exceptions:** None; non‑positive values disable decay.

### MetricsWindowSize
- **Purpose:** Number of most recent metric samples retained for calculating moving averages of latency, error rate, and connection count.
- **Parameters:** None.
- **Return value:** `int` – the size of the sliding window.
- **Exceptions:** None; a value of zero disables windowed averaging.

### AutoRecalibrationEnabled
- **Purpose:** Flag indicating whether the upstream manager should periodically recalibrate its internal weights based on observed performance.
- **Parameters:** None.
- **Return value:** `bool` – true if automatic recalibration is enabled.
- **Exceptions:** None.

### RecalibrationIntervalSeconds
- **Purpose:** Interval (in seconds) between automatic recalibration attempts when `AutoRecalibrationEnabled` is true.
- **Parameters:** None.
- **Return value:** `int` – the recalibration interval in seconds.
- **Exceptions:** None; values less than 1 may cause excessive recalibration.

## Usage

```csharp
using CaddyVpsToolkit.Upstream;

// Create a default options instance and tune it for a latency‑sensitive service.
var opts = new UpstreamManagementOptions
{
    LatencyWeight      = 0.6,
    ErrorRateWeight    = 0.3,
    ConnectionWeight   = 0.1,
    TargetLatencyMs    = 100.0,
    MaxExpectedConnections = 1000,
    WeightAdaptationAlpha = 0.2,
    PenaltyMultiplier   = 2.0,
    PenaltyDecaySeconds = 30.0,
    MetricsWindowSize   = 50,
    AutoRecalibrationEnabled = true,
    RecalibrationIntervalSeconds = 300
};

// Pass the options to the upstream manager during initialization.
var manager = new UpstreamManager(opts);
```

```csharp
using CaddyVpsToolkit.Upstream;

// Adjust options at runtime based on deployment environment.
var opts = new UpstreamManagementOptions();
// Prefer low error rates over latency in an unreliable network.
opts.ErrorRateWeight = 0.7;
opts.LatencyWeight   = 0.2;
opts.ConnectionWeight = 0.1;

// Disable automatic recalibration for a static benchmark.
opts.AutoRecalibrationEnabled = false;

// Apply the updated configuration.
var manager = new UpstreamManager(opts);
manager.UpdateOptions(opts); // Assuming the manager exposes such a method.
```

## Notes

- Setting any weight to a negative value will not throw an exception but will invert its influence on the scoring function, potentially causing counter‑intuitive upstream selection.
- `TargetLatencyMs` of zero will cause a division‑by‑zero in the latency normalization step if the implementation does not guard against it; a small positive epsilon is recommended.
- `MaxExpectedConnections` should reflect the realistic upper bound of traffic; values significantly lower than actual load will cause the connection‑weight component to saturate, reducing its discriminative power.
- The adaptation routine assumes `WeightAdaptationAlpha` lies in the interval (0, 1]; values outside this range may lead to divergence or stagnation of the weight‑learning process.
- `PenaltyDecaySeconds` of zero or negative disables exponential decay, causing penalties to persist indefinitely unless manually cleared.
- `MetricsWindowSize` determines the statistical stability of the moving averages; a window size of one yields instantaneous (noisy) metrics, while very large windows increase memory usage and slow response to changes.
- The type contains only mutable fields; it is **not** thread‑safe. Concurrent reads and writes from multiple threads must be synchronized externally (e.g., using `lock` or `Interlocked` operations) to avoid race conditions.
- No members throw exceptions under normal use; validation of semantic correctness (e.g., non‑negative times, positive weights) is the responsibility of the consumer or the upstream manager that consumes the options.
