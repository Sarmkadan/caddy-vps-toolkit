// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.LoadBalancing
{
    /// <summary>
    /// Represents the composite routing score assigned to a single upstream candidate during an
    /// adaptive evaluation pass. Combines latency, error-rate, and connection-pressure sub-scores
    /// into a single normalised value used for final ranking.
    /// </summary>
    /// <param name="UpstreamId">The upstream server's unique identifier.</param>
    /// <param name="NormalizedScore">
    /// Dimensionless composite score in [0.0, 1.0] where <c>1.0</c> represents the optimal candidate.
    /// Higher scores indicate a more desirable routing target.
    /// </param>
    /// <param name="EffectiveWeight">
    /// Dynamically computed weight derived from the server's configured base weight combined with
    /// all real-time adaptive adjustments. Used as a tie-breaker when two candidates share an equal score.
    /// </param>
    /// <param name="LatencyScore">Latency dimension contribution in [0.0, 1.0]. Higher means lower observed latency.</param>
    /// <param name="ErrorRateScore">Error-rate dimension contribution in [0.0, 1.0]. Higher means fewer request errors.</param>
    /// <param name="ConnectionScore">Connection-pressure contribution in [0.0, 1.0]. Higher means fewer in-flight connections.</param>
    /// <param name="IsEligible">
    /// Whether this upstream passed all health and circuit-breaker eligibility gates.
    /// Ineligible candidates appear in the scored list for observability purposes but are never selected.
    /// </param>
    public record UpstreamRoutingScore(
        string UpstreamId,
        double NormalizedScore,
        int    EffectiveWeight,
        double LatencyScore,
        double ErrorRateScore,
        double ConnectionScore,
        bool   IsEligible
    );

    /// <summary>
    /// Carries the full ranked set of routing scores produced during a single pool evaluation pass,
    /// enabling callers to inspect the scoring rationale beyond the single upstream that was chosen.
    /// </summary>
    /// <param name="PoolId">The pool that was evaluated.</param>
    /// <param name="Scores">
    /// Per-upstream scores ordered descending by <see cref="UpstreamRoutingScore.NormalizedScore"/>.
    /// All candidates, including ineligible ones, are included so operators can diagnose degraded servers.
    /// </param>
    /// <param name="SelectedUpstreamId">
    /// Identifier of the upstream chosen for this routing pass, or <c>null</c> when all candidates
    /// were ineligible and no upstream could be selected.
    /// </param>
    /// <param name="EvaluatedAt">UTC timestamp of the evaluation pass.</param>
    public record PoolRoutingEvaluation(
        string                          PoolId,
        IReadOnlyList<UpstreamRoutingScore> Scores,
        string?                         SelectedUpstreamId,
        DateTime                        EvaluatedAt
    )
    {
        /// <summary>
        /// Gets whether at least one eligible upstream was identified during the evaluation,
        /// meaning <see cref="SelectedUpstreamId"/> is non-null and routing can proceed.
        /// </summary>
        public bool HasEligibleUpstream => SelectedUpstreamId is not null;
    }

    /// <summary>
    /// Computes dynamic routing scores for upstream server candidates, enabling health-aware
    /// selection that adapts continuously to observed latency, error rates, and connection pressure.
    /// The scorer maintains an exponential moving average of each upstream's performance and applies
    /// time-decaying penalties to servers that recently experienced failures.
    /// </summary>
    public interface IAdaptiveLoadBalancer
    {
        /// <summary>
        /// Evaluates all candidates in the specified pool and returns a scored ranking with the
        /// optimal upstream pre-selected. Ineligible candidates are included in the result with
        /// <see cref="UpstreamRoutingScore.IsEligible"/> set to <c>false</c>.
        /// </summary>
        /// <param name="context">Per-request context used for pool lookup and strategy hints.</param>
        /// <param name="cancellationToken">Token to abort the operation.</param>
        /// <returns>
        /// A <see cref="PoolRoutingEvaluation"/> with the full scored ranking and the identity of
        /// the upstream selected for this request.
        /// </returns>
        Task<PoolRoutingEvaluation> EvaluatePoolAsync(
            UpstreamSelectionContext context,
            CancellationToken        cancellationToken = default);

        /// <summary>
        /// Records a completed request outcome for the specified upstream, feeding the adaptive
        /// scoring system's sliding-window metrics. Must be called for every routed request so
        /// the balancer can maintain accurate, up-to-date performance state.
        /// </summary>
        /// <param name="poolId">Pool that owns the upstream.</param>
        /// <param name="upstreamId">Upstream that handled the request.</param>
        /// <param name="responseTimeMs">Observed end-to-end response time in milliseconds.</param>
        /// <param name="succeeded">Whether the request completed without an error.</param>
        Task RecordOutcomeAsync(string poolId, string upstreamId, int responseTimeMs, bool succeeded);

        /// <summary>
        /// Returns the current effective weight for the specified upstream, reflecting the base
        /// configuration weight multiplied by all dynamic adjustments applied since registration.
        /// </summary>
        /// <param name="upstreamId">The upstream server's identifier.</param>
        /// <returns>
        /// Effective weight as a positive integer, always at least <c>1</c>. Returns a weight
        /// derived from the base configuration when no adaptive data has been accumulated yet.
        /// </returns>
        Task<int> GetEffectiveWeightAsync(string upstreamId);

        /// <summary>
        /// Forces an immediate recalibration of adaptive weights for all upstreams in the specified
        /// pool, discarding accumulated metrics and weight adjustments. After recalibration all servers
        /// are treated as equal until fresh performance observations are collected. Useful after
        /// topology changes such as adding or removing backend servers.
        /// </summary>
        /// <param name="poolId">The pool to recalibrate.</param>
        /// <param name="cancellationToken">Token to abort the operation.</param>
        Task RecalibratePoolAsync(string poolId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Aggregates per-upstream request metrics within a configurable sliding sample window,
    /// exposing computed statistics such as p50/p95/p99 latencies and rolling error rates that
    /// feed the adaptive scoring model of <see cref="IAdaptiveLoadBalancer"/>.
    /// </summary>
    public interface IMetricsAggregator
    {
        /// <summary>
        /// Records a single request observation for the specified upstream into its sliding window.
        /// </summary>
        /// <param name="upstreamId">Target upstream identifier.</param>
        /// <param name="responseTimeMs">Observed round-trip time in milliseconds.</param>
        /// <param name="succeeded">Whether the request completed successfully.</param>
        void Record(string upstreamId, int responseTimeMs, bool succeeded);

        /// <summary>
        /// Returns an immutable metrics summary computed from the current sliding window state for
        /// the specified upstream, or <c>null</c> when no samples have been recorded yet.
        /// </summary>
        /// <param name="upstreamId">Target upstream identifier.</param>
        UpstreamMetricsSummary? GetSummary(string upstreamId);

        /// <summary>
        /// Discards all accumulated samples for the specified upstream, resetting its metrics window
        /// to an empty state. Subsequent calls to <see cref="GetSummary"/> will return <c>null</c>
        /// until new samples are recorded.
        /// </summary>
        /// <param name="upstreamId">Target upstream identifier.</param>
        void Reset(string upstreamId);
    }
}
