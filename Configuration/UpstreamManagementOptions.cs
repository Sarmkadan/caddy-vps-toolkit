// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

/// <summary>
/// Defines the policy to apply when all upstreams in a pool are unhealthy.
/// </summary>
public enum AllUpstreamsUnhealthyPolicy
{
/// <summary>
/// Throws a <see cref="NoHealthyUpstreamException"/> immediately when no healthy upstreams are available.
/// This is the safest option for critical systems where routing to an unhealthy upstream would be dangerous.
/// </summary>
FailFast = 0,

/// <summary>
/// Selects the upstream with the best historical performance among unhealthy servers.
/// This allows graceful degradation by continuing to route traffic to the least-failed server, enabling
/// gradual recovery and observability into which upstream recovers first.
/// </summary>
FailOpen = 1
}

namespace CaddyVpsToolkit.Configuration
{
    /// <summary>
    /// Configuration options that govern the v2 adaptive upstream management and health-aware
    /// load-balancing subsystem. Bind these values from <c>appsettings.json</c> under the
    /// <c>"UpstreamManagement"</c> key, or supply them programmatically via
    /// <see cref="CaddyVpsToolkit.Extensions.UpstreamServiceExtensions.AddUpstreamManagement"/>.
    /// </summary>
    /// <remarks>
    /// The three score-weight properties (<see cref="LatencyWeight"/>, <see cref="ErrorRateWeight"/>,
    /// <see cref="ConnectionWeight"/>) do not need to sum to <c>1.0</c>. They express the relative
    /// importance of each dimension; the balancer normalises the weighted sum internally before
    /// applying the penalty factor and clamping the result to [0.0, 1.0].
    /// </remarks>
    public sealed class UpstreamManagementOptions
    {
        // ─── Composite Score Weights ──────────────────────────────────────────

        /// <summary>
        /// Gets or sets the relative weight applied to the p99 latency dimension when computing the
        /// composite upstream routing score. Increase this value to prefer low-latency backends more
        /// aggressively over other scoring dimensions. Defaults to <c>0.4</c>.
        /// </summary>
        public double LatencyWeight { get; set; } = 0.4;

        /// <summary>
        /// Gets or sets the relative weight applied to the rolling error-rate dimension in the
        /// composite score. Increase this value to penalise partially degraded upstreams more heavily
        /// and drain traffic away from error-prone backends faster. Defaults to <c>0.4</c>.
        /// </summary>
        public double ErrorRateWeight { get; set; } = 0.4;

        /// <summary>
        /// Gets or sets the relative weight applied to the active-connection-pressure dimension.
        /// Increase this value in environments where connection fan-out is the primary bottleneck
        /// (e.g. long-lived WebSocket or gRPC streams). Defaults to <c>0.2</c>.
        /// </summary>
        public double ConnectionWeight { get; set; } = 0.2;

        // ─── Latency Target ───────────────────────────────────────────────────

        /// <summary>
        /// Gets or sets the target p99 response latency in milliseconds. Upstreams achieving p99
        /// at or below this value receive a near-perfect latency score of <c>1.0</c>. Each additional
        /// multiple of this target degrades the score by <c>1/9</c>, reaching <c>0.0</c> at ten times
        /// the target. Defaults to <c>200</c> ms.
        /// </summary>
        public double TargetLatencyMs { get; set; } = 200.0;

        /// <summary>
        /// Gets or sets the expected maximum number of concurrent active connections per upstream
        /// during normal operating conditions. Used as the normalisation denominator for the
        /// connection-pressure score. Defaults to <c>100</c>.
        /// </summary>
        public int MaxExpectedConnections { get; set; } = 100;

        // ─── Adaptive Weight Adjustment ───────────────────────────────────────

        /// <summary>
        /// Gets or sets the exponential moving average alpha applied when updating each upstream's
        /// adaptive weight multiplier after a request outcome is recorded. Lower values produce slower
        /// but more stable adaptation to changing conditions; higher values react faster to transient
        /// degradation at the cost of noisier weight distributions.
        /// Valid range: (0.0, 1.0). Defaults to <c>0.15</c>.
        /// </summary>
        public double WeightAdaptationAlpha { get; set; } = 0.15;

        // ─── Failure Penalty ──────────────────────────────────────────────────

