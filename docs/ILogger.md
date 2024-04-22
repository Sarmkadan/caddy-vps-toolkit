# ILogger
The `ILogger` type provides a unified abstraction for logging diagnostic information to both persistent file storage and an in‑memory buffer. It exposes separate logger implementations (`FileLogger` and `MemoryLogger`) each with asynchronous methods for logging at the standard levels (Info, Warning, Error, Debug). Additionally, it offers helpers to retrieve and clear the in‑memory log collection.

## API
### FileLogger
- **Description:** Gets the file‑backed logger instance used for persistent logging.
- **Type:** `FileLogger`
- **Parameters:** None
- **Return value:** The `FileLogger` object.
- **Exceptions:** None (property accessor does not throw).

### LogInfoAsync (FileLogger)
- **Description:** Asynchronously writes an informational message to the file log.
- **Parameters:** `string message` – the text to log.
- **Return value:** `Task` representing the asynchronous operation.
- **Exceptions:** 
  - `IOException` if the underlying file cannot be accessed or written to.
  - `ObjectDisposedException` if the logger has been disposed.

### LogWarningAsync (FileLogger)
- **Description:** Asynchronously writes a warning message to the file log.
- **Parameters:** `string message` – the warning text.
- **Return value:** `Task`
- **Exceptions:** Same as `LogInfoAsync`.

### LogErrorAsync (FileLogger)
- **Description:** Asynchronously writes an error message to the file log.
- **Parameters:** `string message` – the error text.
- **Return value:** `Task`
- **Exceptions:** Same as `LogInfoAsync`.

### LogDebugAsync (FileLogger)
- **Description:** Asynchronously writes a debug message to the file log.
- **Parameters:** `string message` – the debug text.
- **Return value:** `Task`
- **Exceptions:** Same as `LogInfoAsync`.

### MemoryLogger
- **Description:** Gets the memory‑backed logger instance used for volatile log storage.
- **Type:** `MemoryLogger`
- **Parameters:** None
- **Return value:** The `MemoryLogger` object.
- **Exceptions:** None (property accessor does not throw).

### LogInfoAsync (MemoryLogger)
- **Description:** Asynchronously appends an informational message to the in‑memory log.
- **Parameters:** `string message` – the text to store.
- **Return value:** `Task`
- **Exceptions:** 
  - `ObjectDisposedException` if the logger has been disposed.
  - `InvalidOperationException` if logging is temporarily disabled.

### LogWarningAsync (MemoryLogger)
- **Description:** Asynchronously appends a warning message to the in‑memory log.
- **Parameters:** `string message` – the warning text.
- **Return value:** `Task`
- **Exceptions:** Same as `LogInfoAsync` (MemoryLogger).

### LogErrorAsync (MemoryLogger)
- **Description:** Asynchronously appends an error message to the in‑memory log.
- **Parameters:** `string message` – the error text.
- **Return value:** `Task`
- **Exceptions:** Same as `LogInfoAsync` (MemoryLogger).

### LogDebugAsync (MemoryLogger)
- **Description:** Asynchronously appends a debug message to the in‑memory log.
- **Parameters:** `string message` – the debug text.
- **Return value:** `Task`
- **Exceptions:** Same as `LogInfoAsync` (MemoryLogger).

### GetLogs
- **Description:** Retrieves a snapshot of all messages currently stored in the memory logger.
- **Parameters:** None
- **Return value:** `List<string>` containing the logged messages in the order they were added.
- **Exceptions:** 
  - `ObjectDisposedException` if the logger has been disposed.

### Clear
- **Description:** Removes all messages from the memory logger.
- **Parameters:** None
- **Return value:** `void`
- **Exceptions:** 
  - `ObjectDisposedException` if the logger has been disposed.

## Usage
```csharp
// Example 1: Logging to a file
ILogger logger = new LoggerImplementation(); // concrete implementation of ILogger
await logger.FileLogger.LogInfoAsync("Application started.");
await logger.FileLogger.LogWarningAsync("Low disk space detected.");
await logger.FileLogger.LogErrorAsync("Failed to connect to database.");
await logger.FileLogger.LogDebugAsync("Entering ProcessData method.");
```

```csharp
// Example 2: Capturing logs in memory for later inspection
ILogger logger = new LoggerImplementation();
// Perform operations that log to the memory logger
await logger.MemoryLogger.LogInfoAsync("Step 1 completed.");
await logger.MemoryLogger.LogWarningAsync("Step 2 took longer than expected.");
await logger.MemoryLogger.LogErrorAsync("Step 3 failed with exception.");

// Retrieve and inspect the captured logs
List<string> logs = logger.GetLogs();
foreach (var entry in logs)
{
    Console.WriteLine(entry);
}

// Clear the in‑memory log for the next batch of operations
logger.Clear();
```

## Notes
- The `FileLogger` and `MemoryLogger` properties return distinct logger instances; their logging methods operate independently.
- All logging methods are asynchronous and should be awaited to ensure the log entry is persisted before proceeding.
- The `FileLogger` implementation is thread‑safe for concurrent writes; however, disposing the logger while operations are in progress will cause subsequent calls to throw `ObjectDisposedException`.
- The `MemoryLogger` stores logs in an unbounded list; calling `GetLogs` returns a copy, but frequent large calls may impact memory usage. The `Clear` method should be used periodically when the memory logger is employed for long‑running sessions.
- Neither logger validates the content of the `message` parameter; passing `null` will result in a `NullReferenceException` when the underlying logger attempts to write the value. It is the caller’s responsibility to supply non‑null strings. 
- If the underlying file for `FileLogger` becomes unavailable (e.g., due to permission changes or disk removal), subsequent logging attempts will throw `IOException`. Applications should handle such exceptions according to their error‑handling policy.
