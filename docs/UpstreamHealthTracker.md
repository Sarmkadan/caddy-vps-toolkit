# UpstreamHealthTracker

The `UpstreamHealthTracker` class monitors the health of upstream endpoints by recording probe results and providing snapshots of their current state. It is intended for use in scenarios where periodic health checks are performed and consumers need to query the latest health status or wait for the tracker to be drained before shutdown.

## API

### `public UpstreamHealthTracker()`
Creates a new instance of `UpstreamHealthTracker`. The instance starts with no recorded probe results and is ready to accept health data immediately.

### `public async Task RecordProbeResultAsync(ProbeResult result)`
Records the outcome of a single health probe for an upstream endpoint.

- **Parameters**  
  - `result`: An object containing the probe outcome (e.g., success flag, latency, error details).  
- **Return Value**  
  - A `Task` that completes when the result has been stored internally.  
- **Exceptions**  
  - Throws `ArgumentNullException` if `result` is `null`.  
  - May throw `InvalidOperationException` if the tracker has been drained and is no longer accepting new results.

### `public async Task<UpstreamHealthSnapshot?> GetSnapshotAsync()`
Retrieves a snapshot of the current health state of the tracked upstream.

- **Parameters**  
  - None.  
- **Return Value**  
  - A `Task<UpstreamHealthSnapshot?>` yielding the latest snapshot, or `null` if no probe results have been recorded yet.  
- **Exceptions**  
  - Does not throw under normal conditions; any unexpected internal errors are propagated as the originating exception.

### `public async Task DrainAsync()`
Stops accepting new probe results and allows any in‑progress operations to finish.

- **Parameters**  
  - None.  
- **Return Value**  
  - A `Task` that completes when the tracker has been drained and no further results will be recorded.  
- **Exceptions**  
  - Throws `ObjectDisposedException` if called after the tracker has already been disposed or drained.  

## Usage

```csharp
var tracker = new UpstreamHealthTracker();

// Simulate a probe result
var probeResult = new ProbeResult { IsSuccessful = true, Latency = TimeSpan.FromMilliseconds(42) };
await tracker.RecordProbeResultAsync(probeResult);

// Obtain the latest health snapshot
var snapshot = await tracker.GetSnapshotAsync();
if (snapshot != null)
{
    Console.WriteLine($"Upstream healthy: {snapshot.IsHealthy}");
}
```

```csharp
// After all probing is complete, drain the tracker before application shutdown
await tracker.DrainAsync();

// At this point, RecordProbeResultAsync will reject further calls
```

## Notes

- The class is thread‑safe: multiple threads may call `RecordProbeResultAsync`, `GetSnapshotAsync`, and `DrainAsync` concurrently without external synchronization.  
- Once `DrainAsync` completes, subsequent calls to `RecordProbeResultAsync` will throw; `GetSnapshotAsync` will continue to return the last known snapshot (or `null` if none were recorded).  
- If no probe results have been recorded, `GetSnapshotAsync` returns `null`; callers should handle this case appropriately.  
- The tracker does not automatically expire old results; the snapshot reflects the aggregate of all results recorded prior to draining.  
- Implementations should avoid holding references to the tracker after `DrainAsync` has completed to prevent accidental use of a drained instance.