        /// <summary>
        /// Gets or sets the score multiplier applied immediately to an upstream after a failed request.
        /// A value of <c>0.3</c> reduces that upstream's composite score to 30% of its normal value,
        /// making it significantly less likely to be selected during the penalty window. The penalty
        /// decays linearly to <c>1.0</c> over <see cref="PenaltyDecaySeconds"/>.
        /// Valid range: (0.0, 1.0). Defaults to <c>0.3</c>.
        /// </summary>
        public double PenaltyMultiplier { get; set; } = 0.3;

        /// <summary>
        /// Gets or sets the duration in seconds over which a failure penalty linearly decays back to
        /// a neutral factor of <c>1.0</c>. Once this interval has elapsed without a new failure the
        /// upstream's full score is restored and penalty tracking is removed. Defaults to <c>60</c> seconds.
        /// </summary>
        public double PenaltyDecaySeconds { get; set; } = 60.0;

        // ─── Sliding Metrics Window ───────────────────────────────────────────

        /// <summary>
        /// Gets or sets the maximum number of request samples retained in each upstream's sliding
        /// metrics window. Larger windows produce more stable p95/p99 estimates at the cost of
        /// higher memory usage and slower adaptation to a changing load profile. Defaults to <c>200</c>.
        /// </summary>
        public int MetricsWindowSize { get; set; } = 200;

        // ─── Periodic Recalibration ───────────────────────────────────────────

        /// <summary>
        /// Gets or sets whether automatic periodic weight recalibration is enabled for all managed pools.
        /// When <c>true</c>, accumulated adaptive state and metrics windows are flushed on the interval
        /// defined by <see cref="RecalibrationIntervalSeconds"/>, preventing stale weights from
        /// persisting after a prolonged period of low traffic. Defaults to <c>true</c>.
        /// </summary>
        public bool AutoRecalibrationEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval in seconds between automatic full-pool recalibration passes.
        /// Shorter intervals adapt faster to topology changes but increase transient routing variance
        /// as weight histories are discarded. Longer intervals preserve stable distributions under
        /// steady-state load. Defaults to <c>300</c> seconds (5 minutes).
        /// </summary>
        public int RecalibrationIntervalSeconds { get; set; } = 300;

        // ─── Half-Open Recovery ────────────────────────────────────────────────────

        /// <summary>
        /// Gets or sets the score multiplier applied to upstreams in <see cref="UpstreamServerStatus.HalfOpen"/> state.
        /// This limits traffic to recovering upstreams to prevent overwhelming potentially still-fragile servers.
        /// A value of <c>0.2</c> reduces the composite score to 20% of its normal value, making the upstream
        /// much less likely to be selected while still allowing limited traffic for testing.
        /// Valid range: (0.0, 1.0). Defaults to <c>0.2</c>.
        /// </summary>
        public double HalfOpenPenaltyMultiplier { get; set; } = 0.2;

// ─── All Upstreams Unhealthy Policy ────────────────────────────────────────

/// <summary>
/// Defines the policy to apply when all upstreams in a pool are marked unhealthy.
/// <para>
/// - <see cref="AllUpstreamsUnhealthyPolicy.FailFast"/>: Throws a <see cref="NoHealthyUpstreamException"/> immediately when no healthy upstreams are available.
/// This is the safest option for critical systems where routing to an unhealthy upstream would be dangerous.
/// </para>
/// <para>
/// - <see cref="AllUpstreamsUnhealthyPolicy.FailOpen"/>: Selects the upstream with the best historical performance among unhealthy servers.
/// This allows graceful degradation by continuing to route traffic to the least-failed server, enabling
/// gradual recovery and observability into which upstream recovers first. This is the default.
/// </para>
/// </summary>
public AllUpstreamsUnhealthyPolicy AllUpstreamsUnhealthyPolicy { get; set; } = AllUpstreamsUnhealthyPolicy.FailOpen;

// ─── Maintenance Windows ────────────────────────────────────────────────

/// <summary>
/// Gets or sets the maintenance windows during which failing health checks should log but not trigger alerts or state transitions.
/// Multiple maintenance windows can be configured for different time periods and days.
/// </summary>
public List<Domain.Models.MaintenanceWindow> MaintenanceWindows { get; set; } = new();

    /// <summary>
    /// Determines if any maintenance window is currently active.
    /// </summary>
    /// <returns>True if any maintenance window is active, otherwise false.</returns>
    public bool IsMaintenanceWindowActive()
    {
        if (MaintenanceWindows == null || MaintenanceWindows.Count == 0)
        {
            return false;
        }

        return MaintenanceWindows.Any(window => window.IsInWindow());
    }
}
}
