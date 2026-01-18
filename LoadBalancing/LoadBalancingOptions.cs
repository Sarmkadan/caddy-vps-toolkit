// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.LoadBalancing
{
    /// <summary>
    /// Runtime configuration options for the upstream management and health-aware load-balancing
    /// subsystem introduced in v2. Register these settings via
    /// <see cref="CaddyVpsToolkit.Extensions.UpstreamServiceExtensions.AddUpstreamManagement"/>.
    /// </summary>
    /// <remarks>
    /// Individual <see cref="UpstreamPool"/> settings always take precedence over these defaults.
    /// These options govern behaviour when a pool does not specify its own override, and control
    /// features (e.g. circuit breaker) that span all pools.
    /// </remarks>
    public class LoadBalancingOptions
    {
        // ─── Strategy ─────────────────────────────────────────────────────────

        /// <summary>
        /// Gets or sets the default load-balancing strategy applied to pools that do not specify
        /// their own <see cref="UpstreamPool.Strategy"/>. Defaults to
        /// <see cref="LoadBalancingStrategy.RoundRobin"/>.
        /// </summary>
        public LoadBalancingStrategy DefaultStrategy { get; set; } = LoadBalancingStrategy.RoundRobin;

        // ─── Active Health Probing ─────────────────────────────────────────────

        /// <summary>
        /// Gets or sets the interval in seconds between active TCP health probes for each upstream.
        /// Applies when <see cref="ActiveHealthEnabled"/> is <c>true</c>. Defaults to <c>30</c> seconds.
        /// </summary>
        public int HealthCheckIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the TCP connection timeout in milliseconds used by active health probes.
        /// A probe that does not connect within this window is recorded as a failure. Defaults to <c>5000</c> ms.
        /// </summary>
        public int HealthProbeTimeoutMs { get; set; } = 5_000;

        /// <summary>
        /// Gets or sets the URI path written into the generated Caddyfile for Caddy's built-in
        /// active health-probe requests. Defaults to <c>"/health"</c>.
        /// </summary>
        public string HealthProbePath { get; set; } = "/health";

        /// <summary>
        /// Gets or sets whether active periodic health probing is enabled globally across all pools.
        /// Per-pool <see cref="UpstreamPool.ActiveHealthEnabled"/> overrides this value.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool ActiveHealthEnabled { get; set; } = true;

        // ─── Passive Health Tracking ──────────────────────────────────────────

        /// <summary>
        /// Gets or sets whether passive health tracking is enabled globally.
        /// Passive tracking degrades an upstream's health score when proxied requests fail without
        /// an explicit probe, providing faster failure detection under load.
        /// Per-pool <see cref="UpstreamPool.PassiveHealthEnabled"/> overrides this value.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool PassiveHealthEnabled { get; set; } = true;

        // ─── Threshold Configuration ──────────────────────────────────────────

        /// <summary>
        /// Gets or sets the number of consecutive failed probes required to transition an upstream
        /// to <see cref="UpstreamServerStatus.Unhealthy"/>. Defaults to <c>3</c>.
        /// </summary>
        public int UnhealthyThreshold { get; set; } = 3;

        /// <summary>
        /// Gets or sets the number of consecutive successful probes required to promote an unhealthy
        /// upstream back to <see cref="UpstreamServerStatus.Active"/>. Defaults to <c>2</c>.
        /// </summary>
        public int HealthyThreshold { get; set; } = 2;

        // ─── Retry Configuration ──────────────────────────────────────────────

        /// <summary>
        /// Gets or sets the default maximum number of retry attempts routed to a <em>different</em>
        /// upstream when the first selection fails. Defaults to <c>2</c>.
        /// </summary>
        public int MaxRetries { get; set; } = 2;

        /// <summary>
        /// Gets or sets the total time window in seconds within which retries are allowed.
        /// Once this duration has elapsed from the first attempt, no further retries occur.
        /// Defaults to <c>30</c> seconds.
        /// </summary>
        public int RetryDurationSeconds { get; set; } = 30;

        // ─── Sticky Sessions ──────────────────────────────────────────────────

        /// <summary>
        /// Gets or sets whether sticky-session support is enabled across pools.
        /// When <c>true</c>, pools with a configured <see cref="UpstreamPool.StickyCookieName"/> pin
        /// subsequent requests from the same session to the same upstream. Defaults to <c>false</c>.
        /// </summary>
        public bool StickySessionEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the default cookie name used for sticky-session affinity when
        /// <see cref="StickySessionEnabled"/> is <c>true</c> and a pool does not specify its own.
        /// Defaults to <c>"lb_upstream"</c>.
        /// </summary>
        public string DefaultStickyCookieName { get; set; } = "lb_upstream";

        // ─── Circuit Breaker ──────────────────────────────────────────────────

        /// <summary>
        /// Gets or sets whether the circuit breaker is enabled across all pools.
        /// When a pool has zero available upstreams the circuit opens, returning a service-unavailable
        /// response immediately rather than accumulating in-flight requests against degraded backends.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool CircuitBreakerEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum fraction of healthy upstreams (0.0–1.0) below which the circuit
        /// breaker opens. A value of <c>0.0</c> (the default) means the breaker only opens when
        /// <em>all</em> upstreams are unavailable. A value of <c>0.5</c> opens it when fewer than
        /// half are healthy.
        /// </summary>
        public double CircuitBreakerHealthThreshold { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets the time in seconds a circuit remains open before a single half-open probe
        /// is attempted to test for recovery. Defaults to <c>60</c> seconds.
        /// </summary>
        public int CircuitBreakerRecoverySeconds { get; set; } = 60;

        // ─── Connection Draining ──────────────────────────────────────────────

        /// <summary>
        /// Gets or sets the maximum time in seconds granted for in-flight connections to complete
        /// when an upstream enters the <see cref="UpstreamServerStatus.Draining"/> state.
        /// After this window elapses the upstream is forced to <see cref="UpstreamServerStatus.Disabled"/>.
        /// Defaults to <c>30</c> seconds.
        /// </summary>
        public int ConnectionDrainTimeoutSeconds { get; set; } = 30;
    }
}
