// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Results;
using CaddyVpsToolkit.Services;

namespace CaddyVpsToolkit.LoadBalancing
{
    /// <summary>
    /// Unified health-aware routing entry point that layers adaptive scoring on top of the base
    /// <see cref="UpstreamManagerService"/> selection logic.
    /// <para>
    /// When the adaptive model produces a high-confidence scored winner, that candidate is returned
    /// directly. When the model has insufficient data (e.g. all servers are new with no observations),
    /// the call falls back transparently to the strategy configured on the <see cref="UpstreamPool"/>.
    /// </para>
    /// <para>
    /// All circuit-breaker and health-ratio checks are applied before scoring, so callers receive
    /// a structured <see cref="Result{T}"/> that clearly distinguishes between "circuit open",
    /// "pool disabled", and "scoring underway with fallback" outcomes.
    /// </para>
    /// </summary>
    public sealed class HealthAwareRoutingPolicy
    {
        private readonly UpstreamManagerService _upstreamManager;
        private readonly IAdaptiveLoadBalancer  _adaptiveBalancer;
        private readonly LoadBalancingOptions   _options;

        // ─── Construction ─────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the routing policy with the required collaborators.
        /// </summary>
        /// <param name="upstreamManager">Provides pool state and executes fallback strategy selection.</param>
        /// <param name="adaptiveBalancer">Scoring engine that ranks upstream candidates in real-time.</param>
        /// <param name="options">Runtime load-balancing options including circuit-breaker configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
        public HealthAwareRoutingPolicy(
            UpstreamManagerService  upstreamManager,
            IAdaptiveLoadBalancer   adaptiveBalancer,
            LoadBalancingOptions    options)
        {
            _upstreamManager  = upstreamManager  ?? throw new ArgumentNullException(nameof(upstreamManager));
            _adaptiveBalancer = adaptiveBalancer  ?? throw new ArgumentNullException(nameof(adaptiveBalancer));
            _options          = options           ?? throw new ArgumentNullException(nameof(options));
        }

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Selects the best available upstream for the given request using health-aware adaptive scoring.
        /// Falls back to the pool's configured load-balancing strategy when the scoring model lacks
        /// sufficient data to make a confident decision.
        /// </summary>
        /// <param name="context">Per-request context identifying the target pool, client IP, and session token.</param>
        /// <param name="cancellationToken">Token to abort the selection operation.</param>
        /// <returns>
        /// <see cref="Result{T}.Success(T)"/> carrying the chosen <see cref="UpstreamServer"/> on success.
        /// Returns a descriptive <see cref="Result{T}"/> failure when the circuit is open, the pool is
        /// disabled, or all selection strategies are exhausted.
        /// </returns>
        public async Task<Result<UpstreamServer>> RouteAsync(
            UpstreamSelectionContext context,
            CancellationToken        cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            var pool = await _upstreamManager.GetPoolAsync(context.PoolId);
            if (pool is null)
                return Result<UpstreamServer>.Failure(
                    $"Pool '{context.PoolId}' is not registered.",
                    "POOL_NOT_FOUND");

            if (!pool.IsEnabled)
                return Result<UpstreamServer>.Failure(
                    $"Pool '{context.PoolId}' is currently disabled.",
                    "POOL_DISABLED");

            var available = pool.GetAvailableServers();
            if (available.Count == 0)
                return _options.CircuitBreakerEnabled
                    ? Result<UpstreamServer>.Failure(
                        "Circuit open — no healthy upstreams are available.",
                        "CIRCUIT_OPEN")
                    : Result<UpstreamServer>.Failure(
                        "No available upstreams in pool.",
                        "POOL_EXHAUSTED");

            // Enforce pool-level health-ratio threshold when a non-zero threshold is configured.
            if (_options.CircuitBreakerEnabled && _options.CircuitBreakerHealthThreshold > 0.0)
            {
                var healthRatio = (double)available.Count / pool.Servers.Count;
                if (healthRatio < _options.CircuitBreakerHealthThreshold)
                    return Result<UpstreamServer>.Failure(
                        $"Circuit open — health ratio {healthRatio:P0} is below the configured " +
                        $"threshold of {_options.CircuitBreakerHealthThreshold:P0}.",
                        "CIRCUIT_OPEN_THRESHOLD");
            }

            // Attempt adaptive scoring first.
            var evaluation = await _adaptiveBalancer.EvaluatePoolAsync(context, cancellationToken);
            if (evaluation.HasEligibleUpstream)
            {
                var scored = available.FirstOrDefault(s => s.Id == evaluation.SelectedUpstreamId);
                if (scored is not null)
                    return Result<UpstreamServer>.Success(scored);
            }

            // Fallback: delegate to the pool's configured strategy via UpstreamManagerService.
            var fallback = await _upstreamManager.SelectUpstreamAsync(context);
            return fallback is not null
                ? Result<UpstreamServer>.Success(fallback)
                : Result<UpstreamServer>.Failure(
                    "All selection strategies exhausted — no upstream could be selected.",
                    "NO_UPSTREAM_SELECTED");
        }

