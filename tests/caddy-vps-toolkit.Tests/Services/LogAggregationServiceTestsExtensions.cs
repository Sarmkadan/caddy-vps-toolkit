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
using CaddyVpsToolkit.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Services
{
    /// <summary>
    /// Extension methods for testing <see cref="LogAggregationService"/> functionality.
    /// </summary>
    public static class LogAggregationServiceTestsExtensions
    {
        /// <summary>
        /// Creates a temporary log file with the specified content and returns the file path.
        /// </summary>
        /// <param name="_">The test fixture instance (unused).</param>
        /// <param name="content">The log content to write to the file.</param>
        /// <returns>The full path to the created temporary log file.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="content"/> is <see langword="null"/></exception>
        public static string CreateTempLogFile(this LogAggregationServiceTests _, string content)
        {
            ArgumentNullException.ThrowIfNull(content);

            var tempDir = Path.Combine(Path.GetTempPath(), $"log-agg-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var filePath = Path.Combine(tempDir, "test.log");
            File.WriteAllText(filePath, content);
            return filePath;
        }

        /// <summary>
        /// Creates a temporary log file with multiple log entries and returns the file path.
        /// </summary>
        /// <param name="_">The test fixture instance (unused).</param>
        /// <param name="count">Number of log entries to create.</param>
        /// <param name="level">Log level for each entry (default: "Info").</param>
        /// <param name="messagePrefix">Prefix for each message (default: "Event").</param>
        /// <returns>The full path to the created temporary log file.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="level"/> or <paramref name="messagePrefix"/> is <see langword="null"/></exception>
        public static string CreateTempLogFileWithMultipleEntries(
            this LogAggregationServiceTests _,
            int count,
            string level = "Info",
            string messagePrefix = "Event")
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
            ArgumentNullException.ThrowIfNull(level);
            ArgumentNullException.ThrowIfNull(messagePrefix);

            var tempDir = Path.Combine(Path.GetTempPath(), $"log-agg-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var filePath = Path.Combine(tempDir, "test.log");

            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
            {
                var timestamp = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc).AddMinutes(i);
                lines.AppendLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"[{timestamp:yyyy-MM-ddTHH:mm:ss.fffK}] [{level}] {messagePrefix} {i}"));
            }

            File.WriteAllText(filePath, lines.ToString());
            return filePath;
        }

        /// <summary>
        /// Asserts that the log entries are in descending chronological order (newest first).
        /// </summary>
        /// <param name="entries">The log entries to verify.</param>
        /// <param name="because">Optional reason for the assertion.</param>
        /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <see langword="null"/></exception>
        public static void ShouldBeInDescendingChronologicalOrder(this IEnumerable<LogEntry> entries, string because = "")
        {
            ArgumentNullException.ThrowIfNull(entries);

            var orderedEntries = entries.OrderByDescending(e => e.Timestamp).ToList();
            entries.Should().BeEquivalentTo(orderedEntries, because);
        }

        /// <summary>
        /// Asserts that the log entries contain exactly the expected messages in any order.
        /// </summary>
        /// <param name="entries">The log entries to verify.</param>
        /// <param name="expectedMessages">The expected message strings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="entries"/> or <paramref name="expectedMessages"/> is <see langword="null"/></exception>
        public static void ShouldContainExactly(this IEnumerable<LogEntry> entries, params string[] expectedMessages)
        {
            ArgumentNullException.ThrowIfNull(entries);
            ArgumentNullException.ThrowIfNull(expectedMessages);

            var actualMessages = entries.Select(e => e.Message).ToList();
            actualMessages.Should().BeEquivalentTo(expectedMessages);
        }
    }
}