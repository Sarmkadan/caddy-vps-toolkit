// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Describes the operational status of an upstream server within a load-balanced pool.
    /// </summary>
    public enum UpstreamServerStatus
    {
        /// <summary>The server is active and eligible to receive proxied traffic.</summary>
        Active = 0,

        /// <summary>The server is undergoing a graceful connection drain before maintenance.</summary>
        Draining = 1,

        /// <summary>The server has been administratively disabled and receives no traffic.</summary>
        Disabled = 2,

        /// <summary>The server has been automatically marked unhealthy by the health-checking subsystem.</summary>
        Unhealthy = 3,

        /// <summary>
        /// The server is in a half-open recovery state. It has passed some health probes but not enough
        /// to be considered fully healthy. Traffic is limited to prevent overwhelming a potentially
        /// still-fragile upstream.
        /// </summary>
        HalfOpen = 4
    }

    /// <summary>
    /// Represents a single backend upstream server registered in an <see cref="UpstreamPool"/>.
    /// Tracks its own health state, connection metrics, and availability so that the load-balancer
    /// can make informed routing decisions at runtime.
    /// </summary>
    public sealed class UpstreamServer
    {
        /// <summary>Gets or sets the unique identifier for this upstream server.</summary>
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Gets or sets the IP address or hostname of the upstream server.</summary>
        [Required]
        [StringLength(253, MinimumLength = 1)]
        public required string Address { get; set; }

        /// <summary>Gets or sets the TCP port the upstream server listens on (1–65535).</summary>
        [Range(1, 65535)]
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the relative weight used by weighted load-balancing strategies.
        /// Higher values increase the probability of selection. Defaults to <c>1</c>.
        /// </summary>
        [Range(1, 100)]
        public int Weight { get; set; } = 1;

        /// <summary>Gets or sets the current operational status of this upstream.</summary>
        public UpstreamServerStatus Status { get; set; } = UpstreamServerStatus.Active;

        /// <summary>Gets or sets whether this upstream is currently considered healthy by the health subsystem.</summary>
        public bool IsHealthy { get; set; } = true;

        /// <summary>Gets or sets the UTC timestamp of the most recent health probe. <c>null</c> if never probed.</summary>
        public DateTime? LastCheckedAt { get; set; }

        /// <summary>Gets or sets the count of consecutive failed health probes since the last recovery.</summary>
        public int ConsecutiveFailures { get; set; }

        /// <summary>Gets or sets the count of consecutive successful health probes since the last failure.</summary>
        public int ConsecutiveSuccesses { get; set; }

        /// <summary>
        /// Gets or sets the count of consecutive successful half-open probes during recovery.
        /// Used to determine when an upstream in HalfOpen state should be promoted to Active.
        /// </summary>
        public int HalfOpenSuccesses { get; set; }

        /// <summary>
        /// Gets or sets the rolling average probe round-trip time in milliseconds.
        /// Updated on each successful probe using an exponential moving average.
        /// </summary>
        public int AverageResponseTimeMs { get; set; }

        /// <summary>Gets or sets the number of in-flight proxied connections currently routed to this upstream.</summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// Gets or sets optional free-text tags used for grouping or metadata (e.g. <c>"region:eu-west"</c>).
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>Gets or sets operator notes about this upstream server.</summary>
        public string? Notes { get; set; }

        // Thresholds for state transitions (injected or defaults)
        private int _unhealthyThreshold = 3; // Default unhealthy threshold
        private int _healthyThreshold = 2; // Default healthy threshold

        /// <summary>Gets the UTC timestamp when this upstream was registered. Immutable after construction.</summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>Gets or sets the UTC timestamp of the last mutation to this record.</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ─── Derived / Computed ───────────────────────────────────────────────

        /// <summary>
        /// Returns the full upstream address string in <c>host:port</c> format as expected by Caddy's
        /// <c>reverse_proxy</c> directive.
        /// </summary>
        public string GetUpstreamAddress() => $"{Address}:{Port}";

        /// <summary>
        /// Determines whether this upstream is currently eligible to receive proxied requests.
        /// An upstream is available when its status is <see cref="UpstreamServerStatus.Active"/> and it is healthy.
        /// </summary>
        public bool IsAvailable() => Status == UpstreamServerStatus.Active && IsHealthy
            || Status == UpstreamServerStatus.HalfOpen;

        // ─── Behaviour ────────────────────────────────────────────────────────

        /// <summary>
        /// Records the outcome of a single health probe, updating consecutive counters,
        /// the rolling average response time, and the <see cref="IsHealthy"/> flag.
        /// </summary>
        /// <param name="probeSucceeded"><c>true</c> if the probe completed successfully; <c>false</c> on failure.</param>
        /// <param name="responseTimeMs">
        /// The probe round-trip time in milliseconds. Ignored (treated as 0) when <paramref name="probeSucceeded"/> is <c>false</c>.
        /// </param>
        public void RecordHealthProbeResult(bool probeSucceeded, int responseTimeMs = 0)
        {
            LastCheckedAt = DateTime.UtcNow;
            UpdatedAt     = DateTime.UtcNow;

            if (probeSucceeded)
            {
                ConsecutiveFailures = 0;
                ConsecutiveSuccesses++;
                HalfOpenSuccesses++;
                AverageResponseTimeMs = AverageResponseTimeMs == 0
                    ? responseTimeMs
                    : (int)((AverageResponseTimeMs * 0.8) + (responseTimeMs * 0.2));
            }
            else
            {
                ConsecutiveSuccesses = 0;
                ConsecutiveFailures++;
                HalfOpenSuccesses = 0; // Reset half-open success count on failure
            }

            IsHealthy = probeSucceeded;

            // Handle state transitions based on current status and probe results
            HandleHealthStateTransition(probeSucceeded);
        }

        /// <summary>
        /// Handles state transitions for the upstream server based on health probe results.
        /// Implements half-open probing to gradually restore traffic to recovering upstreams.
        /// </summary>
        /// <param name="probeSucceeded">Whether the health probe succeeded.</param>
        private void HandleHealthStateTransition(bool probeSucceeded)
        {
            // State machine for health state transitions with half-open probing
            switch (Status)
            {
                case UpstreamServerStatus.Active:
                    // Transition to Unhealthy on too many consecutive failures
                    if (ConsecutiveFailures >= _unhealthyThreshold)
                    {
                        Status = UpstreamServerStatus.Unhealthy;
                    }
                    break;

                case UpstreamServerStatus.Unhealthy:
                    // Transition to HalfOpen after enough time has passed (cooldown period)
                    // This is handled by UpstreamHealthTracker based on LastCheckedAt
                    break;

                case UpstreamServerStatus.HalfOpen:
                    // In half-open state, successful probes promote to Active
                    // Failed probes demote back to Unhealthy
                    if (probeSucceeded)
                    {
                        // Promote to Active after sufficient consecutive successes in half-open
                        if (HalfOpenSuccesses >= _healthyThreshold)
                        {
                            Status = UpstreamServerStatus.Active;
                            ConsecutiveSuccesses = 0; // Reset for Active state tracking
                            HalfOpenSuccesses = 0;
                        }
                    }
                    else
                    {
                        // Demote back to Unhealthy on any failure in half-open state
                        Status = UpstreamServerStatus.Unhealthy;
                        HalfOpenSuccesses = 0;
                    }
                    break;

                case UpstreamServerStatus.Draining:
                case UpstreamServerStatus.Disabled:
                    // No state transitions from these states via health probes
                    break;
            }
        }

        /// <summary>
        /// Validates this upstream's configuration, throwing when required fields are absent or out of range.
        /// </summary>
        /// <exception cref="ValidationException">Thrown when any validation constraint is violated.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Address))
                throw new ValidationException("Upstream address is required");

            if (Port < 1 || Port > 65535)
                throw new ValidationException($"Upstream port must be between 1 and 65535, got {Port}");

            if (Weight < 1 || Weight > 100)
                throw new ValidationException($"Upstream weight must be between 1 and 100, got {Weight}");
        }
    }
}
