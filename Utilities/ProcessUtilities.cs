#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
        public static async Task<ProcessResult> ExecuteAsync(string command, string arguments, int timeoutMs = 30000)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                try
                {
                    process.Start();

                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    if (!process.WaitForExit(timeoutMs))
                    {
                        process.Kill();
                        return new ProcessResult
                        {
                            ExitCode = -1,
                            Output = "",
                            Error = "Process timeout",
                            IsSuccess = false
                        };
                    }

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
        }

        /// <summary>
        /// Check if process is running by name
        /// </summary>
        public static bool IsProcessRunning(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                return processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get process count by name
        /// </summary>
        public static int GetProcessCount(string processName)
        {
            try
            {
                return Process.GetProcessesByName(processName).Length;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Kill process by name
        /// </summary>
        public static bool KillProcess(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    process.Kill();
                }
                return true;
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
        public int ExitCode { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public bool IsSuccess { get; set; }

        public string GetOutput()
        {
            return !string.IsNullOrEmpty(Error) ? Error : Output;
        }
    }
}
