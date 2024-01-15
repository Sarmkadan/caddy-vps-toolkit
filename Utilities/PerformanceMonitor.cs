// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Measures performance of operations with detailed timing metrics.
    /// Useful for profiling and identifying bottlenecks.
    /// </summary>
    public class PerformanceMonitor : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly string _operationName;
        private readonly List<(string, long)> _milestones;

        public PerformanceMonitor(string operationName)
        {
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
            _milestones = new List<(string, long)>();
        }

        public void MarkMilestone(string name)
        {
            _milestones.Add((name, _stopwatch.ElapsedMilliseconds));
        }

        public long GetElapsedMs()
        {
            return _stopwatch.ElapsedMilliseconds;
        }

        public string GetReport()
        {
            var lines = new List<string>
            {
                $"Performance Report: {_operationName}",
                $"Total Time: {_stopwatch.ElapsedMilliseconds}ms",
                ""
            };

            if (_milestones.Count > 0)
            {
                lines.Add("Milestones:");
                long lastTime = 0;
                foreach (var milestone in _milestones)
                {
                    var elapsed = milestone.Item2 - lastTime;
                    lines.Add($"  {milestone.Item1}: +{elapsed}ms (total: {milestone.Item2}ms)");
                    lastTime = milestone.Item2;
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        public void Dispose()
        {
            _stopwatch?.Stop();
        }
    }

    /// <summary>
    /// Async operation timer for measuring async operations
    /// </summary>
    public class AsyncTimer
    {
        public static async Task<(T result, long elapsedMs)> TimeAsync<T>(Func<Task<T>> operation)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await operation();
            stopwatch.Stop();
            return (result, stopwatch.ElapsedMilliseconds);
        }

        public static async Task<long> TimeAsync(Func<Task> operation)
        {
            var stopwatch = Stopwatch.StartNew();
            await operation();
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }
    }

    /// <summary>
    /// Simple benchmark for comparing multiple operations
    /// </summary>
    public class Benchmark
    {
        private readonly Dictionary<string, List<long>> _results = new();

        public async Task MeasureAsync(string label, Func<Task> operation, int iterations = 1)
        {
            if (!_results.ContainsKey(label))
                _results[label] = new List<long>();

            for (int i = 0; i < iterations; i++)
            {
                var elapsed = await AsyncTimer.TimeAsync(operation);
                _results[label].Add(elapsed);
            }
        }

        public string GetReport()
        {
            var lines = new List<string> { "Benchmark Results:", "" };

            foreach (var kvp in _results)
            {
                var times = kvp.Value;
                var avg = 0.0;
                foreach (var t in times) avg += t;
                avg /= times.Count;

                var min = times[0];
                var max = times[0];
                foreach (var t in times)
                {
                    if (t < min) min = t;
                    if (t > max) max = t;
                }

                lines.Add($"{kvp.Key}:");
                lines.Add($"  Average: {avg:F2}ms");
                lines.Add($"  Min: {min}ms, Max: {max}ms");
                lines.Add($"  Iterations: {times.Count}");
                lines.Add("");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
