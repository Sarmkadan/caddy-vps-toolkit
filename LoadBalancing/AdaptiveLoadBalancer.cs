// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.Configuration;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Services;

namespace CaddyVpsToolkit.LoadBalancing
{
    /// <summary>
    /// Thread-safe implementation of <see cref="IMetricsAggregator"/> backed by per-upstream
    /// <see cref="UpstreamMetricsWindow"/> instances. The window itself handles thread-safety internally,
    /// so no additional locks are required at this level.
    /// </summary>
    public sealed class SlidingWindowMetricsAggregator : IMetricsAggregator
    {
        private readonly int _windowSize;
        private readonly ConcurrentDictionary<string, UpstreamMetricsWindow> _windows = new();

        /// <summary>
        /// Initialises a new aggregator with the specified per-upstream window capacity.
        /// </summary>
        /// <param name="windowSize">
        /// Maximum number of request samples retained per upstream. Older samples are evicted
        /// when the limit is reached. Defaults to <c>200</c>.
        /// </param>
        public SlidingWindowMetricsAggregator(int windowSize = 200) => _windowSize = windowSize;

        /// <inheritdoc/>
        public void Record(string upstreamId, int responseTimeMs, bool succeeded)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(upstreamId);
            var window = _windows.GetOrAdd(upstreamId, id => new UpstreamMetricsWindow(id, _windowSize));
            window.Add(responseTimeMs, succeeded);
        }

        /// <inheritdoc/>
        public UpstreamMetricsSummary? GetSummary(string upstreamId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(upstreamId);
            if (!_windows.TryGetValue(upstreamId, out var window))
                return null;

            return window.Summarize();
        }

