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
    // ─── Context / Report Records ─────────────────────────────────────────────

    /// <summary>
    /// Carries per-request contextual data used by load-balancing strategies when selecting an upstream.
    /// </summary>
    /// <param name="PoolId">The identifier of the pool from which an upstream should be selected.</param>
    /// <param name="ClientIp">
    /// Optional client IP address. Required by the <see cref="LoadBalancingStrategy.IpHash"/> strategy
    /// for deterministic upstream pinning.
    /// </param>
    /// <param name="SessionToken">
    /// Optional opaque session token. Used by sticky-session logic to re-pin a known session
    /// to its previously assigned upstream.
    /// </param>
    public record UpstreamSelectionContext(
        string PoolId,
        string? ClientIp = null,
        string? SessionToken = null
    );

    /// <summary>
    /// An immutable snapshot of health and connection metrics for a single upstream server,
    /// captured at a point in time for reporting and decision-making purposes.
    /// </summary>
    /// <param name="UpstreamId">The upstream server's unique identifier.</param>
    /// <param name="Address">The upstream's address in <c>host:port</c> form.</param>
    /// <param name="IsHealthy">Whether the upstream was considered healthy at snapshot time.</param>
    /// <param name="Status">The operational status of the upstream at snapshot time.</param>
    /// <param name="ConsecutiveFailures">Number of consecutive failed health probes leading up to the snapshot.</param>
    /// <param name="AverageResponseTimeMs">Rolling average probe round-trip time in milliseconds.</param>
    /// <param name="ActiveConnections">Number of in-flight connections at snapshot time.</param>
    /// <param name="LastCheckedAt">UTC time of the most recent health probe, or <c>null</c> if never probed.</param>
    public record UpstreamHealthSnapshot(
        string UpstreamId,
        string Address,
        bool IsHealthy,
        UpstreamServerStatus Status,
        int ConsecutiveFailures,
        int AverageResponseTimeMs,
        int ActiveConnections,
        DateTime? LastCheckedAt
    );

    /// <summary>
    /// Aggregated health report for an entire <see cref="UpstreamPool"/>, suitable for
    /// dashboard display, alerting, and automated remediation decisions.
    /// </summary>
    /// <param name="PoolId">The pool's unique identifier.</param>
    /// <param name="PoolName">The human-readable pool name.</param>
    /// <param name="Strategy">The load-balancing strategy currently in use by this pool.</param>
    /// <param name="TotalUpstreams">Total number of configured upstream servers in the pool.</param>
    /// <param name="HealthyUpstreams">Number of upstreams currently reporting as healthy.</param>
    /// <param name="AvailableUpstreams">Number of upstreams eligible to receive traffic right now.</param>
    /// <param name="TotalActiveConnections">Sum of active in-flight connections across the whole pool.</param>
    /// <param name="Upstreams">Ordered list of per-upstream health snapshots.</param>
    /// <param name="GeneratedAt">UTC timestamp when this report was produced.</param>
    public record UpstreamPoolHealthReport(
        string PoolId,
        string PoolName,
        LoadBalancingStrategy Strategy,
        int TotalUpstreams,
        int HealthyUpstreams,
        int AvailableUpstreams,
        int TotalActiveConnections,
        IReadOnlyList<UpstreamHealthSnapshot> Upstreams,
        DateTime GeneratedAt
    )
    {
        /// <summary>Gets the fraction of healthy upstreams relative to the total, in the range [0.0, 1.0].</summary>
        public double HealthRatio => TotalUpstreams > 0 ? (double)HealthyUpstreams / TotalUpstreams : 0.0;

        /// <summary>
        /// <c>true</c> when at least one upstream is available and the pool can serve traffic.
        /// </summary>
        public bool IsOperational => AvailableUpstreams > 0;

        /// <summary>
        /// <c>true</c> when every upstream in the pool is healthy — the pool is operating at full capacity.
        /// </summary>
        public bool IsFullyHealthy => HealthyUpstreams == TotalUpstreams && TotalUpstreams > 0;
    }

    // ─── Interfaces ───────────────────────────────────────────────────────────

    /// <summary>
    /// Selects the next upstream server from a candidate list according to a load-balancing strategy.
    /// Implementations are expected to be stateless with respect to the candidate list itself;
    /// any state required for ordering (e.g. round-robin index) must be maintained externally
    /// and threaded in via the <see cref="UpstreamSelectionContext"/>.
    /// </summary>
    public interface IUpstreamSelector
    {
        /// <summary>
        /// Selects the best available upstream from <paramref name="servers"/> given the provided
        /// <paramref name="context"/>.
        /// </summary>
        /// <param name="servers">
        /// The candidate set of upstream servers. Callers should pre-filter this list to servers
        /// that are available (<see cref="UpstreamServer.IsAvailable()"/> returns <c>true</c>).
        /// </param>
        /// <param name="context">
        /// Per-request context that informs strategy-specific decisions (e.g. client IP for IP hash).
        /// </param>
        /// <returns>
        /// The selected <see cref="UpstreamServer"/>, or <c>null</c> when the candidate list is empty.
        /// </returns>
        UpstreamServer? Select(IReadOnlyList<UpstreamServer> servers, UpstreamSelectionContext context);
    }

    /// <summary>
    /// Tracks and updates the health state of individual upstream servers within a pool based on
    /// the outcomes of active probes and passive request monitoring.
    /// </summary>
    public interface IUpstreamHealthTracker
    {
        /// <summary>
        /// Records the result of a single health probe for <paramref name="upstreamId"/> and applies
        /// the unhealthy/healthy threshold logic to determine whether the upstream's status should change.
        /// </summary>
        /// <param name="upstreamId">The target upstream server's unique identifier.</param>
        /// <param name="poolId">The pool that owns the upstream (used to locate the pool's thresholds).</param>
        /// <param name="probeSucceeded">Whether the probe completed successfully.</param>
        /// <param name="responseTimeMs">Round-trip probe time in milliseconds; <c>0</c> on failure.</param>
        Task RecordProbeResultAsync(string upstreamId, string poolId, bool probeSucceeded, int responseTimeMs = 0);

        /// <summary>
        /// Returns the latest health snapshot for the specified upstream, or <c>null</c> if the
        /// upstream is not tracked.
        /// </summary>
        /// <param name="upstreamId">The upstream server's unique identifier.</param>
        Task<UpstreamHealthSnapshot?> GetSnapshotAsync(string upstreamId);

        /// <summary>
        /// Initiates a graceful drain on the specified upstream, preventing new requests from being
        /// routed to it while allowing in-flight requests to complete up to <paramref name="drainTimeout"/>.
        /// </summary>
        /// <param name="upstreamId">The upstream to drain.</param>
        /// <param name="drainTimeout">Maximum time to wait for active connections to finish.</param>
        /// <param name="cancellationToken">Token to abort the drain wait early.</param>
        Task DrainAsync(string upstreamId, TimeSpan drainTimeout, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Provides persistence operations for <see cref="UpstreamPool"/> instances, allowing the toolkit
    /// to reload pool configuration across process restarts.
    /// </summary>
    public interface IUpstreamPoolRepository
    {
        /// <summary>Retrieves a single pool by its unique identifier, or <c>null</c> if not found.</summary>
        Task<UpstreamPool?> GetByIdAsync(string poolId);

        /// <summary>Returns all pools associated with the specified service identifier.</summary>
        Task<List<UpstreamPool>> GetByServiceIdAsync(string serviceId);

        /// <summary>Returns all registered upstream pools in the data store.</summary>
        Task<List<UpstreamPool>> GetAllAsync();

        /// <summary>
        /// Persists a new upstream pool and returns its assigned identifier.
        /// </summary>
        Task<string> AddAsync(UpstreamPool pool);

        /// <summary>
        /// Updates an existing upstream pool. Returns <c>true</c> when the pool was found and updated,
        /// <c>false</c> when no matching pool existed.
        /// </summary>
        Task<bool> UpdateAsync(UpstreamPool pool);

        /// <summary>
        /// Removes a pool from the data store by identifier. Returns <c>true</c> on success.
        /// </summary>
        Task<bool> DeleteAsync(string poolId);

        /// <summary>Returns <c>true</c> when a pool with the given identifier exists in the data store.</summary>
        Task<bool> ExistsAsync(string poolId);
    }
}
