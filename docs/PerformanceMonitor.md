# PerformanceMonitor

The `PerformanceMonitor` class provides a lightweight, disposable utility for measuring elapsed time and collecting performance metrics within a code block. It supports manual milestone markers, automatic timing of asynchronous operations, and generation of a human-readable report. Designed for diagnostic and profiling scenarios, it helps identify bottlenecks without introducing significant overhead.

## API

### `public PerformanceMonitor()`

Initializes a new instance of the `PerformanceMonitor` and starts the internal stopwatch. The constructor does not throw.

### `public void MarkMilestone()`

Records a milestone at the current elapsed time. Each call captures a timestamp relative to the monitor’s start. The method does not accept or return any value. It does not throw.

### `public long GetElapsedMs()`

Returns the total elapsed time in milliseconds since the monitor was created. The value is a `long` representing whole milliseconds (truncated). This method does not modify internal state. It does not throw.

### `public string GetReport()`

Returns a formatted string summarizing all recorded milestones and the total elapsed time. The exact format is implementation-defined but typically includes each milestone’s offset from start and the overall duration. This method does not throw.

### `public void Dispose()`

Stops the internal stopwatch and releases any resources held by the instance. After disposal, calling `GetElapsedMs` or `GetReport` may produce undefined results. This method does not throw.

### `public static async Task<(T result, long elapsedMs)> TimeAsync<T>(Func<Task<T>> operation)`

Executes the asynchronous `operation` delegate, measures its total execution time, and returns a tuple containing the operation’s result and the elapsed time in milliseconds. The method throws if `operation` is `null` or if the operation itself throws.

### `public static async Task<long> TimeAsync(Func<Task> operation)`

Executes the asynchronous `operation` delegate, measures its total execution time, and returns the elapsed time in milliseconds. The method throws if `operation` is `null` or if the operation itself throws.

### `public async Task MeasureAsync(Func<Task> operation)`

Executes the asynchronous `operation` delegate within the context of the current `PerformanceMonitor` instance. The elapsed time of the operation is recorded as a milestone (typically with an auto‑generated label). The method throws if `operation` is `null` or if the operation itself throws.

## Usage

### Example 1: Manual milestone tracking

```csharp
using var monitor = new PerformanceMonitor();

// Simulate work
Thread.Sleep(100);
monitor.MarkMilestone();

Thread.Sleep(200);
monitor.MarkMilestone();

Console.WriteLine($"Total: {monitor.GetElapsedMs()} ms");
Console.WriteLine(monitor.GetReport());
```

### Example 2: Timing an asynchronous operation

```csharp
async Task<string> FetchDataAsync()
{
    await Task.Delay(150);
    return "data";
}

var (result, elapsed) = await PerformanceMonitor.TimeAsync(FetchDataAsync);
Console.WriteLine($"Fetched '{result}' in {elapsed} ms");
```

## Notes

- **Thread safety**: Instance members (`MarkMilestone`, `GetElapsedMs`, `GetReport`, `MeasureAsync`, `Dispose`) are **not** thread‑safe. A single `PerformanceMonitor` instance should not be accessed concurrently from multiple threads. The static `TimeAsync` methods are safe to call concurrently from different threads because they create no shared state.
- **Disposal**: After `Dispose` is called, the internal stopwatch is stopped. Subsequent calls to `GetElapsedMs` or `GetReport` may return stale or meaningless values. Always use the `using` pattern or explicit `Dispose` when the monitor is no longer needed.
- **Milestone granularity**: `GetElapsedMs` truncates to whole milliseconds. For sub‑millisecond precision, consider using `Stopwatch` directly. The `MarkMilestone` method records offsets with the same precision as the underlying stopwatch.
- **Empty report**: If `GetReport` is called before any milestone is recorded, the report will contain only the total elapsed time (or an empty list of milestones, depending on implementation).
- **Null delegates**: Both `TimeAsync` overloads and `MeasureAsync` throw `ArgumentNullException` if the provided `operation` delegate is `null`.
