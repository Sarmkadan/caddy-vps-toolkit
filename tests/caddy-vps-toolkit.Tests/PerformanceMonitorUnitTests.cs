#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CaddyVpsToolkit.Utilities;
using Xunit;

namespace CaddyVpsToolkit.Tests
{
    /// <summary>
    /// Unit tests for <see cref="PerformanceMonitor"/>.
    /// Covers construction, milestone handling, reporting, disposal and edge‑cases.
    /// </summary>
    public sealed class PerformanceMonitorUnitTests
    {
        [Fact]
        public void Constructor_ShouldInitializeOperationNameAndStartTimer()
        {
            using var monitor = new PerformanceMonitor("my-op");

            // The operation name must appear in the report header
            var report = monitor.GetReport();
            Assert.Contains("my-op", report);

            // Stopwatch starts immediately – elapsed time should be non‑negative
            var elapsed = monitor.GetElapsedMs();
            Assert.True(elapsed >= 0);
        }

        [Fact]
        public void MarkMilestone_ShouldRecordCorrectElapsedTimes()
        {
            using var monitor = new PerformanceMonitor("milestones-test");

            Thread.Sleep(10);
            monitor.MarkMilestone("first");

            Thread.Sleep(15);
            monitor.MarkMilestone("second");

            var report = monitor.GetReport();

            // Both milestone names must be present
            Assert.Contains("first", report);
            Assert.Contains("second", report);

            // Ensure total times are increasing
            var lines = report.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            var milestoneLines = lines
                .Where(l => l.TrimStart().StartsWith("first") || l.TrimStart().StartsWith("second"))
                .ToList();

            Assert.Equal(2, milestoneLines.Count);

            // Extract total times from the lines (format: "... total: {total}ms")
            long GetTotal(string line)
            {
                // Split on "total:" and take the part after it
                var parts = line.Split(new[] { "total:" }, StringSplitOptions.None);
                if (parts.Length < 2) return 0;
                var numberPart = parts[1].Trim();
                // Remove trailing "ms" if present
                if (numberPart.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
                    numberPart = numberPart.Substring(0, numberPart.Length - 2);
                return long.Parse(numberPart);
            }

            var firstTotal = GetTotal(milestoneLines[0]);
            var secondTotal = GetTotal(milestoneLines[1]);
            Assert.True(secondTotal > firstTotal);
        }

        [Fact]
        public void GetReport_ShouldContainHeaderAndMilestonesFormatting()
        {
            using var monitor = new PerformanceMonitor("report-test");
            monitor.MarkMilestone("step1");
            monitor.MarkMilestone("step2");

            var report = monitor.GetReport();
            var lines = report.Split(Environment.NewLine, StringSplitOptions.None);

            // Header lines
            Assert.StartsWith("Performance Report: report-test", lines[0]);
            Assert.StartsWith("Total Time:", lines[1]);

            // Milestones section header
            var milestoneHeaderIndex = Array.FindIndex(lines, l => l == "Milestones:");
            Assert.True(milestoneHeaderIndex >= 0, "Milestones header missing");

            // Each milestone line should start with two spaces
            for (int i = milestoneHeaderIndex + 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) break;
                Assert.StartsWith("  ", lines[i]);
            }
        }

        [Fact]
        public void Dispose_ShouldStopTimerWithoutThrowing()
        {
            var monitor = new PerformanceMonitor("dispose-test");
            Thread.Sleep(5);
            var before = monitor.GetElapsedMs();

            monitor.Dispose();

            // After disposal the elapsed time should stay the same (stopwatch is stopped)
            var after = monitor.GetElapsedMs();
            Assert.Equal(before, after);

            // Disposing again must not throw
            var exception = Record.Exception(() => monitor.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public void MarkMilestone_WithNullOrEmptyName_ShouldHandleGracefully()
        {
            using var monitor = new PerformanceMonitor("null-empty-test");

            monitor.MarkMilestone(null!);
            monitor.MarkMilestone(string.Empty);

            var report = monitor.GetReport();

            // Null or empty names result in a line that starts with ":" after trimming leading spaces
            var milestoneLines = report.Split(Environment.NewLine)
                                       .Where(l => l.TrimStart().StartsWith(":"))
                                       .ToList();

            // Expect at least two such lines (one for each call)
            Assert.True(milestoneLines.Count >= 2);
        }

        [Fact]
        public void GetElapsedMs_ShouldIncreaseOverTime()
        {
            using var monitor = new PerformanceMonitor("elapsed-increase-test");
            var start = monitor.GetElapsedMs();

            Thread.Sleep(20);
            var later = monitor.GetElapsedMs();

            Assert.True(later > start, $"Expected later ({later}) > start ({start})");
        }

        [Fact]
        public void UsingStatement_ShouldDisposeAutomatically()
        {
            PerformanceMonitor? captured = null;
            long elapsedAfterDispose;

            using (var monitor = new PerformanceMonitor("using-test"))
            {
                captured = monitor;
                Thread.Sleep(5);
            }

            // After the using block the monitor is disposed; GetElapsedMs should still be callable
            elapsedAfterDispose = captured!.GetElapsedMs();

            // Ensure no exception was thrown and elapsed is non‑negative
            Assert.True(elapsedAfterDispose >= 0);
        }

        [Fact]
        public void LargeNumberOfMilestones_ShouldProduceReportWithAllEntries()
        {
            using var monitor = new PerformanceMonitor("many-milestones-test");

            const int count = 10;
            for (int i = 0; i < count; i++)
            {
                Thread.Sleep(1);
                monitor.MarkMilestone($"m{i}");
            }

            var report = monitor.GetReport();

            // The report must contain exactly 'count' milestone lines after the "Milestones:" header
            var lines = report.Split(Environment.NewLine, StringSplitOptions.None);
            var milestoneHeaderIdx = Array.FindIndex(lines, l => l == "Milestones:");
            Assert.True(milestoneHeaderIdx >= 0, "Milestones header missing");

            var milestoneLines = lines.Skip(milestoneHeaderIdx + 1)
                                      .TakeWhile(l => !string.IsNullOrWhiteSpace(l))
                                      .ToList();

            Assert.Equal(count, milestoneLines.Count);
            // Verify that each expected milestone name appears
            for (int i = 0; i < count; i++)
            {
                Assert.Contains($"m{i}", milestoneLines[i]);
            }
        }
    }
}
