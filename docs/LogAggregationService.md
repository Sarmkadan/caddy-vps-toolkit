# LogAggregationService

`LogAggregationService` reads the application's log files, parses them into `LogEntry` objects, and returns them merged across files, filtered, and sorted newest-first. It implements `ILogAggregationService` and lives in `Services/LogAggregationService.cs`.

It parses lines written in the structured format used by `FileLogger`:

```
[2025-05-01T12:00:00.000+00:00] [Info] message text
```

Lines that do not match this format are skipped silently.

The class is `sealed`. Its only state is the log directory path, so a single instance can be shared across threads and is registered as a singleton in `Program.cs`.

The public members are documented below. `TailAsync` is a public method on the class only. It is not part of `ILogAggregationService`.

## API

### `LogAggregationService()`

```csharp
public LogAggregationService()
```

Creates an instance that reads from `AppConstants.LogsDirectory`, which is `%PROGRAMDATA%/caddy-vps-toolkit/logs` after environment-variable expansion. On hosts where `PROGRAMDATA` is not defined, the path is left unexpanded, so pass an explicit directory instead.

### `LogAggregationService(string logDirectory)`

```csharp
public LogAggregationService(string logDirectory)
```

Creates an instance that reads from the given directory. The directory is not checked at construction time. A missing directory is handled per call, as described under `GetLogSources`.

**Parameters**
- `logDirectory` (`string`): Root directory that is scanned recursively for `*.log` files.

**Exceptions**
- `ArgumentException`: Thrown when `logDirectory` is `null`, empty, or whitespace. The parameter name is `logDirectory`.

### `GetLogSources()`

```csharp
public IReadOnlyList<string> GetLogSources()
```

Returns the full paths of every `*.log` file under the log directory, including subdirectories. Returns an empty list when the directory does not exist. This method is part of `ILogAggregationService`.

**Return value**
- `IReadOnlyList<string>`: Full file paths. The order is whatever `Directory.GetFiles` returns and is not guaranteed.

**Exceptions**
- `UnauthorizedAccessException` can propagate from the directory enumeration if a subdirectory is not readable.

### `GetLogsAsync(LogQueryOptions? options = null)`

```csharp
public async Task<IReadOnlyList<LogEntry>> GetLogsAsync(LogQueryOptions? options = null)
```

Reads every source returned by `GetLogSources`, applies the filters in `options`, sorts the combined result by `Timestamp` descending, and keeps the first `options.Lines` entries. This method is part of `ILogAggregationService`.

**Parameters**
- `options` (`LogQueryOptions?`): Query settings. When `null`, a default `LogQueryOptions` is used, which returns up to 100 entries with no other filters.

**Return value**
- `Task<IReadOnlyList<LogEntry>>`: A read-only list of at most `options.Lines` entries, newest first. The list is empty when nothing matches.

**Exceptions**
- `UnauthorizedAccessException` can propagate from file access. Files that fail with `IOException`, for example because another process has them locked, are skipped without an error.

### `TailAsync(string? serviceName, int lines, string? levelFilter = null)`

```csharp
public async Task<IReadOnlyList<LogEntry>> TailAsync(string? serviceName, int lines, string? levelFilter = null)
```

Returns the most recent entries for one service, or for all services, optionally restricted to a single level. Internally it loads every matching entry and then applies the limit, so memory use grows with the total size of the matching logs.

**Parameters**
- `serviceName` (`string?`): Service identifier to match against the `service-<id>.log` file name, compared case-insensitively. Pass `null` or whitespace to include all files.
- `lines` (`int`): Maximum number of entries to return. Must be greater than zero.
- `levelFilter` (`string?`): Exact level to keep, compared case-insensitively against the normalized `Level` of each entry (for example `Error` or `Warning`). Pass `null` or whitespace to include all levels.

**Return value**
- `Task<IReadOnlyList<LogEntry>>`: Up to `lines` entries, newest first.