        /// <inheritdoc/>
        public void Reset(string upstreamId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(upstreamId);
            if (_windows.TryGetValue(upstreamId, out var window))
                window.Clear();
        }
    }

    /// <summary>
    /// Default implementation of <see cref="IAdaptiveLoadBalancer"/>.
    /// <para>
    /// Each evaluation pass scores every upstream in the requested pool along three independent
    /// dimensions — p99 latency, rolling error rate, and active-connection pressure — and combines
    /// them into a single normalised composite score weighted by <see cref="UpstreamManagementOptions"/>.
    /// The top-scoring eligible candidate is pre-selected and returned inside a
    /// <see cref="PoolRoutingEvaluation"/> alongside the full ranked list for observability.
    /// </para>
    /// <para>
    /// Adaptive weights are maintained per upstream as a multiplier in (0.0, ∞) updated after each
    /// request outcome via an exponential moving average. Successes nudge the multiplier toward
    /// <c>1.0</c>; failures nudge it toward <see cref="UpstreamManagementOptions.PenaltyMultiplier"/>.
    /// A separate time-decaying score penalty is applied immediately after each failure and
    /// dissolves linearly over <see cref="UpstreamManagementOptions.PenaltyDecaySeconds"/>.
    /// </para>
    /// </summary>
    public sealed class AdaptiveLoadBalancer : IAdaptiveLoadBalancer
    {
        private readonly UpstreamManagerService _upstreamManager;
        private readonly IMetricsAggregator _metrics;
        private readonly UpstreamManagementOptions _options;

        // Adaptive multiplier per upstream (1.0 = neutral). Adjusted by EMA on each RecordOutcome call.
        // Protected by _weightsLock for atomic read-modify-write operations
        private readonly ConcurrentDictionary<string, double> _adaptiveWeights = new();
        private readonly object _weightsLock = new object();

        // UTC timestamp of the most recent failure-penalty issuance per upstream.
        // Protected by _penaltyLock for atomic updates
        private readonly ConcurrentDictionary<string, DateTime> _penaltyIssuedAt = new();
        private readonly object _penaltyLock = new object();

        // ─── Construction ─────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the adaptive load balancer with its required collaborators.
        /// </summary>
        /// <param name="upstreamManager">Provides pool and upstream server state for candidate enumeration.</param>
        /// <param name="metrics">Aggregates per-upstream request samples for scoring calculations.</param>
        /// <param name="options">Tuning parameters for scoring weights, adaptation speed, and penalty behaviour.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
        public AdaptiveLoadBalancer(
            UpstreamManagerService upstreamManager,
            IMetricsAggregator metrics,
            UpstreamManagementOptions options)
        {
            _upstreamManager = upstreamManager ?? throw new ArgumentNullException(nameof(upstreamManager));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        // ─── IAdaptiveLoadBalancer ────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<PoolRoutingEvaluation> EvaluatePoolAsync(
            UpstreamSelectionContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            var pool = await _upstreamManager.GetPoolAsync(context.PoolId);
            if (pool is null)
                throw new ServiceConfigurationException(
                    $"Upstream pool '{context.PoolId}' is not registered");

            cancellationToken.ThrowIfCancellationRequested();

            if (pool.Servers.Count == 0)
            {
                return new PoolRoutingEvaluation(
                    PoolId: context.PoolId,
                    Scores: [],
                    SelectedUpstreamId: null,
                    EvaluatedAt: DateTime.UtcNow);
            }

            // Compute all scores atomically to ensure consistency between metrics and adaptive weights
            // We need to ensure that the same snapshot of metrics and weights is used for all servers
            var scores = new List<UpstreamRoutingScore>(pool.Servers.Count);
            foreach (var server in pool.Servers)
            {
                scores.Add(ComputeScore(server));
            }

            scores.Sort((a, b) =>
            {
                var cmp = b.NormalizedScore.CompareTo(a.NormalizedScore);
                return cmp != 0 ? cmp : b.EffectiveWeight.CompareTo(a.EffectiveWeight);
            });

            var winner = scores.FirstOrDefault(sc => sc.IsEligible);

            return new PoolRoutingEvaluation(
                PoolId: context.PoolId,
                Scores: scores,
                SelectedUpstreamId: winner?.UpstreamId,
                EvaluatedAt: DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public Task RecordOutcomeAsync(string poolId, string upstreamId, int responseTimeMs, bool succeeded)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(poolId);
            ArgumentException.ThrowIfNullOrWhiteSpace(upstreamId);

            _metrics.Record(upstreamId, responseTimeMs, succeeded);

            // Issue a fresh penalty timestamp on every failure so the decay clock resets.
            // Protected by lock to ensure atomic read-modify-write
            if (!succeeded)
            {
                lock (_penaltyLock)
                {
                    _penaltyIssuedAt[upstreamId] = DateTime.UtcNow;
                }
            }

            // Nudge the adaptive multiplier via EMA: successes pull toward 1.0, failures toward PenaltyMultiplier.
            // Protected by lock to ensure atomic read-modify-write
            lock (_weightsLock)
            {
                _adaptiveWeights.AddOrUpdate(
                    upstreamId,
                    addValue: succeeded ? 1.0 : _options.PenaltyMultiplier,
                    updateValueFactory: (_, current) =>
                    {
                        var target = succeeded ? 1.0 : _options.PenaltyMultiplier;
                        return current * (1.0 - _options.WeightAdaptationAlpha)
                            + target * _options.WeightAdaptationAlpha;
                    });
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<int> GetEffectiveWeightAsync(string upstreamId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(upstreamId);

            // Read is thread-safe via ConcurrentDictionary, but we ensure consistency with writes
            var multiplier = _adaptiveWeights.GetValueOrDefault(upstreamId, 1.0);
            var effective = (int)Math.Max(1, Math.Round(multiplier * 100));
            return Task.FromResult(effective);
        }

        /// <inheritdoc/>
        public async Task RecalibratePoolAsync(string poolId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(poolId);

            var pool = await _upstreamManager.GetPoolAsync(poolId);
            if (pool is null) return;

            cancellationToken.ThrowIfCancellationRequested();

            // Remove all adaptive state for all servers in the pool
            // Protected by locks to prevent concurrent reads during removal
            foreach (var server in pool.Servers)
            {
                _metrics.Reset(server.Id);

                lock (_weightsLock)
                    _adaptiveWeights.TryRemove(server.Id, out _);

                lock (_penaltyLock)
                    _penaltyIssuedAt.TryRemove(server.Id, out _);
            }
        }

        // ─── Private — Composite Scoring ────────────────────────────────────────

        private UpstreamRoutingScore ComputeScore(UpstreamServer server)
        {
            ArgumentNullException.ThrowIfNull(server);

            var summary = _metrics.GetSummary(server.Id);

            var latencyScore = ScoreLatency(summary);
            var errorRateScore = ScoreErrorRate(summary);
            var connectionScore = ScoreConnections(server.ActiveConnections);
            var penaltyFactor = ResolvePenaltyFactor(server.Id);

            var raw = _options.LatencyWeight * latencyScore
                + _options.ErrorRateWeight * errorRateScore
                + _options.ConnectionWeight * connectionScore;

            // Apply half-open penalty if the server is in HalfOpen recovery state
        var halfOpenPenalty = server.Status == UpstreamServerStatus.HalfOpen ? _options.HalfOpenPenaltyMultiplier : 1.0;
        var normalised = Math.Clamp(raw * penaltyFactor * halfOpenPenalty, 0.0, 1.0);

            // Read is thread-safe via ConcurrentDictionary
            var adaptiveMultiplier = _adaptiveWeights.GetValueOrDefault(server.Id, 1.0);
            var effectiveWeight = (int)Math.Max(1, Math.Round(server.Weight * adaptiveMultiplier));

            return new UpstreamRoutingScore(
                UpstreamId: server.Id,
                NormalizedScore: normalised,
                EffectiveWeight: effectiveWeight,
                LatencyScore: latencyScore,
                ErrorRateScore: errorRateScore,
                ConnectionScore: connectionScore,
                IsEligible: server.IsAvailable() && normalised > 0.0
            );
        }

        private double ScoreLatency(UpstreamMetricsSummary? summary)
        {
            // No data or too few samples → optimistically score 1.0 so new upstreams receive fair traffic.
            if (summary is null || !summary.IsStatisticallySignificant)
                return 1.0;

            // Normalise p99 relative to the target. p99 ≤ target → score 1.0;
            // each additional target-multiple of latency degrades the score by 1/9.
            var ratio = summary.P99LatencyMs / _options.TargetLatencyMs;
            return Math.Clamp(1.0 - (ratio - 1.0) / 9.0, 0.0, 1.0);
        }

        private static double ScoreErrorRate(UpstreamMetricsSummary? summary) =>
            summary is null ? 1.0 : Math.Clamp(1.0 - summary.ErrorRate, 0.0, 1.0);

        private double ScoreConnections(int activeConnections)
        {
            if (activeConnections <= 0) return 1.0;
            return Math.Clamp(1.0 - (double)activeConnections / _options.MaxExpectedConnections, 0.0, 1.0);
        }

        // ─── Private — Penalty Decay ────────────────────────────────────────────

        private double ResolvePenaltyFactor(string upstreamId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(upstreamId);

            DateTime issuedAt;
            // Read is thread-safe via ConcurrentDictionary, but we need to ensure we get a consistent snapshot
            lock (_penaltyLock)
            {
                if (!_penaltyIssuedAt.TryGetValue(upstreamId, out issuedAt))
                    return 1.0;
            }

            var elapsed = (DateTime.UtcNow - issuedAt).TotalSeconds;
            if (elapsed >= _options.PenaltyDecaySeconds)
            {
                // Remove expired penalty to clean up state
                lock (_penaltyLock)
                    _penaltyIssuedAt.TryRemove(upstreamId, out _);

                return 1.0;
            }

            // Linear interpolation: PenaltyMultiplier → 1.0 as elapsed → PenaltyDecaySeconds.
            var progress = elapsed / _options.PenaltyDecaySeconds;
            return _options.PenaltyMultiplier + (1.0 - _options.PenaltyMultiplier) * progress;
        }
    }
}
