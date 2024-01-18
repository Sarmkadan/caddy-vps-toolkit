// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaddyVpsToolkit.Monitoring
{
    /// <summary>
    /// Collects application metrics for monitoring and analytics.
    /// Supports counters, gauges, and histograms.
    /// </summary>
    public class MetricsCollector
    {
        private readonly Dictionary<string, Counter> _counters = new();
        private readonly Dictionary<string, Gauge> _gauges = new();
        private readonly Dictionary<string, Histogram> _histograms = new();
        private readonly object _lockObject = new();

        /// <summary>
        /// Record a counter increment
        /// </summary>
        public void IncrementCounter(string name, long value = 1)
        {
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
        /// Record a gauge value
        /// </summary>
        public void SetGauge(string name, double value)
        {
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
        /// Record histogram value
        /// </summary>
        public void RecordHistogram(string name, double value)
        {
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

        public long GetCounter(string name)
        {
            lock (_lockObject)
            {
                return _counters.TryGetValue(name, out var counter) ? counter.Value : 0;
            }
        }

        public double GetGauge(string name)
        {
            lock (_lockObject)
            {
                return _gauges.TryGetValue(name, out var gauge) ? gauge.Value : 0;
            }
        }

        public HistogramStats GetHistogramStats(string name)
        {
            lock (_lockObject)
            {
                return _histograms.TryGetValue(name, out var histogram) ? histogram.GetStats() : null;
            }
        }

        public string GenerateReport()
        {
            var lines = new List<string> { "=== Metrics Report ===", "" };

            lock (_lockObject)
            {
                if (_counters.Count > 0)
                {
                    lines.Add("Counters:");
                    foreach (var kvp in _counters)
                        lines.Add($"  {kvp.Key}: {kvp.Value.Value}");
                    lines.Add("");
                }

                if (_gauges.Count > 0)
                {
                    lines.Add("Gauges:");
                    foreach (var kvp in _gauges)
                        lines.Add($"  {kvp.Key}: {kvp.Value.Value}");
                    lines.Add("");
                }

                if (_histograms.Count > 0)
                {
                    lines.Add("Histograms:");
                    foreach (var kvp in _histograms)
                    {
                        var stats = kvp.Value.GetStats();
                        lines.Add($"  {kvp.Key}: min={stats.Min}, max={stats.Max}, avg={stats.Average:F2}, count={stats.Count}");
                    }
                }
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    public class Counter
    {
        private long _value;
        public long Value => _value;

        public void Increment(long amount = 1)
        {
            _value += amount;
        }

        public void Reset()
        {
            _value = 0;
        }
    }

    public class Gauge
    {
        private double _value;
        public double Value => _value;

        public void Set(double value)
        {
            _value = value;
        }
    }

    public class Histogram
    {
        private readonly List<double> _values = new();

        public void Record(double value)
        {
            lock (_values)
            {
                _values.Add(value);
            }
        }

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

    public class HistogramStats
    {
        public int Count { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Average { get; set; }
        public double Median { get; set; }
    }
}
