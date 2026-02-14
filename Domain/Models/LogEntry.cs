#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Represents a single parsed log entry from any log source.
    /// </summary>
    public sealed class LogEntry
    {
        /// <summary>UTC timestamp of the log event.</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Severity level string: Debug, Info, Warning, Error.</summary>
        public string Level { get; set; } = "Info";

        /// <summary>Log message body.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Source log file path or component name.</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>Optional service ID when the entry originated from a service-scoped log.</summary>
        public string? ServiceId { get; set; }
    }

    /// <summary>
    /// Options for filtering and limiting log entries when querying aggregated logs.
    /// </summary>
    public sealed class LogQueryOptions
    {
        /// <summary>Maximum number of log lines to return (default: 100).</summary>
        public int Lines { get; set; } = 100;

        /// <summary>
        /// Minimum log level to include (Debug, Info, Warning, Error).
        /// Null or empty means all levels are included.
        /// </summary>
        public string? MinLevel { get; set; }

        /// <summary>Include only entries at or after this UTC time. Null means no lower bound.</summary>
        public DateTime? Since { get; set; }

        /// <summary>If specified, only include entries whose source path contains this service ID.</summary>
        public string? ServiceId { get; set; }
    }
}
