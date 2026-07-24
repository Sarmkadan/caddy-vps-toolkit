#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Utilities for executing external processes with timeout and output capture.
    /// Handles both synchronous and asynchronous execution with proper resource cleanup.
    /// </summary>
    public static class ProcessUtilities
    {
        /// <summary>
        /// Maximum size for captured output (default: 10 MB)
        /// </summary>
        public const int DefaultMaxOutputSize = 10 * 1024 * 1024; // 10 MB

        /// <summary>
        /// Maximum size for captured error output (default: 10 MB)
        /// </summary>
        public const int DefaultMaxErrorSize = 10 * 1024 * 1024; // 10 MB

        /// <summary>
        /// Execute command and capture output with timeout
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="arguments">The command arguments</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 30000)</param>
        /// <returns>Process execution result</returns>
        /// <exception cref="ArgumentException">Thrown when command or arguments are null or empty</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timeoutMs is less than or equal to 0</exception>
        public static async Task<ProcessResult> ExecuteAsync(string command, string arguments, int timeoutMs = 30000)
        {
            ArgumentException.ThrowIfNullOrEmpty(command);
            ArgumentException.ThrowIfNullOrEmpty(arguments);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeoutMs, 0);

            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            return await ExecuteAsync(command, arguments, timeoutCts.Token);
        }

        /// <summary>
        /// Execute command and capture output with cancellation support
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="arguments">The command arguments</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Process execution result</returns>
        /// <exception cref="ArgumentException">Thrown when command or arguments are null or empty</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the cancellation token</exception>
        public static async Task<ProcessResult> ExecuteAsync(string command, string arguments, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(command, arguments, DefaultMaxOutputSize, DefaultMaxErrorSize, timeoutMs: 30000, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Execute command and capture output with timeout and cancellation support
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="arguments">The command arguments</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Process execution result</returns>
        /// <exception cref="ArgumentException">Thrown when command or arguments are null or empty</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timeoutMs is less than or equal to 0</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the cancellation token</exception>
        public static async Task<ProcessResult> ExecuteAsync(string command, string arguments, int timeoutMs, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(command);
            ArgumentException.ThrowIfNullOrEmpty(arguments);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeoutMs, 0);

            return await ExecuteAsync(command, arguments, DefaultMaxOutputSize, DefaultMaxErrorSize, outputCallback: null, errorCallback: null, timeoutMs: timeoutMs, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Execute command and capture output with size limits and optional streaming callback
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="arguments">The command arguments</param>
        /// <param name="maxOutputSize">Maximum size in bytes for standard output (0 = unlimited)</param>
        /// <param name="maxErrorSize">Maximum size in bytes for error output (0 = unlimited)</param>
        /// <param name="outputCallback">Optional callback for streaming output line by line</param>
        /// <param name="errorCallback">Optional callback for streaming error output line by line</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Process execution result</returns>
        /// <exception cref="ArgumentException">Thrown when command or arguments are null or empty</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timeoutMs is less than or equal to 0 and maxOutputSize/maxErrorSize are negative</exception>
        public static async Task<ProcessResult> ExecuteAsync(
            string command,
            string arguments,
            int maxOutputSize,
            int maxErrorSize,
            Action<string>? outputCallback = null,
            Action<string>? errorCallback = null,
            int timeoutMs = 30000,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(command);
            ArgumentException.ThrowIfNullOrEmpty(arguments);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeoutMs, 0);
            if (maxOutputSize < 0) throw new ArgumentOutOfRangeException(nameof(maxOutputSize), "Max output size cannot be negative");
            if (maxErrorSize < 0) throw new ArgumentOutOfRangeException(nameof(maxErrorSize), "Max error size cannot be negative");

            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Use ArgumentList for proper quoting/escaping instead of Arguments string
            // This prevents injection issues with spaces, quotes, and backslashes
            if (!string.IsNullOrEmpty(arguments))
            {
                var args = CommandLineToArgs(arguments);
                foreach (var arg in args)
                {
                    startInfo.ArgumentList.Add(arg);
                }
            }

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            using var process = new Process { StartInfo = startInfo };

            try
            {
                process.Start();

                // Create a linked token source that combines timeout and cancellation
                using var timeoutCts = new CancellationTokenSource(timeoutMs);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                var linkedToken = linkedCts.Token;

                try
                {
                    // Use StringBuilder to efficiently capture output with size limits
                    var outputBuilder = new StringBuilder();
                    var errorBuilder = new StringBuilder();
                    var outputLines = new StringBuilder();
                    var errorLines = new StringBuilder();

                    // Create tasks for reading output streams
                    var outputReadTask = ReadStreamWithLimitAsync(
                        process.StandardOutput,
                        outputBuilder,
                        maxOutputSize,
                        outputLines,
                        outputCallback,
                        linkedToken);

                    var errorReadTask = ReadStreamWithLimitAsync(
                        process.StandardError,
                        errorBuilder,
                        maxErrorSize,
                        errorLines,
                        errorCallback,
                        linkedToken);

                    await process.WaitForExitAsync(linkedToken);

                    // Wait for both read operations to complete
                    await Task.WhenAll(outputReadTask, errorReadTask);

                    return new ProcessResult
                    {
                        ExitCode = process.ExitCode,
                        Output = outputBuilder.ToString(),
                        Error = errorBuilder.ToString(),
                        OutputTruncated = outputBuilder.Length > maxOutputSize && maxOutputSize > 0,
                        ErrorTruncated = errorBuilder.Length > maxErrorSize && maxErrorSize > 0,
                        IsSuccess = process.ExitCode == 0
                    };
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    await TerminateProcessTreeAsync(process);
                    throw new OperationCanceledException("Process execution was canceled via CancellationToken", cancellationToken);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    await TerminateProcessTreeAsync(process);
                    return new ProcessResult
                    {
                        ExitCode = -1,
                        Output = "",
                        Error = "Process timeout",
                        IsSuccess = false,
                        TimedOut = true
                    };
                }
            }
            catch (Exception ex)
            {
                return new ProcessResult
                {
                    ExitCode = -1,
                    Output = "",
                    Error = ex.Message,
                    IsSuccess = false
                };
            }
        }

        /// <summary>
        /// Parse command line string into individual arguments using MSVCRT/CommandLineToArgvW rules.
        /// This provides proper handling of spaces, quotes, and backslashes.
        /// </summary>
        /// <param name="commandLine">The command line string to parse</param>
        /// <returns>Array of individual arguments</returns>
        /// <exception cref="ArgumentException">Thrown when commandLine is null or empty</exception>
        private static string[] CommandLineToArgs(string commandLine)
        {
            ArgumentException.ThrowIfNullOrEmpty(commandLine);

            var args = new System.Collections.Generic.List<string>();
            int i = 0;
            int n = commandLine.Length;

            while (i < n)
            {
                // Skip whitespace
                while (i < n && char.IsWhiteSpace(commandLine[i]))
                    i++;

                if (i >= n)
                    break;

                // Start of argument
                int start = i;
                bool inQuotes = false;

                while (i < n)
                {
                    if (commandLine[i] == '"')
                    {
                        inQuotes = !inQuotes;
                        i++;
                    }
                    else if (commandLine[i] == '\\' && i + 1 < n && commandLine[i + 1] == '"')
                    {
                        // Escaped quote - skip both characters
                        i += 2;
                    }
                    else if (char.IsWhiteSpace(commandLine[i]) && !inQuotes)
                    {
                        // End of argument
                        break;
                    }
                    else
                    {
                        i++;
                    }
                }

                // Extract argument
                string arg = commandLine.Substring(start, i - start);
                args.Add(arg);
            }

            return args.ToArray();
        }

        /// <summary>
        /// Read a stream with size limit and optional line-by-line callback
        /// </summary>
        private static async Task ReadStreamWithLimitAsync(
            StreamReader reader,
            StringBuilder outputBuilder,
            int maxSize,
            StringBuilder? lineBuffer,
            Action<string>? lineCallback,
            CancellationToken cancellationToken)
        {
            const int bufferSize = 4096;
            var buffer = new char[bufferSize];
            var bytesRead = 0;
            var pendingLine = new StringBuilder();

            while (!cancellationToken.IsCancellationRequested)
            {
                var readCount = await reader.ReadAsync(buffer, cancellationToken);

                if (readCount == 0)
                {
                    // End of stream
                    if (pendingLine.Length > 0 && lineCallback != null)
                    {
                        lineCallback(pendingLine.ToString());
                    }
                    break;
                }

                // Process character by character to detect line endings
                for (var i = 0; i < readCount; i++)
                {
                    var c = buffer[i];
                    pendingLine.Append(c);

                    if (c == '\n')
                    {
                        // Complete line ready
                        var line = pendingLine.ToString();
                        if (lineCallback != null)
                        {
                            lineCallback(line);
                        }
                        pendingLine.Clear();
                    }
                }

                // Check if we've exceeded the size limit
                if (maxSize > 0)
                {
                    var charsToAdd = readCount;
                    if (outputBuilder.Length + charsToAdd > maxSize)
                    {
                        // Calculate how many characters we can actually add
                        var remaining = maxSize - outputBuilder.Length;
                        if (remaining > 0)
                        {
                            outputBuilder.Append(buffer, 0, remaining);
                        }
                        // Skip the rest to enforce the limit
                        break;
                    }
                    else
                    {
                        outputBuilder.Append(buffer, 0, readCount);
                    }
                }
                else
                {
                    outputBuilder.Append(buffer, 0, readCount);
                }
            }
        }

        /// <summary>
        /// Terminate a process and its entire process tree to ensure all child processes are terminated
        /// </summary>
        /// <param name="process">The process to terminate</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        private static async Task TerminateProcessTreeAsync(Process process)
        {
            try
            {
                // Try to kill the entire process tree first (supported in .NET 5+)
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (PlatformNotSupportedException)
                {
                    // Fallback for older .NET versions - kill the process directly
                    process.Kill();
                }

                // Wait for the process to fully terminate and allow pipes to drain
                // Use a small timeout to avoid hanging indefinitely
                var terminationTask = Task.Run(() => process.WaitForExit());
                if (await Task.WhenAny(terminationTask, Task.Delay(2000)) == terminationTask)
                {
                    await terminationTask;
                }
                else
                {
                    // Process didn't terminate within 2 seconds, continue anyway
                }
            }
            catch (InvalidOperationException)
            {
                // Process exited between the cancellation and the kill attempt.
            }
            catch (Exception)
            {
                // Ignore other exceptions during termination
            }
        }

        /// <summary>
        /// Check if process is running by name
        /// </summary>
        /// <param name="processName">Name of the process to check</param>
        /// <returns>True if process is running, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when processName is null or empty</exception>
        public static bool IsProcessRunning(string processName)
        {
            ArgumentException.ThrowIfNullOrEmpty(processName);

            try
            {
                var processes = Process.GetProcessesByName(processName);
                var running = processes.Length > 0;
                foreach (var process in processes) process.Dispose();
                return running;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get process count by name
        /// </summary>
        /// <param name="processName">Name of the process to count</param>
        /// <returns>Number of processes with the given name</returns>
        /// <exception cref="ArgumentException">Thrown when processName is null or empty</exception>
        public static int GetProcessCount(string processName)
        {
            ArgumentException.ThrowIfNullOrEmpty(processName);

            try
            {
                var processes = Process.GetProcessesByName(processName);
                var count = processes.Length;
                foreach (var process in processes) process.Dispose();
                return count;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Kill process by name
        /// </summary>
        /// <param name="processName">Name of the process to kill</param>
        /// <returns>True if process was killed successfully, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when processName is null or empty</exception>
        public static bool KillProcess(string processName)
        {
            ArgumentException.ThrowIfNullOrEmpty(processName);

            try
            {
                var processes = Process.GetProcessesByName(processName);
                try
                {
                    foreach (var process in processes)
                    {
                        // Kill the entire process tree to ensure all child processes are terminated
                        try
                        {
                            process.Kill(entireProcessTree: true);
                        }
                        catch (PlatformNotSupportedException)
                        {
                            process.Kill();
                        }

                        // Wait for the process to fully terminate
                        process.WaitForExit();
                    }
                    return true;
                }
                finally
                {
                    foreach (var process in processes) process.Dispose();
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Result of process execution
        /// </summary>
        public sealed class ProcessResult
        {
            /// <summary>
            /// Gets or sets the exit code of the process.
            /// </summary>
            public int ExitCode { get; set; }

            /// <summary>
            /// Gets or sets the standard output from the process.
            /// </summary>
            public string Output { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the error output from the process.
            /// </summary>
            public string Error { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets a value indicating whether the process completed successfully.
            /// </summary>
            public bool IsSuccess { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the process timed out.
            /// </summary>
            public bool TimedOut { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the output was truncated due to exceeding size limits.
            /// </summary>
            public bool OutputTruncated { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the error output was truncated due to exceeding size limits.
            /// </summary>
            public bool ErrorTruncated { get; set; }

            /// <summary>
            /// Gets the process output or error, depending on which is available.
            /// </summary>
            /// <returns>The process output or error message.</returns>
            public string GetOutput()
            {
                return !string.IsNullOrEmpty(Error) ? Error : Output;
            }
        }
    }
}