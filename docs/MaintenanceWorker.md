# MaintenanceWorker

`BackgroundWorkers/MaintenanceWorker.cs` defines the `MaintenanceWorker` class, a background worker that periodically removes old health check records from the database. On each pass it computes a cutoff date (the current UTC time minus the retention period) and deletes every health check result recorded before that date through `IHealthCheckRepository`. It then sleeps for the configured interval. The first pass runs as soon as the worker starts. It implements `IBackgroundWorker`, so the same lifecycle code that starts and stops the other workers can manage it. The class lives in the `CaddyVpsToolkit.BackgroundWorkers` namespace and is `sealed`.

## API

### Constructor

* `public MaintenanceWorker(IHealthCheckRepository healthCheckRepo, ILogger logger, int intervalSeconds = 3600, int retentionDays = 30)`: Creates a new worker.
  * `healthCheckRepo`: Deletes the old health check records. Must not be `null`.
  * `logger`: Receives informational and error messages. Must not be `null`.
  * `intervalSeconds`: The delay between passes, in seconds. The default is 3600 (one hour). Values must be greater than zero; see the Notes section.
  * `retentionDays`: The number of days of health check history to keep. The default is 30. Records older than this are deleted on each pass; see the Notes section.
  * Throws `ArgumentNullException` if `healthCheckRepo` or `logger` is `null`.
  * The constructor does not validate `intervalSeconds` or `retentionDays`.

### Properties

* `IsRunning`: `true` after `StartAsync` has been called and until `StopAsync` (or cancellation) takes effect; otherwise `false`.
* `WorkerName`: Always returns `"MaintenanceWorker"`.

### Methods

* `public async Task StartAsync()`: Starts the background loop. If the worker is already running, the call returns without doing anything. Logs an informational message when the worker starts, including the interval and retention period.
* `public async Task StopAsync()`: Cancels the background loop and waits for it to finish. If the worker is not running, the call returns without doing anything. Logs an informational message when the worker stops.

## Usage

The following examples show how to construct and control the worker. The dependencies are assumed to be created elsewhere, for example by the dependency injection setup. Both `IHealthCheckRepository` (in `CaddyVpsToolkit.Data`) and `ILogger` (in `CaddyVpsToolkit.Middleware`) must be in scope.

```csharp
using CaddyVpsToolkit.BackgroundWorkers;

// Example 1: Creating a worker with the defaults (run hourly, keep 30 days of history)
var worker = new MaintenanceWorker(healthCheckRepo, logger);

await worker.StartAsync();
Console.WriteLine($"{worker.WorkerName} running: {worker.IsRunning}");

// Example 2: Running every 6 hours and keeping 7 days of health check history
var weeklyWorker = new MaintenanceWorker(
    healthCheckRepo,
    logger,
    intervalSeconds: 6 * 60 * 60,
    retentionDays: 7);

await weeklyWorker.StartAsync();

// Example 3: Stopping a worker during application shutdown
await weeklyWorker.StopAsync();
Console.WriteLine($"Running after stop: {weeklyWorker.IsRunning}"); // False

// Example 4: Calling StartAsync twice is safe; the second call is a no-op
await worker.StartAsync();
await worker.StartAsync();

// Example 5: Restarting a stopped worker creates a new loop
await worker.StopAsync();
await worker.StartAsync();
```

## Notes

When using the `MaintenanceWorker` type, consider the following:

* **What it deletes**: Each pass calls `DeleteOlderThanAsync` with the cutoff `DateTime.UtcNow.AddDays(-retentionDays)`. Only health check results are removed. Service records and other data are not touched.
* **Pass timing**: The first pass runs immediately after `StartAsync`. Each later pass starts `intervalSeconds` after the previous pass finished. The interval is a fixed delay, not a fixed schedule, and it has no jitter.
* **Logging**: A line is logged only when the repository reports that at least one record was deleted. Passes that delete nothing are silent, apart from the start and stop messages.
* **Per-pass errors**: If the delete call throws, the error is logged and the loop continues normally after the interval. The error is logged twice, once with a `PerformMaintenanceAsync failed` prefix and once with a `Failed to clean old health records` message. This is intentional duplication in the current code and does not change behavior.
* **Stopping**: `StopAsync` cancels the wait between passes and waits for the loop to finish. The delete call does not receive the cancellation token, so `StopAsync` waits for an in-progress delete to complete before it returns.
* **Restarting**: After `StopAsync`, `StartAsync` can be called again. Each start creates a new loop.
* **Interval values**: `intervalSeconds` is not validated. A value of `0` or a negative value makes `Task.Delay` return immediately or throw. A zero interval causes a tight loop of deletes, and a negative interval causes repeated error logs with no delay between them. Always pass a value greater than zero.
* **Retention values**: `retentionDays` is not validated. Zero deletes everything recorded before the current moment. A negative value moves the cutoff into the future, so it deletes recent records too. Use a value of at least `1` in production.
* **Database optimization**: The class summary mentions database optimization, but the current implementation only deletes old health check records. It does not run `VACUUM`, `ANALYZE`, or any other optimization step.
* **Thread safety**: `StartAsync` and `StopAsync` are not synchronized. Call them from one place (for example, the application's lifecycle code), not from several threads at the same time.
* **Time source**: The cutoff is calculated from `DateTime.UtcNow`. Health check timestamps must also be stored in UTC for the retention window to be accurate.
