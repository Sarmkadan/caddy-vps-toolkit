# WorkerCoordinator

The `WorkerCoordinator` class serves as the central management hub for orchestrating multiple background worker processes within the `caddy-vps-toolkit` application. It provides a unified interface to register, start, stop, and monitor the lifecycle of named workers, ensuring coordinated execution and status tracking across the system.

## API

### `public WorkerCoordinator`
Initializes a new instance of the `WorkerCoordinator` class. This constructor sets up the internal registry required to track worker states and prepares the coordinator for immediate registration of worker tasks.

### `public void Register`
Registers a new worker with the coordinator.
*   **Purpose**: Adds a worker definition to the internal collection, making it available for lifecycle management.
*   **Parameters**: Accepts the necessary configuration or delegate required to define the worker (specific parameter types depend on the internal implementation of the registration overload used).
*   **Return Value**: `void`.
*   **Exceptions**: May throw an exception if a worker with the same identifier is already registered or if the provided configuration is invalid.

### `public async Task StartAllAsync`
Asynchronously initiates execution for all registered workers.
*   **Purpose**: Iterates through the registered workers and triggers their start logic concurrently or sequentially based on internal policy.
*   **Parameters**: None.
*   **Return Value**: A `Task` that completes when all workers have successfully started or if a critical failure occurs during initialization.
*   **Exceptions**: Throws if a worker fails to start or if the coordinator is already in a running state that prevents re-initialization.

### `public async Task StopAllAsync`
Asynchronously halts execution for all currently running workers.
*   **Purpose**: Sends a stop signal to all active workers and awaits their graceful shutdown.
*   **Parameters**: None.
*   **Return Value**: A `Task` that completes when all workers have confirmed termination.
*   **Exceptions**: May throw if a worker fails to stop within a designated timeout or encounters an unhandled error during shutdown.

### `public string GetStatus`
Retrieves a textual summary of the current operational state of the coordinator and its workers.
*   **Purpose**: Provides a human-readable snapshot of the system health, including which workers are active, stopped, or in an error state.
*   **Parameters**: None.
*   **Return Value**: A `string` containing the formatted status report.
*   **Exceptions**: Generally does not throw unless the internal state is corrupted.

### `public List<string> GetWorkerNames`
Returns a list of identifiers for all workers currently registered with the coordinator.
*   **Purpose**: Allows enumeration of managed workers without accessing the underlying worker objects directly.
*   **Parameters**: None.
*   **Return Value**: A `List<string>` containing the names of registered workers.
*   **Exceptions**: Does not throw under normal conditions; returns an empty list if no workers are registered.

### `public bool IsWorkerRunning`
Checks the runtime status of a specific worker.
*   **Purpose**: Determines whether a specific named worker is currently executing.
*   **Parameters**: Accepts a `string` representing the name of the worker to check.
*   **Return Value**: `true` if the specified worker is active; otherwise, `false`.
*   **Exceptions**: May throw if the provided worker name is null or does not exist in the registry.

## Usage

### Example 1: Basic Lifecycle Management
The following example demonstrates initializing the coordinator, registering workers, and managing their startup and shutdown sequences.

```csharp
using CaddyVpsToolkit;

public class Program
{
    public static async Task Main(string[] args)
    {
        var coordinator = new WorkerCoordinator();

        // Register workers (assuming a standard registration signature)
        coordinator.Register("caddy-proxy", () => RunCaddy());
        coordinator.Register("health-check", () => RunHealthCheck());

        try 
        {
            // Start all registered workers
            await coordinator.StartAllAsync();
            
            // Application logic runs here
            await Task.Delay(TimeSpan.FromHours(1));
        }
        finally 
        {
            // Ensure graceful shutdown
            await coordinator.StopAllAsync();
        }
    }

    private static Task RunCaddy() { /* Implementation */ return Task.CompletedTask; }
    private static Task RunHealthCheck() { /* Implementation */ return Task.CompletedTask; }
}
```

### Example 2: Monitoring and Status Inspection
This example illustrates how to query the coordinator for specific worker states and generate a system status report.

```csharp
using System;
using CaddyVpsToolkit;

public class MonitorService
{
    private readonly WorkerCoordinator _coordinator;

    public MonitorService(WorkerCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public void PrintDiagnostics()
    {
        // Retrieve full status report
        Console.WriteLine(_coordinator.GetStatus());

        // Check specific worker state
        var workerName = "caddy-proxy";
        if (_coordinator.IsWorkerRunning(workerName))
        {
            Console.WriteLine($"{workerName} is operational.");
        }
        else
        {
            Console.WriteLine($"{workerName} is not running.");
        }

        // List all known workers
        var names = _coordinator.GetWorkerNames();
        Console.WriteLine($"Total registered workers: {names.Count}");
    }
}
```

## Notes

*   **Thread Safety**: While specific internal locking mechanisms are implementation-dependent, the presence of asynchronous start/stop methods alongside synchronous status checks (`GetStatus`, `IsWorkerRunning`) implies that state transitions may occur concurrently. Callers should treat status queries as point-in-time snapshots that may change immediately after return.
*   **Registration Timing**: The `Register` method is synchronous and should ideally be called before `StartAllAsync`. Registering workers while the coordinator is already running may lead to undefined behavior or immediate exceptions depending on the internal state machine.
*   **Error Propagation**: `StartAllAsync` and `StopAllAsync` aggregate results. If one worker fails to start or stop, the method may throw an exception, potentially leaving other workers in a mixed state. Implementations should verify the status of individual workers via `IsWorkerRunning` after bulk operations if partial failures are a concern.
*   **Name Uniqueness**: `GetWorkerNames` returns a list of strings, suggesting that worker identity is strictly string-based. Attempting to register duplicate names will likely result in an exception, as coordinators typically enforce unique keys for state tracking.
