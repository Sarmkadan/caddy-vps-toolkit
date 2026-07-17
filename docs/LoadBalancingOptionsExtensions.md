# LoadBalancingOptionsExtensions

Extension methods for configuring load-balancing behavior in Caddy with health checks, circuit breakers, and retry policies.

## API

### `UseRoundRobinWithHealthChecks(LoadBalancingOptions options, int healthCheckIntervalMs, double healthProbeTimeoutSeconds)`

Configures the load balancer to use round-robin distribution with periodic health checks.

- **Parameters**
  - `options`: The `LoadBalancingOptions` instance to configure.
  - `healthCheckIntervalMs`: Interval between health checks in milliseconds.
  - `healthProbeTimeoutSeconds`: Timeout for individual health probe requests in seconds.
- **Return value**: The configured `LoadBalancingOptions` for method chaining.
- **Throws**: `ArgumentOutOfRangeException` if `healthCheckIntervalMs` is non-positive or `healthProbeTimeoutSeconds` is non-positive.

---

### `UseLeastConnectionsWithHealthChecks(LoadBalancingOptions options, int healthCheckIntervalMs, double healthProbeTimeoutSeconds)`

Configures the load balancer to use least-connections distribution with periodic health checks.

- **Parameters**
  - `options`: The `LoadBalancingOptions` instance to configure.
  - `healthCheckIntervalMs`: Interval between health checks in milliseconds.
  - `healthProbeTimeoutSeconds`: Timeout for individual health probe requests in seconds.
- **Return value**: The configured `LoadBalancingOptions` for method chaining.
- **Throws**: `ArgumentOutOfRangeException` if `healthCheckIntervalMs` is non-positive or `healthProbeTimeoutSeconds` is non-positive.

---
### `ConfigureCircuitBreaker(LoadBalancingOptions options, bool isStrict, int failureThreshold, TimeSpan resetTimeout)`

Configures circuit breaker behavior for the load balancer.

- **Parameters**
  - `options`: The `LoadBalancingOptions` instance to configure.
  - `isStrict`: If `true`, the circuit breaker trips on the first failure; otherwise, it requires multiple failures.
  - `failureThreshold`: Number of consecutive failures required to trip the circuit.
  - `resetTimeout`: Duration after which the circuit resets to half-open state.
- **Return value**: The configured `LoadBalancingOptions` for method chaining.
- **Throws**: `ArgumentOutOfRangeException` if `failureThreshold` is less than 1 or `resetTimeout` is non-positive.

---
### `EnableStickySessions(LoadBalancingOptions options, string cookieName = "CaddySticky")`

Enables session affinity using a cookie with the specified name.

- **Parameters**
  - `options`: The `LoadBalancingOptions` instance to configure.
  - `cookieName`: Name of the cookie used for sticky sessions. Defaults to `"CaddySticky"`.
- **Return value**: The configured `LoadBalancingOptions` for method chaining.

---
### `GetHealthCheckIntervalMs(LoadBalancingOptions options)`

Retrieves the configured health check interval in milliseconds.

- **Parameters**
  - `options`: The `LoadBalancingOptions` instance to inspect.
- **Return value**: The health check interval in milliseconds.
- **Throws**: `ArgumentNullException` if `options` is `null`.

---
### `GetHealthProbeTimeoutSeconds(LoadBalancingOptions options)`

Retrieves the configured health probe timeout in seconds.

- **Parameters**
  - `options`: The `LoadBalancingOptions` instance to inspect.
- **Return value**: The health probe timeout in seconds.
- **Throws**: `ArgumentNullException` if `options` is `null`.

---
### `IsStrictCircuitBreaker(LoadBalancingOptions options)`

Determines whether the circuit breaker is in strict mode.

- **Parameters**
  - `options`: The `LoadBalancingOptions` instance to inspect.
- **Return value**: `true` if the circuit breaker trips on the first failure; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `options` is `null`.

---
### `GetRetryConfiguration(LoadBalancingOptions options)`

Retrieves the retry policy configuration as a sequence of key-value pairs.

- **Parameters**
  - `options`: The `LoadBalancingOptions` instance to inspect.
- **Return value**: An enumerable of key-value pairs representing retry configuration (e.g., max retries, backoff strategy).
- **Throws**: `ArgumentNullException` if `options` is `null`.

## Usage

### Example 1: Round-robin with health checks and circuit breaker
```csharp
var options = new LoadBalancingOptions()
    .UseRoundRobinWithHealthChecks(healthCheckIntervalMs: 5000, healthProbeTimeoutSeconds: 2.0)
    .ConfigureCircuitBreaker(isStrict: false, failureThreshold: 3, resetTimeout: TimeSpan.FromSeconds(30));

if (options.IsStrictCircuitBreaker())
{
    Console.WriteLine("Circuit breaker is in strict mode.");
}
```

### Example 2: Least-connections with sticky sessions and retry policy
```csharp
var options = new LoadBalancingOptions()
    .UseLeastConnectionsWithHealthChecks(healthCheckIntervalMs: 10000, healthProbeTimeoutSeconds: 3.0)
    .EnableStickySessions(cookieName: "MyStickyCookie")
    .ConfigureCircuitBreaker(isStrict: true, failureThreshold: 5, resetTimeout: TimeSpan.FromMinutes(1));

foreach (var setting in options.GetRetryConfiguration())
{
    Console.WriteLine($"{setting.Key}: {setting.Value}");
}
```

## Notes

- **Thread safety**: All methods are thread-safe and may be called concurrently on the same `LoadBalancingOptions` instance.
- **State consistency**: Modifying the same `LoadBalancingOptions` instance across threads without synchronization may lead to race conditions in derived configurations.
- **Health check timing**: A `healthCheckIntervalMs` value of `0` disables health checks entirely; negative values are rejected.
- **Circuit breaker thresholds**: A `failureThreshold` of `1` with `isStrict: true` trips the circuit immediately on the first failure.
- **Retry configuration**: The exact keys and values in the retry policy are implementation-defined and may change between versions.
