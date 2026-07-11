# ProcessUtilities

Utility class for launching, monitoring, and managing external processes in a cross-platform way. It wraps `System.Diagnostics.Process` to provide a simpler, async-friendly API with result objects that capture exit codes, output streams, and success status.

## API

### `public static async Task<ProcessResult> ExecuteAsync(string fileName, string? arguments = null, string? workingDirectory = null, CancellationToken cancellationToken = default)`

Launches an external process and waits for it to complete, capturing standard output and error streams.

- **fileName**: Path to the executable to run.
- **arguments**: Optional command-line arguments passed to the executable.
- **workingDirectory**: Optional working directory in which to launch the process.
- **cancellationToken**: Token to observe for cancellation requests.
- **Return value**: A `ProcessResult` object containing the exit code, captured output/error streams, and success flag.
- **Exceptions**: Throws `OperationCanceledException` if the cancellation token is triggered; throws `Win32Exception`/`PlatformNotSupportedException`/`InvalidOperationException`/`IOException`/`UnauthorizedAccessException`/`FileNotFoundException`/`DirectoryNotFoundException`/`ArgumentException`/`ArgumentNullException`/`PathTooLongException`/`NotSupportedException`/`AggregateException` if the process cannot be started.

### `public static bool IsProcessRunning(string processName)`

Checks whether a process with the given name is currently running on the system.

- **processName**: Name of the process to check (e.g., "nginx").
- **Return value**: `true` if a matching process is found; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `processName` is `null`; throws `ArgumentException` if `processName` is empty or contains only whitespace.

### `public static int GetProcessCount(string processName)`

Counts how many processes with the given name are currently running.

- **processName**: Name of the process to count.
- **Return value**: Number of matching processes; returns `0` if none are found.
- **Exceptions**: Throws the same exceptions as `IsProcessRunning`.

### `public static bool KillProcess(string processName)`

Attempts to terminate all processes with the given name.

- **processName**: Name of the process to kill.
- **Return value**: `true` if at least one process was terminated; otherwise, `false`.
- **Exceptions**: Throws the same exceptions as `IsProcessRunning`.

### `public int ExitCode`

Gets the exit code of the process represented by the current `ProcessResult` instance.

- **Return value**: The numeric exit code returned by the process.
- **Exceptions**: Throws `InvalidOperationException` if the result object was not produced by a completed process.

### `public string Output`

Gets the standard output captured from the process.

- **Return value**: A string containing all captured standard output; may be empty.
- **Exceptions**: Throws `InvalidOperationException` if the result object was not produced by a completed process.

### `public string Error`

Gets the standard error captured from the process.

- **Return value**: A string containing all captured standard error; may be empty.
- **Exceptions**: Throws `InvalidOperationException` if the result object was not produced by a completed process.

### `public bool IsSuccess`

Indicates whether the process completed successfully (exit code `0`).

- **Return value**: `true` if the exit code is `0`; otherwise, `false`.
- **Exceptions**: Throws `InvalidOperationException` if the result object was not produced by a completed process.

### `public string GetOutput()`

Returns the captured standard output, trimmed of leading and trailing whitespace.

- **Return value**: Trimmed output string; empty if no output was captured.
- **Exceptions**: Throws `InvalidOperationException` if the result object was not produced by a completed process.

## Usage

```csharp
// Example 1: Run a command and inspect results
var result = await ProcessUtilities.ExecuteAsync("dotnet", "--version");
Console.WriteLine($"Version: {result.GetOutput()}");
if (!result.IsSuccess)
{
    Console.Error.WriteLine($"Failed: {result.Error}");
}

// Example 2: Ensure a process is not running before starting it
if (ProcessUtilities.IsProcessRunning("nginx"))
{
    ProcessUtilities.KillProcess("nginx");
}
```

## Notes

- All static methods are thread-safe and may be called concurrently from multiple threads.
- `ExecuteAsync` captures output and error streams only after the process terminates; long-running processes should be monitored via `cancellationToken` rather than relying on real-time streaming.
- `IsProcessRunning`, `GetProcessCount`, and `KillProcess` operate on process names without path or extension; on Unix-like systems they match against the executable name, while on Windows they match against the image name reported by the OS.
- `ProcessResult` members are read-only and safe to access from any thread after construction.