**Exceptions**
- `ArgumentOutOfRangeException`: Thrown when `lines` is zero or negative. The parameter name is `lines`.

## Usage

### Example 1: Default query through the interface

The service is registered as a singleton, so consumers normally receive it through DI. The default query returns the 100 newest entries across all log files.

```csharp
public sealed class LogsController
{
    private readonly ILogAggregationService _logs;

    public LogsController(ILogAggregationService logs)
    {
        _logs = logs;
    }

    public Task<IReadOnlyList<LogEntry>> GetRecentAsync() =>
        _logs.GetLogsAsync();
}
```

### Example 2: Filtering by level and time window

```csharp
var logs = new LogAggregationService("/var/log/caddy-vps-toolkit");

var options = new LogQueryOptions
{
    Lines = 50,
    MinLevel = "Warning",               // canonical name: Debug, Info, Warning, Error
    Since = DateTime.UtcNow.AddHours(-6) // compared against UTC timestamps
};

IReadOnlyList<LogEntry> entries = await logs.GetLogsAsync(options);

foreach (LogEntry entry in entries)
{
    Console.WriteLine($"[{entry.Timestamp:O}] {entry.Level} {entry.Source}: {entry.Message}");
}
```

### Example 3: Tailing errors for one service

```csharp
var logs = new LogAggregationService("/var/log/caddy-vps-toolkit");

// Reads service-web.log (and any other file whose name maps to "web").
IReadOnlyList<LogEntry> errors = await logs.TailAsync(
    serviceName: "web",
    lines: 20,
    levelFilter: "Error");

foreach (LogEntry entry in errors)
{
    Console.WriteLine($"{entry.Timestamp:u} {entry.Message}");
}
```

### Example 4: Listing the files that will be scanned

```csharp
var logs = new LogAggregationService("/var/log/caddy-vps-toolkit");

foreach (string path in logs.GetLogSources())
{
    Console.WriteLine($"Scanning: {path}");
}
```

## Notes

- **Line format.** Only lines matching `[timestamp] [level] message` are parsed. Continuation lines, such as stack-trace frames, and lines with an unparseable timestamp are dropped. Blank lines are ignored.
- **Level normalization.** Level names are mapped to canonical forms: `dbg`, `verbose`, and `debug` become `Debug`; `information` and `info` become `Info`; `warn` and `warning` become `Warning`; `err`, `fatal`, `critical`, and `error` become `Error`. Other values are kept as written.
- **Level filtering uses canonical names.** `MinLevel` and `levelFilter` are not normalized. Pass `Warning`, not `warn`. Unrecognized `MinLevel` values, and unrecognized entry levels, are treated as `Info` for ordering. An unrecognized `levelFilter` matches only entries whose raw level text is identical to it, because unrecognized levels are not normalized.
- **Timestamps.** Each timestamp is parsed with the invariant culture and converted with `ToUniversalTime()`. Timestamps without a UTC offset are interpreted in the host's local time zone. Pass `Since` as a UTC `DateTime`, since it is compared directly against the stored UTC values. The lower bound is inclusive.
- **Service matching.** A file named `service-<id>.log` gets the `ServiceId` `<id>`. Other files have a `null` `ServiceId`. `LogQueryOptions.ServiceId` is matched against this file-name-derived value, not against the message content.
- **No deduplication.** Identical lines in different files, or repeated lines within one file, are each returned.
- **Source field.** `LogEntry.Source` is the file name only, without the directory.
- **Cancellation.** Neither method accepts a `CancellationToken`. Each file is read in full with `File.ReadAllLinesAsync`, so very large log files are held in memory for the duration of a call.
- **Concurrency.** Each call reads the files fresh, and nothing is cached between calls. Concurrent calls on one instance are safe because the instance holds no mutable state.
- **Limits.** `Lines` values that are zero or negative in `LogQueryOptions` produce an empty result rather than an exception. Only `TailAsync` validates its `lines` argument.
