// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// An immutable point-in-time summary of aggregated request performance metrics derived from
    /// a sliding observation window for a single upstream server. All latency values are in
    /// milliseconds; the error rate is expressed as a fraction in [0.0, 1.0].
    /// </summary>
    /// <param name="UpstreamId">Identifier of the upstream server this summary describes.</param>
    /// <param name="SampleCount">Number of request observations that contributed to this summary.</param>
    /// <param name="P50LatencyMs">50th-percentile (median) response latency in milliseconds.</param>
    /// <param name="P95LatencyMs">95th-percentile response latency in milliseconds.</param>
    /// <param name="P99LatencyMs">
    /// 99th-percentile response latency in milliseconds. Used as the primary latency signal by the
    /// adaptive scoring model because it captures tail behaviour rather than typical performance.
    /// </param>
    /// <param name="MeanLatencyMs">Arithmetic mean response latency in milliseconds.</param>
    /// <param name="ErrorRate">Fraction of samples that resulted in a request error, in [0.0, 1.0].</param>
    /// <param name="ThroughputRps">Estimated request throughput in requests per second over the window duration.</param>
    /// <param name="WindowStartUtc">UTC timestamp marking the opening of the observation window.</param>
    /// <param name="WindowEndUtc">UTC timestamp marking the closing of the observation window.</param>
    public record UpstreamMetricsSummary(
        string   UpstreamId,
        int      SampleCount,
        double   P50LatencyMs,
        double   P95LatencyMs,
        double   P99LatencyMs,
        double   MeanLatencyMs,
        double   ErrorRate,
        double   ThroughputRps,
        DateTime WindowStartUtc,
        DateTime WindowEndUtc
    )
    {
        /// <summary>
        /// Gets whether this summary was derived from a statistically meaningful sample set.
        /// Summaries based on fewer than five observations should be treated as provisional estimates
        /// and their percentile values weighted less heavily by the adaptive scoring model.
        /// </summary>
        public bool IsStatisticallySignificant => SampleCount >= 5;

        /// <summary>
        /// Returns a single-line human-readable description of the most critical performance metrics,
        /// suitable for log output or status displays.
        /// </summary>
        public override string ToString() =>
            $"[{UpstreamId}] n={SampleCount} p50={P50LatencyMs:F1}ms p99={P99LatencyMs:F1}ms " +
            $"err={ErrorRate:P1} rps={ThroughputRps:F2}";
    }

    /// <summary>
    /// A single request observation captured for a specific upstream server. Observations accumulate
    /// inside an <see cref="UpstreamMetricsWindow"/> and are used by the
    /// <see cref="CaddyVpsToolkit.LoadBalancing.IMetricsAggregator"/> to compute latency percentiles,
    /// error rates, and throughput estimates.
    /// </summary>
    /// <param name="ResponseTimeMs">Round-trip time of the observed request in milliseconds.</param>
    /// <param name="Succeeded">Whether the request completed without an error.</param>
    /// <param name="RecordedAt">UTC timestamp when this observation was captured.</param>
    public record RequestSample(int ResponseTimeMs, bool Succeeded, DateTime RecordedAt);

    /// <summary>
    /// Mutable ring-buffer container that accumulates <see cref="RequestSample"/> observations for
    /// a single upstream server within a bounded sliding window. When the window reaches its capacity,
    /// the oldest observation is evicted to make room for the newest entry.
    /// <para>
    /// Thread safety for concurrent <see cref="Add"/> calls must be coordinated externally by the
    /// owning aggregator implementation.
    /// </para>
    /// </summary>
    public sealed class UpstreamMetricsWindow
    {
        private readonly int _maxSamples;
        private readonly Queue<RequestSample> _samples;

        /// <summary>Gets the upstream server identifier this window is tracking.</summary>
        public string UpstreamId { get; }

        /// <summary>
        /// Gets the UTC timestamp when the first sample in the current window was recorded.
        /// Used to calculate throughput (samples ÷ elapsed seconds) in <see cref="Summarize"/>.
        /// </summary>
        public DateTime WindowStartUtc { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// Initialises a new <see cref="UpstreamMetricsWindow"/> for the specified upstream.
        /// </summary>
        /// <param name="upstreamId">The upstream server identifier. Must not be null or whitespace.</param>
        /// <param name="maxSamples">
        /// Maximum number of observations retained in the sliding window. Older samples are evicted
        /// when this limit is exceeded. Must be at least 1. Defaults to <c>200</c>.
        /// </param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="upstreamId"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxSamples"/> is less than 1.</exception>
        public UpstreamMetricsWindow(string upstreamId, int maxSamples = 200)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(upstreamId);
            if (maxSamples < 1)
                throw new ArgumentOutOfRangeException(nameof(maxSamples), "Window capacity must be at least 1.");

            UpstreamId  = upstreamId;
            _maxSamples = maxSamples;
            _samples    = new Queue<RequestSample>(_maxSamples + 1);
        }

        /// <summary>
        /// Adds a new observation to the window, evicting the oldest sample when the window is full.
        /// Negative response times are clamped to zero to prevent distorted percentile calculations.
        /// </summary>
        /// <param name="responseTimeMs">Request round-trip time in milliseconds. Clamped to 0 if negative.</param>
        /// <param name="succeeded">Whether the request completed without an error.</param>
        public void Add(int responseTimeMs, bool succeeded)
        {
            if (_samples.Count >= _maxSamples)
                _samples.Dequeue();

            _samples.Enqueue(new RequestSample(
                Math.Max(0, responseTimeMs),
                succeeded,
                DateTime.UtcNow
            ));
        }

        /// <summary>
        /// Discards all observations and resets <see cref="WindowStartUtc"/> to the current UTC time.
        /// Future calls to <see cref="Summarize"/> will return <c>null</c> until new observations are added.
        /// </summary>
        public void Clear()
        {
            _samples.Clear();
            WindowStartUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Computes and returns an immutable <see cref="UpstreamMetricsSummary"/> from the current
        /// window state, or <c>null</c> when the window contains no observations. The returned
        /// summary is a snapshot; future <see cref="Add"/> calls do not affect it.
        /// </summary>
        public UpstreamMetricsSummary? Summarize()
        {
            if (_samples.Count == 0)
                return null;

            var now      = DateTime.UtcNow;
            var snapshot = _samples.ToArray();

            var latencies = snapshot
                .Select(s => (double)s.ResponseTimeMs)
                .OrderBy(v => v)
                .ToArray();

            var errorCount  = snapshot.Count(s => !s.Succeeded);
            var windowSecs  = (now - WindowStartUtc).TotalSeconds;
            var throughput  = windowSecs > 0 ? snapshot.Length / windowSecs : 0.0;

            return new UpstreamMetricsSummary(
                UpstreamId:     UpstreamId,
                SampleCount:    snapshot.Length,
                P50LatencyMs:   ComputePercentile(latencies, 0.50),
                P95LatencyMs:   ComputePercentile(latencies, 0.95),
                P99LatencyMs:   ComputePercentile(latencies, 0.99),
                MeanLatencyMs:  latencies.Average(),
                ErrorRate:      (double)errorCount / snapshot.Length,
                ThroughputRps:  throughput,
                WindowStartUtc: WindowStartUtc,
                WindowEndUtc:   now
            );
        }

        // ─── Private Helpers ──────────────────────────────────────────────────

        private static double ComputePercentile(double[] sortedValues, double percentile)
        {
            if (sortedValues.Length == 0) return 0.0;
            if (sortedValues.Length == 1) return sortedValues[0];

            var rank  = percentile * (sortedValues.Length - 1);
            var lower = (int)Math.Floor(rank);
            var upper = Math.Min(lower + 1, sortedValues.Length - 1);
            var frac  = rank - lower;

            return sortedValues[lower] * (1.0 - frac) + sortedValues[upper] * frac;
        }
    }
}
