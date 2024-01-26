#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Middleware
{
    /// <summary>
    /// Structured logging interface supporting multiple log levels and asynchronous operations.
    /// Logs are persisted to file and can be written to console based on configuration.
    /// </summary>
    public interface ILogger
    {
        Task LogInfoAsync(string message);
        Task LogWarningAsync(string message);
        Task LogErrorAsync(string message);
        Task LogDebugAsync(string message);
    }

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    /// <summary>
    /// File-based logger with console output option.
    /// Keeps a rotating log file to prevent unlimited growth.
    /// </summary>
    public sealed class FileLogger : ILogger
    {
        private readonly string _logPath;
        private readonly LogLevel _minLevel;
        private readonly bool _consoleOutput;
        private readonly object _lockObject = new();
        private const long MaxLogSizeBytes = 10 * 1024 * 1024; // 10MB

        public FileLogger(string logPath, LogLevel minLevel = LogLevel.Info, bool consoleOutput = true)
        {
            _logPath = logPath ?? throw new ArgumentNullException(nameof(logPath));
            _minLevel = minLevel;
            _consoleOutput = consoleOutput;

            // Ensure directory exists
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        public async Task LogInfoAsync(string message)
            => await LogAsync(LogLevel.Info, message);

        public async Task LogWarningAsync(string message)
            => await LogAsync(LogLevel.Warning, message);

        public async Task LogErrorAsync(string message)
            => await LogAsync(LogLevel.Error, message);

        public async Task LogDebugAsync(string message)
            => await LogAsync(LogLevel.Debug, message);

        private async Task LogAsync(LogLevel level, string message)
        {
            if (level < _minLevel)
                return;

            var timestamp = DateTime.UtcNow.ToString("o");
            var logEntry = $"[{timestamp}] [{level}] {message}";

            // Console output
            if (_consoleOutput)
            {
                Console.WriteLine(logEntry);
            }

            // File output with thread safety
            lock (_lockObject)
            {
                try
                {
                    // Check file size and rotate if needed
                    if (File.Exists(_logPath) && new FileInfo(_logPath).Length > MaxLogSizeBytes)
                    {
                        var backupPath = $"{_logPath}.{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                        File.Move(_logPath, backupPath);
                    }

                    File.AppendAllText(_logPath, logEntry + Environment.NewLine);
                }
                catch
                {
                    // Silently fail if logging fails to prevent cascading errors
                }
            }
        }
    }

    /// <summary>
    /// In-memory logger for testing and minimal overhead scenarios
    /// </summary>
    public sealed class MemoryLogger : ILogger
    {
        private readonly List<string> _logs = new();
        private readonly LogLevel _minLevel;

        public MemoryLogger(LogLevel minLevel = LogLevel.Info)
        {
            _minLevel = minLevel;
        }

        public async Task LogInfoAsync(string message)
            => await LogAsync(LogLevel.Info, message);

        public async Task LogWarningAsync(string message)
            => await LogAsync(LogLevel.Warning, message);

        public async Task LogErrorAsync(string message)
            => await LogAsync(LogLevel.Error, message);

        public async Task LogDebugAsync(string message)
            => await LogAsync(LogLevel.Debug, message);

        private async Task LogAsync(LogLevel level, string message)
        {
            if (level < _minLevel)
                return;

            var timestamp = DateTime.UtcNow.ToString("o");
            _logs.Add($"[{timestamp}] [{level}] {message}");
            await Task.CompletedTask;
        }

        public List<string> GetLogs() => new(_logs);
        public void Clear() => _logs.Clear();
    }
}
