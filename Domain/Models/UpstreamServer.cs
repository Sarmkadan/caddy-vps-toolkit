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
        Unhealthy = 3
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
        public bool IsAvailable() => Status == UpstreamServerStatus.Active && IsHealthy;

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
                AverageResponseTimeMs = AverageResponseTimeMs == 0
                    ? responseTimeMs
                    : (int)((AverageResponseTimeMs * 0.8) + (responseTimeMs * 0.2));
            }
            else
            {
                ConsecutiveSuccesses = 0;
                ConsecutiveFailures++;
            }

            IsHealthy = probeSucceeded;
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
