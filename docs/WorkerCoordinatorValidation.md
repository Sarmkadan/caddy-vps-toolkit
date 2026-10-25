# WorkerCoordinatorValidation

`WorkerCoordinatorValidation` is a static class of extension methods that check a `WorkerCoordinator` for problems in its current state and return a list of messages. It offers three entry points: `Validate` returns every problem found, `IsValid` returns a simple boolean, and `EnsureValid` throws an `ArgumentException` that lists all problems. All three are defined in the `CaddyVpsToolkit.BackgroundWorkers` namespace, so add `using CaddyVpsToolkit.BackgroundWorkers;` before calling them.

Validation looks at the coordinator's registered workers only. It checks that at least one worker is registered, that not every worker is running, and that no worker name is whitespace. It does not start, stop, or otherwise change any worker.

## API

### `Validate(this WorkerCoordinator value)`
Validates a `WorkerCoordinator` and collects every problem found. Validation does not stop at the first failure.

The following checks run, in this order:

| Check | Condition | Message |
|-------|-----------|---------|
| Registration | No workers are registered. | `WorkerCoordinator has no registered workers` |
| Running state | At least one worker is registered and every registered worker is running. | `All workers are running, which may indicate they were not properly stopped` |
| Names | A worker name is null, empty, or whitespace. One message is added per such name. | `Worker with empty name` |

**Parameters:**
- `value` (`WorkerCoordinator`) – The coordinator to validate.

**Returns:**
`IReadOnlyList<string>` – A read-only list of validation messages. The list is empty if the coordinator is valid.

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

### `IsValid(this WorkerCoordinator value)`
Returns whether the coordinator passes every check in `Validate`. It is equivalent to `value.Validate().Count == 0`, except that a `null` argument returns `false` instead of throwing.

**Parameters:**
- `value` (`WorkerCoordinator`) – The coordinator to check. May be `null`.

**Returns:**
`bool` – `true` if the coordinator has no validation problems; otherwise, `false`. Returns `false` if `value` is `null`.

**Throws:**
This method does not throw for a `null` argument.

### `EnsureValid(this WorkerCoordinator value)`
Validates the coordinator and throws if any problem is found. Use it at boundaries where an invalid coordinator should be rejected rather than handled.

**Parameters:**
- `value` (`WorkerCoordinator`) – The coordinator to validate.

**Returns:**
`void`

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

`ArgumentException` – If the coordinator is invalid. The exception message has the form `WorkerCoordinator is not valid. Problems: <problem 1>; <problem 2>`, listing every problem from `Validate` in order. This exception does not set a parameter name.

## Usage

### Example 1: Collecting all problems

A coordinator with no registered workers is invalid.

```csharp
using System;
using System.Collections.Generic;
using CaddyVpsToolkit.BackgroundWorkers;
using CaddyVpsToolkit.Middleware;

// logger is an existing ILogger implementation.
var coordinator = new WorkerCoordinator(logger);

IReadOnlyList<string> problems = coordinator.Validate();

foreach (var problem in problems)
{
    Console.WriteLine(problem);
}
// WorkerCoordinator has no registered workers
```

### Example 2: Checking validity with a boolean

```csharp
using System;
using CaddyVpsToolkit.BackgroundWorkers;

if (!coordinator.IsValid())
{
    Console.Error.WriteLine("Background workers are not configured; skipping startup.");
    return;
}

await coordinator.StartAllAsync();
```

### Example 3: Rejecting an invalid coordinator with an exception

`Register` rejects `null` and empty names but accepts whitespace names. The whitespace name is caught by validation.

```csharp
using System;
using CaddyVpsToolkit.BackgroundWorkers;

coordinator.Register(" ", worker); // worker is an existing IBackgroundWorker

try
{
    coordinator.EnsureValid();
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    // WorkerCoordinator is not valid. Problems: Worker with empty name
}
```

### Example 4: Checking a coordinator that may be null

`IsValid` accepts a `null` coordinator and returns `false`, so it can be used on a value that has not been assigned yet.

```csharp
using CaddyVpsToolkit.BackgroundWorkers;

WorkerCoordinator? coordinator = null;

bool valid = coordinator.IsValid(); // false, no exception
```

### Example 5: Detecting workers that were not stopped

The running-state check fires only when every registered worker is running. Call `EnsureValid` before starting workers, or after `StopAllAsync` has completed. Calling it while all workers are running throws.

```csharp
using System;
using CaddyVpsToolkit.BackgroundWorkers;

await coordinator.StartAllAsync();

foreach (var problem in coordinator.Validate())
{
    Console.WriteLine(problem);
}
// All workers are running, which may indicate they were not properly stopped

await coordinator.StopAllAsync();

coordinator.EnsureValid(); // passes if the coordinator has workers and none are running
```

### Example 6: Calling the static methods directly

The extension methods are also static methods on `WorkerCoordinatorValidation`, so they can be called without the `this` syntax:

```csharp
using CaddyVpsToolkit.BackgroundWorkers;

bool valid = WorkerCoordinatorValidation.IsValid(coordinator);
```

## Notes

- Validation reads the current state at the time of the call. A worker can start or stop immediately after `Validate` returns, so a valid result does not guarantee the state of any worker later on.
- The running-state check is a heuristic, not an error condition in the coordinator itself. A coordinator whose workers are all running is reported as invalid, so `IsValid` returns `false` and `EnsureValid` throws in that state.
- `Register` only rejects `null` and empty names. Whitespace names are accepted, and `Validate` reports them with the message `Worker with empty name`. The message text is the same for both cases, so it describes any blank name.
- `Validate` does not check whether a worker's `WorkerName` matches the name it was registered under. It checks only the registration key.
- Each call to `Validate` runs `GetWorkerNames` and `IsWorkerRunning`, and both log an informational message through the coordinator's `ILogger`. A single call writes one `GetWorkerNames` entry and one `IsWorkerRunning` entry per registered worker.
- `IsValid` returns `false` for a `null` argument, while `Validate` and `EnsureValid` throw `ArgumentNullException`. Check for `null` separately if the distinction matters to the caller.
- The class holds no state. The coordinator's own lock protects each query, but `Validate` makes several queries in sequence, so it does not read a single atomic snapshot of the registry.
