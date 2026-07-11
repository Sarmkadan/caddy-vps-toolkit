#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Contract for reading and filtering aggregated log entries.
    /// </summary>
    public interface ILogAggregationService
    {
        /// <summary>
        /// Reads log entries from all discovered log files, applying the given filter options,
        /// and returns them sorted by timestamp descending (most recent first).
        /// </summary>
        Task<IReadOnlyList<LogEntry>> GetLogsAsync(LogQueryOptions? options = null);

        /// <summary>
        /// Returns the list of log file paths that will be scanned when aggregating logs.
        /// </summary>
        IReadOnlyList<string> GetLogSources();
    }

    /// <summary>
    /// Reads and aggregates log entries from the application log directory.
    /// Entries are parsed from the structured format written by <c>FileLogger</c>:
    /// <c>[ISO-8601-UTC] [Level] message</c>.
    /// Results are deduplicated, merged across log files, and returned sorted newest-first.
    /// </summary>
    public sealed class LogAggregationService : ILogAggregationService
    {
        // Matches: [2025-05-01T12:00:00.000+00:00] [Info] message text
        private static readonly Regex LogLineRegex = new(
            @"^\[(?<ts>[^\]]+)\]\s+\[(?<level>[^\]]+)\]\s+(?<msg>.+)$",
            RegexOptions.Compiled);

        private static readonly string[] KnownLevelOrder = { "Debug", "Info", "Warning", "Error" };

        private readonly string _logDirectory;

        /// <summary>
        /// Initializes a new instance of <see cref="LogAggregationService"/> using the default
        /// log directory from <see cref="AppConstants.LogsDirectory"/>.
        /// </summary>
        public LogAggregationService() : this(AppConstants.LogsDirectory) { }

        /// <summary>
        /// Initializes a new instance with an explicit log directory (useful for testing).
        /// </summary>
        public LogAggregationService(string logDirectory)
        {
            if (string.IsNullOrWhiteSpace(logDirectory))
                throw new ArgumentException("Log directory must not be empty.", nameof(logDirectory));

            _logDirectory = logDirectory;
        }

        /// <inheritdoc/>
        public IReadOnlyList<string> GetLogSources()
        {
            if (!Directory.Exists(_logDirectory))
                return Array.Empty<string>();

            return Directory.GetFiles(_logDirectory, "*.log", SearchOption.AllDirectories);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<LogEntry>> GetLogsAsync(LogQueryOptions? options = null)
        {
            options ??= new LogQueryOptions();

            var sources = GetLogSources();
            var allEntries = new List<LogEntry>();

            foreach (var filePath in sources)
            {
                var entries = await ReadLogFileAsync(filePath, options);
                allEntries.AddRange(entries);
            }

            // Sort newest-first then take the requested number of lines
            var sorted = allEntries
                .OrderByDescending(e => e.Timestamp)
                .Take(options.Lines)
                .ToList();

            return sorted.AsReadOnly();
        }

        // Reads a single log file and returns entries matching the filter options.
        private static async Task<List<LogEntry>> ReadLogFileAsync(string filePath, LogQueryOptions options)
        {
            var entries = new List<LogEntry>();

            string[] lines;
            try
            {
                lines = await File.ReadAllLinesAsync(filePath);
            }
            catch (IOException)
            {
                // Skip files that cannot be read (e.g., locked by another process)
                return entries;
            }

            var source = Path.GetFileName(filePath);

            // Derive a serviceId from the filename convention "service-<id>.log"
            string? serviceId = null;
            var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            if (nameWithoutExt.StartsWith("service-", StringComparison.OrdinalIgnoreCase))
                serviceId = nameWithoutExt["service-".Length..];

            // Apply service filter early
            if (!string.IsNullOrWhiteSpace(options.ServiceId) &&
                !string.Equals(serviceId, options.ServiceId, StringComparison.OrdinalIgnoreCase))
                return entries;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var entry = ParseLogLine(line, source, serviceId);
                if (entry is null)
                    continue;

                if (options.Since.HasValue && entry.Timestamp < options.Since.Value)
                    continue;

                if (!string.IsNullOrWhiteSpace(options.MinLevel) &&
                    CompareLevel(entry.Level, options.MinLevel) < 0)
                    continue;

                entries.Add(entry);
            }

            return entries;
        }

        // Parses a single log line into a LogEntry. Returns null for unrecognised formats.
        private static LogEntry? ParseLogLine(string line, string source, string? serviceId)
        {
            var match = LogLineRegex.Match(line);
            if (!match.Success)
                return null;

            if (!DateTime.TryParse(match.Groups["ts"].Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
                return null;

            return new LogEntry
            {
                Timestamp = timestamp.ToUniversalTime(),
                Level = NormalizeLevel(match.Groups["level"].Value.Trim()),
                Message = match.Groups["msg"].Value.Trim(),
                Source = source,
                ServiceId = serviceId
            };
        }

        // Normalizes level strings to canonical form.
        private static string NormalizeLevel(string raw) =>
            raw.ToLowerInvariant() switch
            {
                "debug" or "dbg" or "verbose" => "Debug",
                "info" or "information" => "Info",
                "warn" or "warning" => "Warning",
                "error" or "err" or "fatal" or "critical" => "Error",
                _ => raw
            };

        // Returns -1, 0, or 1 for level comparison. Unknown levels are treated as Info.
        private static int CompareLevel(string level, string minLevel)
        {
            var levelIdx = Array.IndexOf(KnownLevelOrder, level);
            var minIdx = Array.IndexOf(KnownLevelOrder, minLevel);
            if (levelIdx < 0) levelIdx = 1;
            if (minIdx < 0) minIdx = 1;
            return levelIdx.CompareTo(minIdx);
        }
    }
}
