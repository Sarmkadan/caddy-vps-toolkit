# LoggingMiddleware

`LoggingMiddleware.cs` defines the logging primitives used by the `caddy-vps-toolkit` project. The file contains one interface, `ILogger`, one enum, `LogLevel`, and two implementations: `FileLogger`, which appends entries to a log file and can echo them to the console, and `MemoryLogger`, which keeps entries in an in-memory list for tests and low-overhead scenarios. All types live in the `CaddyVpsToolkit.Middleware` namespace.

Every entry has the same format: `[<UTC timestamp in ISO 8601 "o" format>] [<level>] <message>`, for example `[2026-10-25T10:15:30.1234567Z] [Warning] Upstream health check timed out`. Entries below the configured minimum level are discarded before any output is produced.

## API

### ILogger
- **Signature:** `public interface ILogger`
- **Purpose:** The contract that consumers should depend on. Code that writes log entries takes an `ILogger` so the destination (file or memory) can be swapped without changing call sites.
- **Members:**

| Member | Description |
|--------|-------------|
| `Task LogInfoAsync(string message)` | Writes an entry at `LogLevel.Info`. |
| `Task LogWarningAsync(string message)` | Writes an entry at `LogLevel.Warning`. |
| `Task LogErrorAsync(string message)` | Writes an entry at `LogLevel.Error`. |
| `Task LogDebugAsync(string message)` | Writes an entry at `LogLevel.Debug`. |

### LogLevel
- **Signature:** `public enum LogLevel`
- **Purpose:** Severity of a log entry. Values are ordered from least to most severe, so a minimum level filters out everything below it.
- **Values:**

| Value | Integer | Description |
|-------|---------|-------------|
| `Debug` | `0` | Detailed diagnostics for development. |
| `Info` | `1` | Normal operational events. This is the default minimum level. |
| `Warning` | `2` | Unexpected but recoverable conditions. |
| `Error` | `3` | Failures that affect an operation. |

### FileLogger(string logPath, LogLevel minLevel = LogLevel.Info, bool consoleOutput = true)
- **Signature:** `public sealed class FileLogger : ILogger`
- **Purpose:** File-based logger. Appends entries to `logPath`, optionally writing each entry to the console as well.
- **Parameters:**
  - `logPath` - path to the log file. Must not be `null`. The parent directory is created if it does not exist.
  - `minLevel` - the lowest level that is recorded. Defaults to `LogLevel.Info`.
  - `consoleOutput` - when `true`, each entry is also written with `Console.WriteLine`. Defaults to `true`.
- **Throws:** `ArgumentNullException` (parameter name `logPath`) when `logPath` is `null`. Exceptions from `Directory.CreateDirectory` (for example, an unauthorized path) propagate to the caller.
- **Remarks:** The constructor does not create the file itself; the file is created on the first entry that is written. Console output happens before the file write and is not guarded by the file lock.

### FileLogger rotation
- **Purpose:** Prevents unbounded growth of the log file. Before each file write, if the file is larger than 10 MB (`10 * 1024 * 1024` bytes), it is moved to `<logPath>.<yyyyMMdd_HHmmss>` (UTC timestamp) and a new file is started.
- **Remarks:** Rotation is not configurable. Only one backup is produced per rotation, and older backups are not deleted. Two rotations within the same second would produce the same backup name; the second `File.Move` then fails and, because file errors are suppressed, that entry is lost.

### MemoryLogger(LogLevel minLevel = LogLevel.Info)
- **Signature:** `public sealed class MemoryLogger : ILogger`
- **Purpose:** In-memory logger. Stores formatted entries in a `List<string>` and performs no I/O. Intended for unit tests and for cases where logs are inspected programmatically.
- **Parameters:**
  - `minLevel` - the lowest level that is recorded. Defaults to `LogLevel.Info`.
- **Remarks:** Nothing is written to the console or to disk.

### MemoryLogger.GetLogs()
- **Signature:** `public List<string> GetLogs()`
- **Purpose:** Returns a snapshot of the recorded entries in the order they were written.
- **Returns:** A new `List<string>` copy. Changes to the returned list do not affect the logger.

### MemoryLogger.Clear()
- **Signature:** `public void Clear()`
- **Purpose:** Removes all recorded entries. The minimum level is unchanged.

## Usage

### Example 1: Registering the file logger with dependency injection
```csharp
// Registers ILogger as a singleton backed by FileLogger.
// Defaults: logs/app.log, LogLevel.Info, console output enabled.
services.AddLoggingServices();

// Or pick a path and a stricter level
services.AddLoggingServices("/var/log/caddy-vps/toolkit.log", LogLevel.Warning);
```

### Example 2: Writing entries from a service
```csharp
public sealed class ServiceHealthReporter
{
    private readonly ILogger _logger;

    public ServiceHealthReporter(ILogger logger)
    {
        _logger = logger;
    }

    public async Task ReportAsync(string serviceName, bool healthy)
    {
        if (healthy)
        {
            await _logger.LogInfoAsync($"Service '{serviceName}' is healthy");
        }
        else
        {
            await _logger.LogErrorAsync($"Service '{serviceName}' failed its health check");
        }
    }
}
```

### Example 3: Creating a logger manually
```csharp
// Debug and above, written to a file only (no console output)
ILogger logger = new FileLogger("logs/ssl-monitor.log", LogLevel.Debug, consoleOutput: false);

await logger.LogDebugAsync("Checking certificate for example.com");
await logger.LogWarningAsync("Certificate expires in 12 days");
```

### Example 4: Asserting on log output in tests
```csharp
// Entries below Warning are dropped, so only the warning is recorded
var logger = new MemoryLogger(LogLevel.Warning);

await logger.LogInfoAsync("Starting backup");
await logger.LogWarningAsync("Backup volume is 90% full");

List<string> entries = logger.GetLogs();
// entries.Count == 1
// entries[0] ends with "[Warning] Backup volume is 90% full"

logger.Clear();
```

## Notes

- **Level filtering**  
  The check is `level < minLevel`, so a logger created with `LogLevel.Warning` records `Warning` and `Error` only. Filtering happens before the timestamp is computed, so filtered calls cost almost nothing.

- **Error suppression in FileLogger**  
  Any exception raised while rotating or appending to the file is swallowed so that a logging failure cannot break the calling operation. Consequently, a full disk or a file that became unwritable after startup produces no error at all. Check the file if entries are expected but absent. Console output, if enabled, still appears. Errors from the constructor (see `FileLogger` above) are not suppressed.

- **Thread safety**  
  `FileLogger` serializes file access with a private lock, so one instance can be shared across threads. `MemoryLogger` has no lock: concurrent calls to `LogXxxAsync` can race on the underlying list. Use it from a single thread, or synchronize access externally.

- **Async methods**  
  The `LogXxxAsync` methods are asynchronous only in signature. `FileLogger` performs synchronous file I/O inside the lock, and `MemoryLogger` returns a completed task. Awaiting them does not move work off the calling thread.

- **Clock source**  
  Timestamps use `DateTime.UtcNow`, so entries from different hosts can be compared directly, but they carry no time zone other than UTC.

- **Disposal**  
  Neither implementation holds unmanaged resources, so neither implements `IDisposable`. The file is opened and closed for every write.

- **Related documentation**  
  See [ILogger](ILogger.md) for the per-method reference of the logger implementations.
