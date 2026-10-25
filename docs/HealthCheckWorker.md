# HealthCheckWorker
The `HealthCheckWorker` class is a background worker that periodically runs health checks against every enabled managed service. On each pass it loads all services through `ServiceManagementService`, checks each enabled one through `HealthMonitoringService`, and logs a warning for every unhealthy result. It then sleeps for the configured interval, with a random jitter so that multiple instances do not all fire at the same moment. It implements `IBackgroundWorker`, so it can be started and stopped by the same lifecycle code that manages other workers.

## API
### Constructor
* `public HealthCheckWorker(HealthMonitoringService healthMonitor, ServiceManagementService serviceManager, ILogger logger, int intervalSeconds = 300, double jitterPercent = 0.1)`: Creates a new worker.
  * `healthMonitor`: Performs the health check for each service. Must not be `null`.
  * `serviceManager`: Supplies the list of services to check. Must not be `null`.
  * `logger`: Receives informational, warning, and error messages. Must not be `null`.
  * `intervalSeconds`: The base delay between passes, in seconds. Must be greater than zero.
  * `jitterPercent`: The maximum random deviation from the base interval, as a fraction between `0` and `1` (for example, `0.1` means ±10%). Must be between `0` and `1` inclusive.
  * Throws `ArgumentNullException` if any dependency is `null`.
  * Throws `ArgumentOutOfRangeException` if `intervalSeconds` is zero or negative, or if `jitterPercent` is outside `[0, 1]`.

### Properties
* `IsRunning`: `true` after `StartAsync` has been called and until `StopAsync` (or cancellation) takes effect; otherwise `false`.
* `WorkerName`: Always returns `"HealthCheckWorker"`.

### Methods
* `public async Task StartAsync()`: Starts the background loop. If the worker is already running, the call returns without doing anything. Logs an informational message when the worker starts.
* `public async Task StopAsync()`: Cancels the background loop and waits for it to finish. If the worker is not running, the call returns without doing anything. Logs an informational message when the worker stops.

## Usage
The following examples show how to construct and control the worker. The dependencies are assumed to be created elsewhere, for example by the dependency injection setup.

```csharp
// Example 1: Creating a worker with the default interval (300 s) and jitter (10%)
var worker = new HealthCheckWorker(healthMonitor, serviceManager, logger);

await worker.StartAsync();
Console.WriteLine($"{worker.WorkerName} running: {worker.IsRunning}");

// Example 2: Checking every 60 seconds with ±5% jitter
var frequentWorker = new HealthCheckWorker(
    healthMonitor,
    serviceManager,
    logger,
    intervalSeconds: 60,
    jitterPercent: 0.05);

await frequentWorker.StartAsync();

// Example 3: Stopping a worker during application shutdown
await frequentWorker.StopAsync();
Console.WriteLine($"Running after stop: {frequentWorker.IsRunning}"); // False

// Example 4: Calling StartAsync twice is safe; the second call is a no-op
await worker.StartAsync();
await worker.StartAsync();
```

## Notes
When using the `HealthCheckWorker` type, consider the following:
* **Jitter**: Each delay is the base interval plus a random offset between `-jitterPercent` and `+jitterPercent` of that interval. With the defaults (300 s and 0.1), each delay falls between 270 and 330 seconds.
* **Per-service errors**: If checking one service throws, the error is logged with the service name and the pass continues with the next service. Disabled services (`IsEnabled == false`) are skipped.
* **Unhealthy results**: An unhealthy result is logged as a warning with the service name and response time. It does not change any other state.
* **Stopping**: `StopAsync` cancels only the wait between passes. The service-loading and health-check calls do not receive the cancellation token, so `StopAsync` waits for the current pass to complete before it returns.
* **Restarting**: After `StopAsync`, `StartAsync` can be called again. It creates a new loop each time.
* **Failure of the service list**: If `GetAllServicesAsync` throws, the error is logged and the loop starts again without waiting for the interval. If the failure persists, this produces repeated error logs with no delay between them. Use a longer interval or fix the underlying failure to avoid log flooding.
* **Thread safety**: `StartAsync` and `StopAsync` are not synchronized. Call them from one place (for example, the application's lifecycle code), not from several threads at the same time.
* **Random source**: Jitter uses a new, unseeded `System.Random` instance per worker, so the offsets are not reproducible between runs.
