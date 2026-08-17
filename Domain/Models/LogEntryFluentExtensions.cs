#nullable enable
using System;
using System.Text.RegularExpressions;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Provides fluent extension methods for <see cref="LogEntry"/>.
    /// </summary>
    public static class LogEntryFluentExtensions
    {
        /// <summary>
        /// Determines whether the log entry represents an error.
        /// </summary>
        /// <param name="entry">The <see cref="LogEntry"/> to evaluate.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="entry"/> is not <c>null</c> and its <c>Level</c> equals
        /// <c>"Error"</c> (case‑insensitive); otherwise, <c>false</c>.
        /// </returns>
        public static bool IsError(this LogEntry? entry)
        {
            return entry != null &&
                   string.Equals(entry.Level, "Error", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the log entry's message matches the supplied regular‑expression pattern.
        /// </summary>
        /// <param name="entry">The <see cref="LogEntry"/> whose <c>Message</c> will be tested.</param>
        /// <param name="pattern">
        /// A regular‑expression pattern. The pattern is passed directly to <see cref="Regex.IsMatch"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="entry"/> is not <c>null</c> and its <c>Message</c> matches the pattern;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool Matches(this LogEntry? entry, string pattern)
        {
            if (entry == null) return false;
            if (string.IsNullOrEmpty(pattern)) return false;

            return Regex.IsMatch(entry.Message ?? string.Empty, pattern);
        }

        /// <summary>
        /// Returns a compact, single‑line string representation of the log entry.
        /// </summary>
        /// <param name="entry">The <see cref="LogEntry"/> to format.</param>
        /// <returns>
        /// A string in the form <c>"{Timestamp:o} [{Level}] {Source}: {Message}"</c>.
        /// If <c>Source</c> is empty, it is omitted.
        /// </returns>
        public static string ToCompactString(this LogEntry? entry)
        {
            if (entry == null) return string.Empty;

            var timestamp = entry.Timestamp.ToString("o"); // ISO 8601 format
            var sourcePart = string.IsNullOrWhiteSpace(entry.Source) ? string.Empty : $" {entry.Source}:";

            return $"{timestamp} [{entry.Level}]{sourcePart} {entry.Message}";
        }
    }
}
