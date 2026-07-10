#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Services;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Services
{
    public static class LogAggregationServiceTestsExtensions
    {
        /// <summary>
        /// Creates a temporary log file with the specified content and returns the file path.
        /// </summary>
        public static string CreateTempLogFile(this LogAggregationServiceTests _, string content)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"log-agg-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var filePath = Path.Combine(tempDir, "test.log");
            File.WriteAllText(filePath, content);
            return filePath;
        }

        /// <summary>
        /// Creates a temporary log file with multiple log entries and returns the file path.
        /// </summary>
        public static string CreateTempLogFileWithMultipleEntries(this LogAggregationServiceTests _, int count, string level = "Info", string messagePrefix = "Event")
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"log-agg-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var filePath = Path.Combine(tempDir, "test.log");

            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
            {
                var timestamp = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc).AddMinutes(i);
                lines.AppendLine($"[{timestamp:yyyy-MM-ddTHH:mm:ss.fffK}] [{level}] {messagePrefix} {i}");
            }

            File.WriteAllText(filePath, lines.ToString());
            return filePath;
        }

        /// <summary>
        /// Asserts that the log entries are in descending chronological order (newest first).
        /// </summary>
        public static void ShouldBeInDescendingChronologicalOrder(this IEnumerable<LogEntry> entries, string because = "")
        {
            var orderedEntries = entries.OrderByDescending(e => e.Timestamp).ToList();
            entries.Should().BeEquivalentTo(orderedEntries, because);
        }

        /// <summary>
        /// Asserts that the log entries contain exactly the expected messages in any order.
        /// </summary>
        public static void ShouldContainExactly(this IEnumerable<LogEntry> entries, params string[] expectedMessages)
        {
            var actualMessages = entries.Select(e => e.Message).ToList();
            actualMessages.Should().BeEquivalentTo(expectedMessages);
        }
    }
}