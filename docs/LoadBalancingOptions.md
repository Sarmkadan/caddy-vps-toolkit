# LoadBalancingOptions

`LoadBalancingOptions` is a configuration class used to define and customize load balancing behavior for backend services in the `caddy-vps-toolkit`. It provides settings for health checks, retry policies, session persistence, circuit breaking, and connection draining to ensure robust and efficient traffic distribution across multiple endpoints.

## API

### `DefaultStrategy`
- **Purpose**: Specifies the default load balancing algorithm to use when distributing requests.
- **Type**: `LoadBalancingStrategy` (enum)
- **Return Value**: The configured strategy (e.g., RoundRobin, LeastConnections).
- **Notes**: Must be set to a valid strategy; invalid values may cause runtime errors during initialization.

### `HealthCheckIntervalSeconds`
- **Purpose**: Defines the interval (in seconds) between active health checks for backend endpoints.
- **Type**: `int`
- **Return Value**: The configured interval.
- **Notes**: A value of `0` disables periodic health checks. Negative values are invalid and may throw `ArgumentOutOfRangeException`.

### `HealthProbeTimeoutMs`
- **Purpose**: Sets the timeout duration (in milliseconds) for individual health probe requests.
- **Type**: `int`
- **Return Value**: The timeout value in milliseconds.
- **Notes**: Must be a positive integer; zero or negative values may result in immediate timeouts.

### `HealthProbePath`
- **Purpose**: Specifies the HTTP path used for health check probes.
- **Type**: `string`
- **Return Value**: The configured probe path (e.g., `/health`).
- **Notes**: Must be a valid HTTP path; `null` or empty strings may cause health checks to fail.

### `ActiveHealthEnabled`
- **Purpose**: Enables or disables active health checks that proactively monitor backend health.
- **Type**: `bool`
- **Return Value**: `true` if active health checks are enabled, `false` otherwise.
- **Notes**: Requires `HealthProbePath` to be set when enabled.

### `PassiveHealthEnabled`
- **Purpose**: Enables or disables passive health checks that infer backend health from request outcomes.
- **Type**: `bool`
- **Return Value**: `true` if passive health checks are enabled, `false` otherwise.
- **Notes**: Works in conjunction with `UnhealthyThreshold` and `HealthyThreshold`.

### `UnhealthyThreshold`
- **Purpose**: Defines the number of consecutive failed requests required to mark a backend as unhealthy.
- **Type**: `int`
- **Return Value**: The configured threshold.
- **Notes**: Must be a positive integer; a value of `0` may prevent backends from being marked unhealthy.

### `HealthyThreshold`
- **Purpose**: Defines the number of consecutive successful requests required to mark a backend as healthy.
- **Type**: `int`
- **Return Value**: The configured threshold.
- **Notes**: Must be a positive integer; a value of `0` may prevent backends from being marked healthy.

### `MaxRetries`
- **Purpose**: Sets the maximum number of retry attempts for failed requests.
- **Type**: `int`
- **Return Value**: The maximum retry count.
- **Notes**: A value of `0` disables retries. Negative values are invalid.

### `RetryDurationSeconds`
- **Purpose**: Specifies the total duration (in seconds) allowed for retry attempts.
- **Type**: `int`
- **Return Value**: The configured retry duration.
- **Notes**: Must be a positive integer; `0` may cause retries to be skipped immediately.

### `StickySessionEnabled`
- **Purpose**: Enables or disables sticky sessions to maintain client affinity with specific backends.
- **Type**: `bool`
- **Return Value**: `true` if sticky sessions are enabled, `false` otherwise.
- **Notes**: Requires `DefaultStickyCookieName` to be set when enabled.

### `DefaultStickyCookieName`
- **Purpose**: Specifies the cookie name used for sticky session tracking.
- **Type**: `string`
- **Return Value**: The configured cookie name.
- **Notes**: Must be a valid cookie name; `null` or empty strings may cause session affinity to fail.

### `CircuitBreakerEnabled`
- **Purpose**: Enables or disables the circuit breaker pattern to prevent cascading failures.
- **Type**: `bool`
- **Return Value**: `true` if the circuit breaker is enabled, `false` otherwise.
- **Notes**: Requires `CircuitBreakerHealthThreshold` to be configured when enabled.

### `CircuitBreakerHealthThreshold`
- **Purpose**: Defines the failure percentage threshold at which the circuit breaker trips.
- **Type**: `double`
- **Return Value**: The configured threshold (e.g., `0.5` for 50% failures).
- **Notes**: Must be between `0.0` and `1.0`; values outside this range may throw `ArgumentOutOfRangeException`.

### `CircuitBreakerRecoverySeconds`
- **Purpose**: Specifies the duration (in seconds) the circuit breaker remains open before attempting recovery.
- **Type**: `int`
- **Return Value**: The configured recovery time.
- **Notes**: Must be a positive integer; `0` may cause immediate retries without backoff.

### `ConnectionDrainTimeoutSeconds`
- **Purpose**: Defines the time (in seconds) to wait for active connections to complete during backend removal.
- **Type**: `int`
- **Return Value**: The configured drain timeout.
- **Notes**: A value of `0` disables connection draining; negative values are invalid.

## Usage

### Example 1: Configuring Active Health Checks and Retry Policies
```csharp
var options = new LoadBalancingOptions
{
    ActiveHealthEnabled = true,
    HealthCheckIntervalSeconds = 30,
    HealthProbePath = "/health",
    HealthProbeTimeoutMs = 5000,
    MaxRetries = 3,
    RetryDurationSeconds = 10
};
```

### Example 2: Enabling Sticky Sessions and Circuit Breaker
```csharp
var options = new LoadBalancingOptions
{
    StickySessionEnabled = true,
    DefaultStickyCookieName = "JSESSIONID",
    CircuitBreakerEnabled = true,
    CircuitBreakerHealthThreshold = 0.7,
    CircuitBreakerRecoverySeconds = 60
};
```

## Notes

- **Thread Safety**: `LoadBalancingOptions` is a mutable configuration class. Concurrent modifications to its properties may lead to inconsistent states. It is recommended to configure instances once during initialization and avoid runtime changes.
- **Edge Cases**:
  - Setting `HealthCheckIntervalSeconds` to `0` disables active health checks entirely.
  - A `CircuitBreakerHealthThreshold` of `1.0` will trip the circuit breaker on any failure.
  - `ConnectionDrainTimeoutSeconds` should be set to a value greater than the expected request duration to avoid abrupt connection termination.
- **Validation**: Invalid configurations (e.g., negative thresholds, out-of-range percentages) may throw exceptions during validation or runtime initialization.