        /// <summary>
        /// Reports the outcome of a request routed through this policy back into both the adaptive
        /// scoring engine and the underlying upstream health tracker, keeping both layers consistent.
        /// </summary>
        /// <param name="poolId">Pool that owns the upstream.</param>
        /// <param name="upstreamId">Upstream that handled the request.</param>
        /// <param name="responseTimeMs">Total observed round-trip time in milliseconds.</param>
        /// <param name="succeeded">Whether the request completed without an error.</param>
        public async Task NotifyOutcomeAsync(
            string poolId,
            string upstreamId,
            int    responseTimeMs,
            bool   succeeded)
        {
            await _adaptiveBalancer.RecordOutcomeAsync(poolId, upstreamId, responseTimeMs, succeeded);
            await _upstreamManager.RecordUpstreamResultAsync(poolId, upstreamId, succeeded, responseTimeMs);
        }

        /// <summary>
        /// Returns the full adaptive scoring result for the specified pool without performing a
        /// routing selection. Useful for health dashboards, CLI status output, and diagnostics.
        /// </summary>
        /// <param name="poolId">The pool to evaluate.</param>
        /// <param name="cancellationToken">Token to abort the operation.</param>
        /// <returns>
        /// All upstream candidates ordered descending by composite score, including ineligible servers
        /// so operators can diagnose degraded-but-not-failed backends.
        /// </returns>
        public async Task<IReadOnlyList<UpstreamRoutingScore>> GetScoredCandidatesAsync(
            string            poolId,
            CancellationToken cancellationToken = default)
        {
            var evaluation = await _adaptiveBalancer.EvaluatePoolAsync(
                new UpstreamSelectionContext(poolId),
                cancellationToken);

            return evaluation.Scores;
        }

        /// <summary>
        /// Forces a full weight recalibration for the specified pool, discarding accumulated adaptive
        /// state and treating all upstreams as equal until fresh observations are collected.
        /// </summary>
        /// <param name="poolId">The pool to recalibrate.</param>
        /// <param name="cancellationToken">Token to abort the operation.</param>
        public Task RecalibrateAsync(string poolId, CancellationToken cancellationToken = default) =>
            _adaptiveBalancer.RecalibratePoolAsync(poolId, cancellationToken);

        /// <summary>
        /// Returns the current effective weight for the specified upstream, incorporating all
        /// dynamic adjustments applied by the adaptive scoring engine.
        /// </summary>
        /// <param name="upstreamId">The upstream server's identifier.</param>
        /// <returns>
        /// Effective weight as a positive integer. Returns a weight derived from the server's base
        /// configuration when no adaptive adjustments have been applied yet.
        /// </returns>
        public Task<int> GetEffectiveWeightAsync(string upstreamId) =>
            _adaptiveBalancer.GetEffectiveWeightAsync(upstreamId);
    }
}
