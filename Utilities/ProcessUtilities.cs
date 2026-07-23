#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System;
using System.Diagnostics;
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
            ArgumentException.ThrowIfNullOrEmpty(command);
            ArgumentException.ThrowIfNullOrEmpty(arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            try
            {
                process.Start();

                // Create a linked token source that combines timeout and cancellation
                // Default timeout of 30 seconds when only cancellation token is provided
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(30000));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                var linkedToken = linkedCts.Token;

                try
                {
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync(linkedToken);

                    var output = await outputTask;
                    var error = await errorTask;

                    return new ProcessResult
                    {
                        ExitCode = process.ExitCode,
                        Output = output,
                        Error = error,
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

            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

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
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync(linkedToken);

                    var output = await outputTask;
                    var error = await errorTask;

                    return new ProcessResult
                    {
                        ExitCode = process.ExitCode,
                        Output = output,
                        Error = error,
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
                foreach (var process in processes)
                    process.Dispose();
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
                foreach (var process in processes)
                    process.Dispose();
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
                    foreach (var process in processes)
                        process.Dispose();
                }
            }
            catch
            {
                return false;
            }
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
        /// Gets the process output or error, depending on which is available.
        /// </summary>
        /// <returns>The process output or error message.</returns>
        public string GetOutput()
        {
            return !string.IsNullOrEmpty(Error) ? Error : Output;
        }
    }
}