#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Extension methods for <see cref="PerformanceMonitor"/> that provide additional performance monitoring utilities.
    /// </summary>
    public static class PerformanceMonitorExtensions
    {
        /// <summary>
        /// Measures the execution time of an action and returns both the result and timing information.
        /// </summary>
        /// <typeparam name="T">The return type of the action.</typeparam>
        /// <param name="monitor">The performance monitor instance.</param>
        /// <param name="action">The action to measure.</param>
        /// <returns>A tuple containing the action result and elapsed milliseconds.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="monitor"/> or <paramref name="action"/> is null.</exception>
        public static (T Result, long ElapsedMs) Measure<T>(this PerformanceMonitor monitor, Func<T> action)
        {
            ArgumentNullException.ThrowIfNull(monitor);
            ArgumentNullException.ThrowIfNull(action);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = action();
            stopwatch.Stop();

            return (result, stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Measures the execution time of an async action and returns both the result and timing information.
        /// </summary>
        /// <typeparam name="T">The return type of the async action.</typeparam>
        /// <param name="monitor">The performance monitor instance.</param>
        /// <param name="action">The async action to measure.</param>
        /// <returns>A tuple containing the action result and elapsed milliseconds.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="monitor"/> or <paramref name="action"/> is null.</exception>
        public static async System.Threading.Tasks.Task<(T Result, long ElapsedMs)> MeasureAsync<T>(this PerformanceMonitor monitor, Func<System.Threading.Tasks.Task<T>> action)
        {
            ArgumentNullException.ThrowIfNull(monitor);
            ArgumentNullException.ThrowIfNull(action);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await action().ConfigureAwait(false);
            stopwatch.Stop();

            return (result, stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Gets a formatted performance report with additional statistics including average, minimum, and maximum times.
        /// </summary>
        /// <param name="monitor">The performance monitor instance.</param>
        /// <param name="includeStatistics">Whether to include min/avg/max statistics in the report.</param>
        /// <returns>A formatted performance report string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="monitor"/> is null.</exception>
        public static string GetReport(this PerformanceMonitor monitor, bool includeStatistics = false)
        {
            ArgumentNullException.ThrowIfNull(monitor);

            if (!includeStatistics)
            {
                return monitor.GetReport();
            }

            var reportLines = new List<string>();
            var baseReport = monitor.GetReport();
            reportLines.AddRange(baseReport.Split(new[] { Environment.NewLine }, StringSplitOptions.None));

            // Get milestone times directly from the monitor
            var milestoneTimes = GetMilestoneTimes(monitor);
            if (milestoneTimes.Count > 0)
            {
                // Find the line with "Total Time:"
                for (int i = 0; i < reportLines.Count; i++)
                {
                    if (reportLines[i].StartsWith("Total Time:"))
                    {
                        // Insert statistics after total time
                        var totalTime = long.Parse(reportLines[i].Split(':')[1].Trim().Replace("ms", "").Trim(), CultureInfo.InvariantCulture);

                        var avg = milestoneTimes.Average();
                        var min = milestoneTimes.Min();
                        var max = milestoneTimes.Max();

                        reportLines.Insert(i + 1, string.Empty);
                        reportLines.Insert(i + 2, "Statistics:");
                        reportLines.Insert(i + 3, $" Average: {avg:F2}ms");
                        reportLines.Insert(i + 4, $" Min: {min}ms, Max: {max}ms");
                        break;
                    }
                }
            }

            return string.Join(Environment.NewLine, reportLines);
        }

        /// <summary>
        /// Gets the elapsed time of each milestone in milliseconds.
        /// </summary>
        /// <param name="monitor">The performance monitor instance.</param>
        /// <returns>An enumerable of milestone times in milliseconds.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="monitor"/> is null.</exception>
        private static IReadOnlyList<long> GetMilestoneTimes(this PerformanceMonitor monitor)
        {
            ArgumentNullException.ThrowIfNull(monitor);

            // Use reflection to access the private _milestones field since PerformanceMonitor doesn't expose it publicly
            var field = monitor.GetType().GetField("_milestones", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(monitor) is List<(string Name, long Time)> milestones)
            {
                return milestones.ConvertAll(x => x.Time);
            }

            return Array.Empty<long>();
        }

        /// <summary>
        /// Creates a child performance monitor that inherits the current elapsed time.
        /// Useful for measuring nested operations while preserving parent context.
        /// </summary>
        /// <param name="monitor">The parent performance monitor instance.</param>
        /// <param name="childOperationName">Name for the child operation.</param>
        /// <returns>A new performance monitor with inherited timing context.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="monitor"/> is null or <paramref name="childOperationName"/> is null or empty.</exception>
        public static PerformanceMonitor CreateChild(this PerformanceMonitor monitor, string childOperationName)
        {
            ArgumentNullException.ThrowIfNull(monitor);
            ArgumentException.ThrowIfNullOrEmpty(childOperationName);

            var childMonitor = new PerformanceMonitor(childOperationName);

            // Copy the elapsed time from parent to child by marking a milestone at the current elapsed time
            var elapsedMs = monitor.GetElapsedMs();
            childMonitor.MarkMilestone("Child started after " + elapsedMs + "ms");

            return childMonitor;
        }
    }
}