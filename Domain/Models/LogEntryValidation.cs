#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Provides validation helpers for <see cref="LogEntry"/> instances.
    /// </summary>
    public static class LogEntryValidation
    {
        private static readonly HashSet<string> ValidLevels = new(StringComparer.OrdinalIgnoreCase)
        {
            "Debug", "Info", "Warning", "Error"
        };

        /// <summary>
        /// Validates a <see cref="LogEntry"/> instance and returns a list of validation problems.
        /// </summary>
        /// <param name="value">The log entry to validate.</param>
        /// <returns>A list of human-readable validation problems; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this LogEntry value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // Validate Timestamp - ensure it's a valid UTC DateTime
            if (value.Timestamp.Kind != DateTimeKind.Utc)
            {
                problems.Add("Timestamp must be a UTC DateTime value.");
            }
            else if (value.Timestamp == default)
            {
                problems.Add("Timestamp must be set to a non-default DateTime value.");
            }

            // Validate Level
            if (string.IsNullOrWhiteSpace(value.Level))
            {
                problems.Add("Level cannot be null, empty, or whitespace.");
            }
            else if (!ValidLevels.Contains(value.Level, StringComparer.OrdinalIgnoreCase))
            {
                problems.Add($"Level '{value.Level}' is not a valid log level. Valid levels are: Debug, Info, Warning, Error.");
            }

            // Validate Message
            if (string.IsNullOrWhiteSpace(value.Message))
            {
                problems.Add("Message cannot be null, empty, or whitespace.");
            }

            // Validate Source
            if (string.IsNullOrWhiteSpace(value.Source))
            {
                problems.Add("Source cannot be null, empty, or whitespace.");
            }

            // Validate ServiceId - if set, must not be whitespace
            if (value.ServiceId is not null && string.IsNullOrWhiteSpace(value.ServiceId))
            {
                problems.Add("ServiceId cannot be an empty or whitespace string when set.");
            }

            return problems;
        }

        /// <summary>
        /// Determines whether a <see cref="LogEntry"/> instance is valid.
        /// </summary>
        /// <param name="value">The log entry to check.</param>
        /// <returns>True if the log entry is valid; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static bool IsValid(this LogEntry value)
            => value.Validate().Count == 0;

        /// <summary>
        /// Ensures that a <see cref="LogEntry"/> instance is valid, throwing an <see cref="ArgumentException"/>
        /// with a detailed message if it is not.
        /// </summary>
        /// <param name="value">The log entry to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid.</exception>
        public static void EnsureValid(this LogEntry value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();
            if (problems.Count > 0)
            {
                throw new ArgumentException(
                    $"LogEntry is not valid. Problems: {string.Join(" ", problems)}",
                    nameof(value));
            }
        }
    }
}
