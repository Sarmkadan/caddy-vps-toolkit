# ILogAggregationService

`ILogAggregationService` defines the contract for collecting and querying log entries from multiple sources within the caddy-vps-toolkit. Implementations aggregate logs from configured providers, expose the available source names, and return unified, read-only collections of `LogEntry` objects for downstream processing or display.

## API

### LogAggregationService()

```csharp
public LogAggregationService()
```

Parameterless constructor. Delegates to the overload that accepts a directory path, supplying `AppConstants.LogsDirectory` as the default log storage location.

### LogAggregationService(string logsDirectory)

```csharp
public LogAggregationService(string logsDirectory)
```

Constructs the service with an explicit directory path where log files or sources reside.

**Parameters**
- `logsDirectory` (`string`): The filesystem path to the directory containing log data.

**Exceptions**
- `ArgumentNullException`: Thrown when `logsDirectory` is `null`.
- `DirectoryNotFoundException`: Thrown when the specified directory does not exist at construction time.

### GetLogSources

```csharp
public IReadOnlyList<string> GetLogSources { get; }
```

Returns the names of all log sources currently available to the service. The returned list is read-only and reflects the state at the time of the property access.

**Return value**
- `IReadOnlyList<string>`: An immutable snapshot of source identifiers. May be empty if no sources are configured.

### GetLogsAsync

```csharp
public async Task<IReadOnlyList<LogEntry>> GetLogsAsync(
    string source,
    DateTime? startTime = null,
    DateTime? endTime = null,
    CancellationToken cancellationToken = default)
```

Asynchronously retrieves log entries from a specific source, optionally filtered by a time range.

**Parameters**
- `source` (`string`): The source identifier from which to fetch logs. Must match a value returned by `GetLogSources`.
- `startTime` (`DateTime?`): Inclusive lower bound for log entry timestamps. When `null`, no lower bound is applied.
- `endTime` (`DateTime?`): Exclusive upper bound for log entry timestamps. When `null`, no upper bound is applied.
- `cancellationToken` (`CancellationToken`): Token to cancel the asynchronous operation.

**Return value**
- `Task<IReadOnlyList<LogEntry>>`: A task that resolves to a read-only list of `LogEntry` instances matching the criteria, ordered chronologically.

**Exceptions**
- `ArgumentException`: Thrown when `source` is not a recognised log source.
- `OperationCanceledException`: Thrown when `cancellationToken` is triggered before the operation completes.

## Usage

### Example 1: Default construction and listing sources

```csharp
var aggregationService = new LogAggregationService();

IReadOnlyList<string> sources = aggregationService.GetLogSources;

foreach (string source in sources)
{
    Console.WriteLine($"Available source: {source}");
}
```

### Example 2: Retrieving filtered logs with cancellation support

```csharp
var aggregationService = new LogAggregationService("/var/log/caddy-vps");

DateTime from = DateTime.UtcNow.AddHours(-6);
DateTime to = DateTime.UtcNow;

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    IReadOnlyList<LogEntry> entries = await aggregationService.GetLogsAsync(
        "caddy-access",
        startTime: from,
        endTime: to,
        cancellationToken: cts.Token);

    foreach (LogEntry entry in entries)
    {
        Console.WriteLine($"[{entry.Timestamp:O}] {entry.Message}");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Log retrieval timed out.");
}
```

## Notes

- `GetLogSources` returns a snapshot; sources added or removed after the property is read are not reflected until the property is accessed again.
- `GetLogsAsync` returns entries in chronological order. When both `startTime` and `endTime` are `null`, all available entries for the source are returned, which may be a large data set.
- The `endTime` parameter is treated as exclusive. An entry timestamped exactly at `endTime` is not included in the result.
- The service does not perform internal caching between calls to `GetLogsAsync`; each invocation reads fresh data from the underlying storage.
- Thread safety: `GetLogSources` and `GetLogsAsync` are safe to call concurrently. The read-only collections returned eliminate the risk of external mutation, but callers should not rely on the underlying data remaining static between successive reads.
