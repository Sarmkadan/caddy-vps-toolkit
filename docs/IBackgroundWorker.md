# IBackgroundWorker

`IBackgroundWorker` is the contract for long-running components that run in the background of `caddy-vps-toolkit`. It is defined in `BackgroundWorkers/IBackgroundWorker.cs` in the `CaddyVpsToolkit.BackgroundWorkers` namespace. The interface covers only the lifecycle: start, stop, a running flag, and a name. It says nothing about what the work does or how often it runs. Each implementation decides that.

The interface is `public`. It has no base interface and no generic parameters. The toolkit has two implementations: [HealthCheckWorker](HealthCheckWorker.md) and [MaintenanceWorker](MaintenanceWorker.md). [WorkerCoordinator](WorkerCoordinator.md) takes any `IBackgroundWorker` and starts or stops a set of them together.

## API

### `public interface IBackgroundWorker`
```csharp
public interface IBackgroundWorker
{
    Task StartAsync();
    Task StopAsync();
    bool IsRunning { get; }
    string WorkerName { get; }
}
```

The interface declares four members. The signatures are fixed. Any new implementation must provide all four.

### Methods

- `Task StartAsync()`: Starts the worker's background work and returns. It does not wait for the work to finish. Implementations start their loop on a separate task, so the returned task completes once startup is done.
- `Task StopAsync()`: Requests that the background work stop and waits for it to end. It returns once the worker has stopped.

### Properties

- `bool IsRunning { get; }`: `true` while the worker is active. The toolkit's implementations set it to `true` after `StartAsync` and back to `false` after `StopAsync` or cancellation.
- `string WorkerName { get; }`: A human-readable name used in logs. `WorkerCoordinator` logs it when a worker starts or stops, and when one fails. Each implementation returns a fixed value, such as `"HealthCheckWorker"`.

### Contract

The interface does not state these rules. The toolkit's implementations follow them, so new implementations should too:

- **Idempotent start**: Calling `StartAsync` on a running worker does nothing.
- **Idempotent stop**: Calling `StopAsync` on a stopped worker does nothing.
- **Restartable**: After `StopAsync`, `StartAsync` can start the worker again.
- **Stop waits**: `StopAsync` does not return while the background loop is still running, so a caller can rely on the worker being fully stopped when it returns.

## Usage

The examples below show a minimal implementation and how to control it. They assume the worker is used in the same project as the rest of the toolkit.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.BackgroundWorkers;

// Example 1: A minimal worker that runs a callback on a fixed interval
public sealed class CertificateReminderWorker : IBackgroundWorker
{
    private readonly TimeSpan _interval;
    private readonly Func<CancellationToken, Task> _pass;
    private CancellationTokenSource _cts;
    private Task _loop;

    public CertificateReminderWorker(TimeSpan interval, Func<CancellationToken, Task> pass)
    {
        _interval = interval;
        _pass = pass ?? throw new ArgumentNullException(nameof(pass));
    }

    public bool IsRunning => _cts is not null && !_cts.IsCancellationRequested;

    public string WorkerName => "CertificateReminderWorker";

    public Task StartAsync()
    {
        if (_cts is not null)
            return Task.CompletedTask; // Already running

        _cts = new CancellationTokenSource();
        _loop = RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is null)
            return; // Not running

        _cts.Cancel();
        try
        {
            await _loop;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }

        _cts.Dispose();
        _cts = null;
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await _pass(token);
            await Task.Delay(_interval, token).ContinueWith(_ => { });
        }
    }
}
```

```csharp
// Example 2: Starting and stopping a worker directly
var worker = new CertificateReminderWorker(
    TimeSpan.FromHours(6),
    ct => CheckExpiringCertificatesAsync(ct));

await worker.StartAsync();
Console.WriteLine($"{worker.WorkerName} running: {worker.IsRunning}"); // True

await worker.StopAsync();
Console.WriteLine($"Running after stop: {worker.IsRunning}"); // False
```

```csharp
// Example 3: Writing code that works with any worker
public static async Task RunWorkerForAsync(IBackgroundWorker worker, TimeSpan duration)
{
    await worker.StartAsync();
    try
    {
        await Task.Delay(duration);
    }
    finally
    {
        await worker.StopAsync();
    }

    Console.WriteLine($"{worker.WorkerName} stopped: {!worker.IsRunning}");
}
```

```csharp
// Example 4: Registering workers with WorkerCoordinator
var coordinator = new WorkerCoordinator(logger);

coordinator.Register("health", healthCheckWorker);     // HealthCheckWorker implements IBackgroundWorker
coordinator.Register("maintenance", maintenanceWorker); // MaintenanceWorker implements IBackgroundWorker
coordinator.Register("certs", worker);                  // Any IBackgroundWorker works

await coordinator.StartAllAsync();
// ... run the application ...
await coordinator.StopAllAsync();
```

## Notes

- **Start does not block**: `StartAsync` returns once the loop is scheduled. Do not use it to run work in the foreground. Keep the process alive some other way, such as a host or `Task.Delay` in `Program.cs`.
- **Name is for logs**: `WorkerName` is not a key. `WorkerCoordinator` keys its registrations by the `name` argument to `Register`, so the two names can differ. Keep `WorkerName` stable so log lines stay searchable.
- **Failures are caught by the coordinator**: `WorkerCoordinator.StartAllAsync` and `StopAllAsync` catch exceptions thrown by each worker and log them, so one failing worker does not stop the others. Calling a worker directly does not get this protection. Handle its exceptions yourself.
- **Cancellation is internal**: The interface has no `CancellationToken` parameter. Implementations create and cancel their own `CancellationTokenSource`, as the example does. Callers stop a worker only through `StopAsync`.
- **Related**: [WorkerCoordinator](WorkerCoordinator.md) manages a set of workers. [HealthCheckWorker](HealthCheckWorker.md) and [MaintenanceWorker](MaintenanceWorker.md) are the built-in implementations.
