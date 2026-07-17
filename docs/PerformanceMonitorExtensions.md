# PerformanceMonitorExtensions

Provides extension methods and static helpers for measuring execution time of synchronous and asynchronous operations, aggregating milestone timings, and generating a human-readable performance report. It also exposes a factory for creating child monitors that can track nested or independent timing scopes.

## API

### Measure\<T\>

```csharp
public static (T Result, long ElapsedMs) Measure<T>(this PerformanceMonitor monitor, Func<T> action)
```

Executes a synchronous delegate and returns both its result and the wall-clock time taken in milliseconds.

- **Parameters:**
  - `monitor`: The `PerformanceMonitor` instance to record the measurement against.
  - `action`: The delegate to invoke and time.
- **Returns:** A tuple containing the result produced by `action` and the elapsed milliseconds as a `long`.
- **Exceptions:** Propagates any exception thrown by `action`; the timing is not recorded on failure.

### MeasureAsync\<T\>

```csharp
public static async Task<(T Result, long ElapsedMs)> MeasureAsync<T>(this PerformanceMonitor monitor, Func<Task<T>> asyncAction)
```

Asynchronously executes a task-returning delegate and returns both its result and the wall-clock time taken in milliseconds.

- **Parameters:**
  - `monitor`: The `PerformanceMonitor` instance to extend.
  - `asyncAction`: The asynchronous delegate to execute and time.
- **Returns:** A task that, when awaited, produces a tuple of the result and elapsed milliseconds.
- **Exceptions:** Thrown if `asyncAction` throws; timing is not recorded for failed invocations.

### GetReport

```csharp
public static string GetReport(this PerformanceMonitor monitor)
```

Generates a human-readable multi-line report summarising all recorded milestone timings.

- **Parameters:**
  - `monitor`: The monitor whose data to report.
- **Returns:** A formatted string containing milestone counts, individual times, and aggregate statistics.
- **Exceptions:** None.

### GetMilestoneCount

```csharp
public static int GetMilestoneCount(this PerformanceMonitor monitor)
```

Returns the total number of milestones recorded by the monitor.

- **Parameters:**
  - `monitor`: The monitor to query.
- **Returns:** The count of recorded milestones.
- **Exceptions:** None.

### GetMilestoneTimes

```csharp
public static IReadOnlyList<long> GetMilestoneTimes(this PerformanceMonitor monitor)
```

Returns a read-only list of all milestone elapsed times in milliseconds, in the order they were recorded.

- **Parameters:**
  - `monitor`: The monitor to query.
- **Returns:** An `IReadOnlyList<long>` containing the individual milestone durations.
- **Exceptions:** None.

### CreateChild

```csharp
public static PerformanceMonitor CreateChild(this PerformanceMonitor monitor)
```

Creates a new `PerformanceMonitor` that is linked to the parent monitor. Milestones recorded in the child are typically aggregated into the parent's report.

- **Parameters:**
  - `monitor`: The parent monitor.
- **Returns:** A new `PerformanceMonitor` instance associated with the parent.
- **Exceptions:** None.

## Usage

### Example 1: Measuring synchronous and asynchronous operations with a report

```csharp
var monitor = new PerformanceMonitor();

var (result, elapsed) = monitor.Measure(() =>
{
    // Simulate work
    Thread.Sleep(100);
    return 42;
});

Console.WriteLine($"Sync result: {result}, took {elapsed} ms");

var (asyncResult, asyncElapsed) = await monitor.MeasureAsync(async () =>
{
    await Task.Delay(200);
    return "done";
});

Console.WriteLine($"Async result: {asyncResult}, took {asyncElapsed} ms");

Console.WriteLine(monitor.GetReport());
```

### Example 2: Using child monitors for nested scopes

```csharp
var rootMonitor = new PerformanceMonitor();

rootMonitor.Measure(() =>
{
    var child = rootMonitor.CreateChild();

    child.Measure(() => Thread.Sleep(50));
    child.Measure(() => Thread.Sleep(30));

    Console.WriteLine($"Child milestones: {child.GetMilestoneCount()}");
    foreach (var time in child.GetMilestoneTimes())
    {
        Console.WriteLine($"  {time} ms");
    }

    return 0;
});

Console.WriteLine(rootMonitor.GetReport());
```

## Notes

- **Thread safety:** The methods do not provide internal synchronisation. If a `PerformanceMonitor` instance is shared across threads, callers must serialise access externally. `GetMilestoneTimes` returns a snapshot copy, so it is safe to iterate even if the underlying monitor is later mutated on another thread.
- **Exception handling:** `Measure` and `MeasureAsync` do not record a milestone when the supplied delegate throws. The exception propagates immediately, and the elapsed time is discarded.
- **Child monitors:** `CreateChild` returns a new instance that typically contributes its milestones to the parent's aggregate report. The exact aggregation behaviour depends on the `PerformanceMonitor` implementation, but callers should treat the child as a separate instance for milestone recording while expecting the parent's `GetReport` to include child data.
- **Report format:** The string returned by `GetReport` is intended for diagnostic output. Its exact layout is implementation-defined and should not be parsed programmatically.
- **Milestone ordering:** `GetMilestoneTimes` preserves insertion order. If multiple threads record milestones concurrently without external synchronisation, the order may not reflect real-time chronological sequence.
