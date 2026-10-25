# UpstreamServiceExtensions

`UpstreamServiceExtensions` is a static class in the `CaddyVpsToolkit.Extensions` namespace. It adds one extension method to `IServiceCollection`, `AddUpstreamManagement`, which registers the v2 dynamic upstream management and health-aware load-balancing subsystem in the dependency injection container.

The class has one public member. Its method is an extension method on `IServiceCollection`, so call it on the service collection instead of on the class.

## API

### `AddUpstreamManagement`
```csharp
public static IServiceCollection AddUpstreamManagement(
    this IServiceCollection services,
    Action<UpstreamManagementOptions>? configure = null)
```

Registers the adaptive upstream management services. The method creates a new `UpstreamManagementOptions`, runs the optional `configure` delegate on it, and then registers the components below. All registrations use singleton lifetime, because adaptive state such as weights and penalty timestamps must be shared across requests.

| Service | Implementation | Purpose |
|---------|----------------|---------|
| `UpstreamManagementOptions` | the instance configured here | Tuning parameters. Consumed by the adaptive balancer and available to operator tooling. |
| `IMetricsAggregator` | `SlidingWindowMetricsAggregator` | Thread-safe sliding-window metrics. Created with `MetricsWindowSize`. |
| `IUpstreamPoolRepository` | `CaddyVpsToolkit.Data.UpstreamPoolRepository` | Upstream pool storage. |
| `IUpstreamHealthTracker` | `UpstreamHealthTracker` | Per-upstream health state. |
| `IUpstreamSelector` | `UpstreamSelector` | Upstream selection. |
| `IAdaptiveLoadBalancer` | `AdaptiveLoadBalancer` | Adaptive scoring engine. |
| `HealthAwareRoutingPolicy` | `HealthAwareRoutingPolicy` | Unified routing entry point for callers. Resolve this type when routing requests. |

**Parameters:**
- `services` (`IServiceCollection`): The service collection to add the registrations to.
- `configure` (`Action<UpstreamManagementOptions>?`, optional): A delegate that customises the options before they are registered. When `null`, the defaults defined on `UpstreamManagementOptions` are used.

**Return value:**
- Returns the same `IServiceCollection` instance, so calls can be chained.

**Exceptions:**
- Throws `ArgumentNullException` if `services` is `null`.
- Any exception thrown by `configure` is passed through to the caller.

**Notes on registration order:**
- `configure` runs once, during this call. Its changes are not re-applied later.
- `IAdaptiveLoadBalancer` and `HealthAwareRoutingPolicy` resolve `UpstreamManagerService` and `LoadBalancingOptions` from the container when they are created. Those two types must be registered before the first resolution of these services. Registering them after this call is fine, but a missing registration fails at resolution time, not when this method is called.
- The `IMetricsAggregator` instance is constructed immediately with `MetricsWindowSize`, so an invalid window size can surface during this call.

## Options

The following properties of `UpstreamManagementOptions` are the ones most often set through `configure`. The defaults shown are the values in the options class.

| Property | Default | Effect |
|----------|---------|--------|
| `LatencyWeight` | `0.4` | Weight of the p99 latency dimension in the composite score. |
| `ErrorRateWeight` | `0.4` | Weight of the error-rate dimension in the composite score. |
| `ConnectionWeight` | `0.2` | Weight of the connection-pressure dimension in the composite score. |
| `TargetLatencyMs` | `200.0` | Target p99 latency in milliseconds. Upstreams at or below it get the full latency score. |
| `MaxExpectedConnections` | `100` | Expected maximum concurrent connections per upstream. Used to normalise the connection score. |
| `WeightAdaptationAlpha` | `0.15` | Exponential moving average alpha for adaptive weight updates. Valid range (0, 1). |
| `PenaltyMultiplier` | `0.3` | Score multiplier applied right after a failed request. Valid range (0, 1). |
| `PenaltyDecaySeconds` | `60.0` | Time over which a failure penalty decays linearly back to `1.0`. |
| `MetricsWindowSize` | `200` | Maximum request samples kept in each upstream's sliding metrics window. |
| `AutoRecalibrationEnabled` | `true` | Whether periodic weight recalibration runs for all managed pools. |
| `RecalibrationIntervalSeconds` | `300` | Interval between automatic full-pool recalibration passes. |
| `HalfOpenPenaltyMultiplier` | `0.2` | Score multiplier for upstreams in the half-open state. Valid range (0, 1). |

## Usage

### Example 1: Default Registration
```csharp
// Program.cs
builder.Services
    .AddInfrastructureServices()
    .AddUpstreamManagement();
```

### Example 2: Customised Tuning
```csharp
builder.Services
    .AddInfrastructureServices()
    .AddUpstreamManagement(opts =>
    {
        opts.TargetLatencyMs = 150.0;
        opts.PenaltyMultiplier = 0.2;
        opts.PenaltyDecaySeconds = 45.0;
        opts.WeightAdaptationAlpha = 0.2;
        opts.AutoRecalibrationEnabled = true;
        opts.RecalibrationIntervalSeconds = 180;
    });
```

### Example 3: Routing a Request
```csharp
// Resolve the routing policy from the container, for example in a request handler
var policy = serviceProvider.GetRequiredService<HealthAwareRoutingPolicy>();

var context = new UpstreamSelectionContext(
    PoolId: "web-pool",
    ClientIp: "203.0.113.10",
    SessionToken: null);

Result<UpstreamServer> result = await policy.RouteAsync(context);
if (result.IsSuccess)
{
    // result carries the chosen UpstreamServer
}
```

## Notes

- **Lifetime**: Every registration is a singleton. Do not register these services as scoped or transient, or each request would start with empty metrics and default weights.
- **Dependencies**: Call `AddUpstreamManagement` after `AddInfrastructureServices`, so that `UpstreamManagerService` and `LoadBalancingOptions` are available when the adaptive components are resolved.
- **Single registration**: The method does not check for duplicate registrations. Calling it twice adds a second set of singleton registrations, and resolving a single-instance service then uses the last one registered.
- **Options visibility**: Because `UpstreamManagementOptions` is registered as an instance, an operator tool can resolve it to read the effective tuning values.
