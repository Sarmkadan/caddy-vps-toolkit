# HealthAwareRoutingPolicy

`HealthAwareRoutingPolicy` is a routing policy that selects upstream servers based on real-time health metrics and historical outcome data. It maintains a dynamic scoring system that accounts for server availability, response quality, and configurable weight adjustments, enabling adaptive load distribution in a Caddy VPS environment. The policy exposes methods for synchronous recalibration, asynchronous routing decisions, outcome feedback, and weight retrieval.

## API

### `HealthAwareRoutingPolicy`

The constructor initializes a new instance of the policy. It accepts configuration parameters that define health thresholds, scoring coefficients, and the set of upstream servers to manage. Exact parameters are defined in the source code.

**Throws:** `ArgumentException` if required configuration values are missing or invalid.

---

### `async Task<Result<UpstreamServer>> RouteAsync`

Selects the most suitable upstream server for the current request based on the policy’s scoring algorithm.

**Parameters:**  
- A routing context (e.g., `HttpContext` or a custom request object) that provides request metadata.

**Returns:**  
A `Result<UpstreamServer>` indicating success with the chosen server, or failure with an error description if no healthy server is available.

**Throws:**  
- `InvalidOperationException` if the policy has not been properly initialized.  
- `OperationCanceledException` if the operation is cancelled via the context’s cancellation token.

---

### `async Task NotifyOutcomeAsync`

Reports the outcome of a request that was routed to an upstream server. This feedback is used to update the server’s health score and adjust future routing decisions.

**Parameters:**  
- The `UpstreamServer` that handled the request.  
- An outcome object (e.g., `RequestOutcome`) containing success/failure status, response time, and optional error details.

**Returns:**  
A `Task` that completes when the outcome has been processed.

**Throws:**  
- `ArgumentNullException` if the server or outcome parameter is `null`.  
- `InvalidOperationException` if the policy is in an inconsistent state.

---

### `async Task<IReadOnlyList<UpstreamRoutingScore>> GetScoredCandidatesAsync`

Returns a snapshot of all candidate upstream servers with their current routing scores, ordered by descending score. This is useful for diagnostics and monitoring.

**Parameters:**  
- (Optional) A routing context to filter or weight scores based on request attributes. If omitted, all servers are returned.

**Returns:**  
A read-only list of `UpstreamRoutingScore` objects, each containing the server reference, its computed score, and any contributing health factors.

**Throws:**  
- `InvalidOperationException` if the policy has not been initialized.

---

### `Task RecalibrateAsync`

Triggers a full recalculation of internal health scores and effective weights based on the latest outcome history and configuration. This is typically called after a significant configuration change or on a periodic maintenance schedule.

**Parameters:**  
None.

**Returns:**  
A `Task` that completes when recalibration is finished.

**Throws:**  
- `InvalidOperationException` if the policy is not in a state that allows recalibration (e.g., during an ongoing routing operation).

---

### `Task<int> GetEffectiveWeightAsync`

Retrieves the current effective weight of a specified upstream server, as computed by the policy after applying health penalties and bonuses.

**Parameters:**  
- The `UpstreamServer` whose weight is requested.

**Returns:**  
An integer representing the server’s effective weight. A weight of zero indicates the server is currently excluded from routing.

**Throws:**  
- `ArgumentNullException` if the server parameter is `null`.  
- `KeyNotFoundException` if the server is not registered with the policy.

## Usage

### Example 1: Basic routing with outcome feedback

```csharp
var policy = new HealthAwareRoutingPolicy(config);
var context = new RoutingContext(request);

// Select a server
Result<UpstreamServer> result = await policy.RouteAsync(context);
if (result.IsSuccess)
{
    UpstreamServer server = result.Value;
    // Forward request to server...
    
    // After request completes, report outcome
    var outcome = new RequestOutcome(success: true, responseTimeMs: 120);
    await policy.NotifyOutcomeAsync(server, outcome);
}
else
{
    // Handle routing failure (e.g., return 503)
}
```

### Example 2: Periodic recalibration and weight inspection

```csharp
// Recalibrate after configuration update
await policy.RecalibrateAsync();

// Inspect effective weights for all servers
var candidates = await policy.GetScoredCandidatesAsync();
foreach (var candidate in candidates)
{
    int weight = await policy.GetEffectiveWeightAsync(candidate.Server);
    Console.WriteLine($"Server {candidate.Server.Id}: score={candidate.Score}, weight={weight}");
}
```

## Notes

- **Thread safety:** All public methods are thread-safe. Internal state is protected with appropriate synchronization primitives. Concurrent calls to `RouteAsync`, `NotifyOutcomeAsync`, and `RecalibrateAsync` are supported, though `RecalibrateAsync` may briefly block routing decisions while it acquires a write lock.
- **Edge cases:**  
  - If all servers have an effective weight of zero, `RouteAsync` returns a failed `Result` with an appropriate error.  
  - Calling `GetEffectiveWeightAsync` for a server that was removed from the policy after initialization throws `KeyNotFoundException`.  
  - `NotifyOutcomeAsync` ignores outcomes for servers that are no longer tracked, but does not throw.  
  - `RecalibrateAsync` is idempotent; calling it multiple times in succession is safe but may be wasteful.  
- **Initialization:** The policy must be fully configured before any routing or feedback methods are invoked. Attempting to use uninitialized instances results in `InvalidOperationException`.
