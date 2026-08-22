#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaddyVpsToolkit.Monitoring
{
    /// <summary>
    /// Collects application metrics for monitoring and analytics.
    /// Supports counters, gauges, histograms, and timers.
    /// <para>
    /// This class is thread-safe and provides methods to record and retrieve
    /// metric data. It uses an internal lock to ensure concurrent access safety.
    /// </para>
    /// </summary>
    public sealed class MetricsCollector
    {
        private readonly Dictionary<string, Counter> _counters = new();
        private readonly Dictionary<string, Gauge> _gauges = new();
        private readonly Dictionary<string, Histogram> _histograms = new();
        private readonly Dictionary<string, Timer> _timers = new();
        private readonly object _lockObject = new();

        /// <summary>
        /// Record a counter increment.
        /// </summary>
        /// <param name="name">The counter name.</param>
        /// <param name="value">The amount to increment (default 1).</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public void IncrementCounter(string name, long value = 1)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            lock (_lockObject)
            {
                if (!_counters.TryGetValue(name, out var counter))
                {
                    counter = new Counter();
                    _counters[name] = counter;
                }
                counter.Increment(value);
            }
        }

        /// <summary>
        /// Record a gauge value.
        /// </summary>
        /// <param name="name">The gauge name.</param>
        /// <param name="value">The gauge value to set.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public void SetGauge(string name, double value)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            lock (_lockObject)
            {
                if (!_gauges.TryGetValue(name, out var gauge))
                {
                    gauge = new Gauge();
                    _gauges[name] = gauge;
                }
                gauge.Set(value);
            }
        }

        /// <summary>
        /// Record histogram value.
        /// </summary>
        /// <param name="name">The histogram name.</param>
        /// <param name="value">The value to record.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public void RecordHistogram(string name, double value)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            lock (_lockObject)
            {
                if (!_histograms.TryGetValue(name, out var histogram))
                {
                    histogram = new Histogram();
                    _histograms[name] = histogram;
                }
                histogram.Record(value);
            }
        }

        /// <summary>
        /// Start a timer with the given name.
        /// </summary>
        /// <param name="name">The timer name.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public void StartTimer(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            lock (_lockObject)
            {
                if (!_timers.TryGetValue(name, out var timer))
                {
                    timer = new Timer();
                    _timers[name] = timer;
                }
                timer.Start();
            }
        }

        /// <summary>
        /// Stop a timer with the given name.
        /// </summary>
        /// <param name="name">The timer name.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public void StopTimer(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            lock (_lockObject)
            {
                if (_timers.TryGetValue(name, out var timer))
                {
                    timer.Stop();
                }
            }
        }

        /// <summary>
        /// Record a timing value directly.
        /// </summary>
        /// <param name="name">The timer name.</param>
        /// <param name="milliseconds">The timing value in milliseconds.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public void RecordTimer(string name, double milliseconds)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            lock (_lockObject)
            {
                if (!_timers.TryGetValue(name, out var timer))
                {
                    timer = new Timer();
                    _timers[name] = timer;
                }
                timer.Record(milliseconds);
            }
        }

        /// <summary>
        /// Returns the current cumulative value of the named counter, or <c>0</c> if it has never been incremented.
        /// </summary>
        /// <param name="name">Counter name.</param>
        /// <returns>The current cumulative value of the counter, or 0 if never incremented.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public long GetCounter(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            lock (_lockObject)
            {
                return _counters.TryGetValue(name, out var counter) ? counter.Value : 0;
            }
        }

        /// <summary>
        /// Returns the most recently recorded value for the named gauge, or <c>0</c> if it has never been set.
        /// </summary>
        /// <param name="name">Gauge name.</param>
        /// <returns>The most recently recorded value for the named gauge, or 0 if never set.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public double GetGauge(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            lock (_lockObject)
            {
                return _gauges.TryGetValue(name, out var gauge) ? gauge.Value : 0;
            }
        }

        /// <summary>
        /// Returns computed statistics for the named histogram, or <c>null</c> if no values have been recorded.
        /// </summary>
        /// <param name="name">Histogram name.</param>
        /// <returns>Computed statistics for the named histogram, or null if no values have been recorded.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public HistogramStats GetHistogramStats(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            lock (_lockObject)
            {
                return _histograms.TryGetValue(name, out var histogram) ? histogram.GetStats() : null;
            }
        }

        /// <summary>
        /// Returns computed statistics for the named timer, or <c>null</c> if no values have been recorded.
        /// </summary>
        /// <param name="name">Timer name.</param>
        /// <returns>Computed statistics for the named timer, or null if no values have been recorded.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public HistogramStats GetTimerStats(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            lock (_lockObject)
            {
                return _timers.TryGetValue(name, out var timer) ? timer.GetStats() : null;
            }
        }

        /// <summary>
        /// Generates a human-readable text report of all counters, gauges, histograms, and timers.
        /// </summary>
        /// <returns>A human-readable text report of all metrics.</returns>
        public string GenerateReport()
        {
            var lines = new List<string> { "=== Metrics Report ===", "" };

            lock (_lockObject)
            {
                if (_counters.Count > 0)
                {
                    lines.Add("Counters:");
                    foreach (var kvp in _counters)
                        lines.Add($" {kvp.Key}: {kvp.Value.Value}");
                    lines.Add("");
                }

                if (_gauges.Count > 0)
                {
                    lines.Add("Gauges:");
                    foreach (var kvp in _gauges)
                        lines.Add($" {kvp.Key}: {kvp.Value.Value}");
                    lines.Add("");
                }

                if (_histograms.Count > 0)
                {
                    lines.Add("Histograms:");
                    foreach (var kvp in _histograms)
                    {
                        var stats = kvp.Value.GetStats();
                        lines.Add($" {kvp.Key}: min={stats.Min}, max={stats.Max}, avg={stats.Average:F2}, count={stats.Count}");
                    }
                    lines.Add("");
                }

                if (_timers.Count > 0)
                {
                    lines.Add("Timers:");
                    foreach (var kvp in _timers)
                    {
                        var stats = kvp.Value.GetStats();
                        lines.Add($" {kvp.Key}: min={stats.Min:F2}ms, max={stats.Max:F2}ms, avg={stats.Average:F2}ms, count={stats.Count}");
                    }
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Creates a point-in-time snapshot of all metrics (counters, gauges, histograms, and timers).
        /// The snapshot is immutable and safe to use outside the lock.
        /// </summary>
        /// <returns>A point-in-time snapshot of all metrics.</returns>
        public SnapshotReport SnapshotReport()
        {
            lock (_lockObject)
            {
                // Create immutable snapshots of all metrics
                var counters = _counters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
                var gauges = _gauges.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);

                var histogramSnapshots = new Dictionary<string, HistogramStats>();
                foreach (var kvp in _histograms)
                {
                    histogramSnapshots[kvp.Key] = kvp.Value.GetStats();
                }

                var timerSnapshots = new Dictionary<string, HistogramStats>();
                foreach (var kvp in _timers)
                {
                    timerSnapshots[kvp.Key] = kvp.Value.GetStats();
                }

                return new SnapshotReport(
                    counters,
                    gauges,
                    histogramSnapshots,
                    timerSnapshots,
                    DateTime.UtcNow
                );
            }
        }
    }

    /// <summary>
    /// Thread-unsafe monotonically increasing counter. Guarded externally by <see cref="MetricsCollector"/>'s lock.
    /// </summary>
    public sealed class Counter
    {
        private long _value;
        /// <summary>Gets the current counter value.</summary>
        public long Value => _value;

        /// <summary>
        /// Increments the counter by the specified amount.
        /// </summary>
        /// <param name="amount">The amount to increment (default 1).</param>
        public void Increment(long amount = 1)
        {
            _value += amount;
        }

        /// <summary>
        /// Resets the counter to zero.
        /// </summary>
        public void Reset()
        {
            _value = 0;
        }
    }

    /// <summary>
    /// Holds a single mutable floating-point value that can be set arbitrarily.
    /// Guarded externally by <see cref="MetricsCollector"/>'s lock.
    /// </summary>
    public sealed class Gauge
    {
        private double _value;
        /// <summary>Gets the current gauge value.</summary>
        public double Value => _value;

        /// <summary>
        /// Sets the gauge to the specified value.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void Set(double value)
        {
            _value = value;
        }
    }

    /// <summary>
    /// Thread-safe timer that records elapsed time in milliseconds.
    /// Guarded externally by <see cref="MetricsCollector"/>'s lock.
    /// </summary>
    public sealed class Timer
    {
        private readonly Histogram _histogram = new();
        private DateTime? _startTime;

        /// <summary>
        /// Starts the timer.
        /// </summary>
        public void Start()
        {
            lock (_histogram)
            {
                _startTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Stops the timer and records the elapsed time.
        /// </summary>
        public void Stop()
        {
            lock (_histogram)
            {
                if (_startTime.HasValue)
                {
                    var elapsedMs = (DateTime.UtcNow - _startTime.Value).TotalMilliseconds;
                    _histogram.Record(elapsedMs);
                    _startTime = null;
                }
            }
        }

        /// <summary>
        /// Gets the statistics for the timer.
        /// </summary>
        /// <returns>The histogram statistics representing the timer's recorded values.</returns>
        public HistogramStats GetStats() => _histogram.GetStats();

        /// <summary>
        /// Records a timing value directly.
        /// </summary>
        /// <param name="milliseconds">The timing value in milliseconds to record.</param>
        public void Record(double milliseconds)
        {
            lock (_histogram)
            {
                _histogram.Record(milliseconds);
            }
        }
    }

    /// <summary>
    /// Accumulates an unbounded list of observed values and computes summary statistics on demand.
    /// Internal access is guarded by a per-instance lock.
    /// </summary>
    public sealed class Histogram
    {
        private readonly List<double> _values = new();

        /// <summary>
        /// Records a value in the histogram.
        /// </summary>
        /// <param name="value">The value to record.</param>
        public void Record(double value)
        {
            lock (_values)
            {
                _values.Add(value);
            }
        }

        /// <summary>
        /// Computes summary statistics for the recorded values.
        /// </summary>
        /// <returns>The histogram statistics, or an empty statistics object if no values have been recorded.</returns>
        public HistogramStats GetStats()
        {
            lock (_values)
            {
                if (_values.Count == 0)
                    return new HistogramStats();

                return new HistogramStats
                {
                    Count = _values.Count,
                    Min = _values.Min(),
                    Max = _values.Max(),
                    Average = _values.Average(),
                    Median = _values.OrderBy(v => v).ElementAt(_values.Count / 2)
                };
            }
        }
    }

    /// <summary>
    /// Immutable summary statistics computed from a <see cref="Histogram"/> sample set.
    /// </summary>
    public sealed class HistogramStats
    {
        /// <summary>Gets or sets the total number of recorded observations.</summary>
        public int Count { get; set; }

        /// <summary>Gets or sets the minimum observed value.</summary>
        public double Min { get; set; }

        /// <summary>Gets or sets the maximum observed value.</summary>
        public double Max { get; set; }

        /// <summary>Gets or sets the arithmetic mean of all observations.</summary>
        public double Average { get; set; }

        /// <summary>Gets or sets the median (50th percentile) of all observations.</summary>
        public double Median { get; set; }
    }

    /// <summary>
    /// Immutable point-in-time snapshot of all metrics collected by a <see cref="MetricsCollector"/>.
    /// Provides thread-safe access to counters, gauges, histograms, and timers at the moment the snapshot was taken.
    /// </summary>
    public sealed class SnapshotReport
    {
        /// <summary>Gets the UTC timestamp when the snapshot was created.</summary>
        public DateTime TimestampUtc { get; }

        /// <summary>Gets the counters captured in the snapshot.</summary>
        public IReadOnlyDictionary<string, long> Counters { get; }

        /// <summary>Gets the gauges captured in the snapshot.</summary>
        public IReadOnlyDictionary<string, double> Gauges { get; }

        /// <summary>Gets the histograms captured in the snapshot.</summary>
        public IReadOnlyDictionary<string, HistogramStats> Histograms { get; }

        /// <summary>Gets the timers captured in the snapshot.</summary>
        public IReadOnlyDictionary<string, HistogramStats> Timers { get; }

        /// <summary>
        /// Initializes a new immutable snapshot report.
        /// </summary>
        /// <param name="counters">Counter values at snapshot time.</param>
        /// <param name="gauges">Gauge values at snapshot time.</param>
        /// <param name="histograms">Histogram statistics at snapshot time.</param>
        /// <param name="timers">Timer statistics at snapshot time.</param>
        /// <param name="timestampUtc">UTC timestamp of when the snapshot was taken.</param>
        public SnapshotReport(
            IReadOnlyDictionary<string, long> counters,
            IReadOnlyDictionary<string, double> gauges,
            IReadOnlyDictionary<string, HistogramStats> histograms,
            IReadOnlyDictionary<string, HistogramStats> timers,
            DateTime timestampUtc)
        {
            TimestampUtc = timestampUtc;
            Counters = counters ?? new Dictionary<string, long>();
            Gauges = gauges ?? new Dictionary<string, double>();
            Histograms = histograms ?? new Dictionary<string, HistogramStats>();
            Timers = timers ?? new Dictionary<string, HistogramStats>();
        }

        /// <summary>
        /// Generates a human-readable text report from the snapshot data.
        /// </summary>
        /// <returns>A human-readable text report of the snapshot metrics.</returns>
        public string ToTextReport()
        {
            var lines = new List<string>();
            lines.Add("=== Metrics Snapshot Report ===");
            lines.Add($"Timestamp (UTC): {TimestampUtc:yyyy-MM-dd HH:mm:ss.fff}");
            lines.Add("");

            if (Counters.Count > 0)
            {
                lines.Add("Counters:");
                foreach (var kvp in Counters.OrderBy(kvp => kvp.Key))
                    lines.Add($" {kvp.Key}: {kvp.Value}");
                lines.Add("");
            }

            if (Gauges.Count > 0)
            {
                lines.Add("Gauges:");
                foreach (var kvp in Gauges.OrderBy(kvp => kvp.Key))
                    lines.Add($" {kvp.Key}: {kvp.Value}");
                lines.Add("");
            }

            if (Histograms.Count > 0)
            {
                lines.Add("Histograms:");
                foreach (var kvp in Histograms.OrderBy(kvp => kvp.Key))
                {
                    var stats = kvp.Value;
                    lines.Add($" {kvp.Key}: min={stats.Min}, max={stats.Max}, avg={stats.Average:F2}, median={stats.Median:F2}, count={stats.Count}");
                }
                lines.Add("");
            }

            if (Timers.Count > 0)
            {
                lines.Add("Timers:");
                foreach (var kvp in Timers.OrderBy(kvp => kvp.Key))
                {
                    var stats = kvp.Value;
                    lines.Add($" {kvp.Key}: min={stats.Min:F2}ms, max={stats.Max:F2}ms, avg={stats.Average:F2}ms, median={stats.Median:F2}ms, count={stats.Count}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}