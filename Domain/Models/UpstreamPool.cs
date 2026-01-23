// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Defines the algorithm used to distribute requests across upstream servers in a pool.
    /// </summary>
    public enum LoadBalancingStrategy
    {
        /// <summary>
        /// Requests are distributed in sequential order across the healthy upstream set.
        /// Provides even distribution under uniform request cost.
        /// </summary>
        RoundRobin = 0,

        /// <summary>
        /// Each incoming request is routed to the upstream with the fewest in-flight connections,
        /// reducing hot-spots under variable request latency.
        /// </summary>
        LeastConnections = 1,

        /// <summary>An upstream is chosen uniformly at random from the healthy set on each request.</summary>
        Random = 2,

        /// <summary>
        /// Upstreams are chosen randomly but biased by their configured <see cref="UpstreamServer.Weight"/>,
        /// allowing traffic to be proportionally skewed toward higher-capacity nodes.
        /// </summary>
        WeightedRandom = 3,

        /// <summary>
        /// Requests are deterministically pinned to an upstream based on a stable hash of the client IP address,
        /// providing soft session affinity without explicit session tracking.
        /// </summary>
        IpHash = 4
    }

    /// <summary>
    /// Represents a named pool of backend upstream servers that shares a load-balancing strategy,
    /// health-check policy, and retry configuration. A pool is owned by a single
    /// <see cref="ManagedService"/> and is the primary unit of Caddy config generation for v2
    /// dynamic upstream management.
    /// </summary>
    public sealed class UpstreamPool
    {
        /// <summary>Gets or sets the unique identifier for this pool.</summary>
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Gets or sets the human-readable name of this upstream pool.</summary>
        [Required]
        [StringLength(255, MinimumLength = 3)]
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the <see cref="ManagedService"/> this pool belongs to.
        /// All upstreams in the pool serve traffic on behalf of this service.
        /// </summary>
        [Required]
        public required string ServiceId { get; set; }

        /// <summary>Gets or sets the load-balancing strategy applied when selecting an upstream for each request.</summary>
        public LoadBalancingStrategy Strategy { get; set; } = LoadBalancingStrategy.RoundRobin;

        /// <summary>Gets or sets the collection of upstream servers that make up this pool.</summary>
        public List<UpstreamServer> Servers { get; set; } = new();

        /// <summary>
        /// Gets or sets whether passive health tracking is enabled.
        /// When <c>true</c>, proxied request failures automatically degrade an upstream's health score.
        /// </summary>
        public bool PassiveHealthEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether active periodic health probing is enabled.
        /// When <c>true</c>, the toolkit probes each upstream on the configured interval.
        /// </summary>
        public bool ActiveHealthEnabled { get; set; } = true;

        /// <summary>Gets or sets the interval in seconds between active health probes. Must be between 5 and 3600.</summary>
        [Range(5, 3600)]
        public int HealthCheckIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the number of consecutive failed probes required to mark an upstream
        /// <see cref="UpstreamServerStatus.Unhealthy"/>. Defaults to <c>3</c>.
        /// </summary>
        [Range(1, 20)]
        public int UnhealthyThreshold { get; set; } = 3;

        /// <summary>
        /// Gets or sets the number of consecutive successful probes required to promote an upstream
        /// back to <see cref="UpstreamServerStatus.Active"/>. Defaults to <c>2</c>.
        /// </summary>
        [Range(1, 20)]
        public int HealthyThreshold { get; set; } = 2;

        /// <summary>Gets or sets the maximum number of retries attempted against different upstreams on failure.</summary>
        [Range(0, 10)]
        public int MaxRetries { get; set; } = 2;

        /// <summary>
        /// Gets or sets the total window in seconds during which retry attempts may occur.
        /// Once this duration elapses from the first attempt, no further retries are made.
        /// </summary>
        [Range(1, 300)]
        public int RetryDurationSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the name of the sticky-session cookie. When set, requests carrying this
        /// cookie are pinned to the upstream that set it. <c>null</c> disables sticky sessions.
        /// </summary>
        public string? StickyCookieName { get; set; }

        /// <summary>
        /// Gets or sets the URI path used by Caddy's built-in active health probing.
        /// Defaults to <c>"/health"</c>.
        /// </summary>
        public string HealthProbePath { get; set; } = "/health";

        /// <summary>Gets or sets whether this pool is active and eligible to serve traffic.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Gets the UTC timestamp when this pool was created. Immutable after construction.</summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>Gets or sets the UTC timestamp of the last modification to this pool.</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ─── Computed Members ─────────────────────────────────────────────────

        /// <summary>
        /// Returns the subset of upstream servers that are currently eligible to receive traffic
        /// (i.e. status is <see cref="UpstreamServerStatus.Active"/> and <see cref="UpstreamServer.IsHealthy"/> is <c>true</c>).
        /// </summary>
        public List<UpstreamServer> GetAvailableServers() =>
            Servers.Where(s => s.IsAvailable()).ToList();

        /// <summary>Returns the total number of active in-flight connections across all servers in this pool.</summary>
        public int GetTotalActiveConnections() =>
            Servers.Sum(s => s.ActiveConnections);

        // ─── Caddy Config Generation ──────────────────────────────────────────

        /// <summary>
        /// Generates the Caddy <c>reverse_proxy</c> configuration block for this pool,
        /// using only the currently available upstreams. Falls back to all non-disabled servers
        /// when no healthy upstream is present so that Caddy always has a valid configuration.
        /// The output is suitable for embedding directly in a Caddyfile.
        /// </summary>
        /// <param name="matchPath">
        /// The path pattern to match (e.g. <c>"/*"</c> or <c>"/api/*"</c>). Defaults to <c>"/*"</c>.
        /// </param>
        /// <returns>A multi-line Caddyfile <c>reverse_proxy</c> block string.</returns>
        public string GenerateCaddyUpstreamBlock(string matchPath = "/*")
        {
            var sb = new StringBuilder();
            var candidates = GetAvailableServers();

            if (candidates.Count == 0)
                candidates = Servers.Where(s => s.Status != UpstreamServerStatus.Disabled).ToList();

            sb.AppendLine($"\treverse_proxy {matchPath} {{");

            foreach (var server in candidates)
                sb.AppendLine($"\t\tto {server.GetUpstreamAddress()} weight:{server.Weight}");

            sb.AppendLine($"\t\tlb_policy {MapStrategyToCaddy(Strategy)}");

            if (MaxRetries > 0)
                sb.AppendLine($"\t\tlb_retries {MaxRetries}");

            if (RetryDurationSeconds > 0)
                sb.AppendLine($"\t\tlb_try_duration {RetryDurationSeconds}s");

            if (ActiveHealthEnabled)
            {
                sb.AppendLine($"\t\thealth_uri {HealthProbePath}");
                sb.AppendLine($"\t\thealth_interval {HealthCheckIntervalSeconds}s");
            }

            if (PassiveHealthEnabled)
            {
                sb.AppendLine($"\t\tfail_duration 10s");
                sb.AppendLine($"\t\tmax_fails {UnhealthyThreshold}");
                sb.AppendLine($"\t\tunhealthy_request_count {UnhealthyThreshold}");
            }

            if (!string.IsNullOrWhiteSpace(StickyCookieName))
                sb.AppendLine($"\t\tlb_policy cookie {StickyCookieName}");

            sb.AppendLine("\t}");
            return sb.ToString();
        }

        // ─── Validation ───────────────────────────────────────────────────────

        /// <summary>
        /// Validates this pool and all contained upstream servers.
        /// </summary>
        /// <exception cref="ValidationException">Thrown when any validation constraint is violated.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new ValidationException("Pool name is required");

            if (string.IsNullOrWhiteSpace(ServiceId))
                throw new ValidationException("Pool must be associated with a service identifier");

            if (Servers is null || Servers.Count == 0)
                throw new ValidationException("Pool must contain at least one upstream server");

            foreach (var server in Servers)
                server.Validate();
        }

        // ─── Private Helpers ──────────────────────────────────────────────────

        private static string MapStrategyToCaddy(LoadBalancingStrategy strategy) => strategy switch
        {
            LoadBalancingStrategy.RoundRobin      => "round_robin",
            LoadBalancingStrategy.LeastConnections => "least_conn",
            LoadBalancingStrategy.Random           => "random",
            LoadBalancingStrategy.WeightedRandom   => "random",
            LoadBalancingStrategy.IpHash           => "ip_hash",
            _                                      => "round_robin"
        };
    }
}
