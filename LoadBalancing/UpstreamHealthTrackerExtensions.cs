#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.LoadBalancing
{
    /// <summary>
    /// Extension methods for <see cref="UpstreamHealthTracker"/> that provide additional health monitoring and management functionality.
    /// </summary>
    public static class UpstreamHealthTrackerExtensions
    {
        /// <summary>
        /// Records multiple probe results at once for batch processing.
        /// </summary>
        /// <param name="tracker">The health tracker instance.</param>
        /// <param name="results">Collection of probe results to record.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tracker"/> or <paramref name="results"/> is null.</exception>
        public static async Task RecordProbeResultsAsync(this UpstreamHealthTracker tracker, IEnumerable<UpstreamProbeResult> results)
        {
            ArgumentNullException.ThrowIfNull(tracker);
            ArgumentNullException.ThrowIfNull(results);

            foreach (var result in results)
            {
                await tracker.RecordProbeResultAsync(
                    result.UpstreamId,
                    result.PoolId,
                    result.ProbeSucceeded,
                    result.ResponseTimeMs
                );
            }
        }

        /// <summary>
        /// Gets a snapshot for all upstreams across all pools.
        /// </summary>
        /// <param name="tracker">The health tracker instance.</param>
        /// <returns>Read-only collection of health snapshots for all upstreams, or empty collection if none found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tracker"/> is null.</exception>
        public static async Task<IReadOnlyList<UpstreamHealthSnapshot>> GetAllSnapshotsAsync(this UpstreamHealthTracker tracker)
        {
            ArgumentNullException.ThrowIfNull(tracker);

            var pools = await tracker.GetAllPoolsAsync();
            var snapshots = new List<UpstreamHealthSnapshot>();

            foreach (var pool in pools)
            {
                foreach (var server in pool.Servers)
                {
                    snapshots.Add(new UpstreamHealthSnapshot(
                        server.Id,
                        server.GetUpstreamAddress(),
                        server.IsHealthy,
                        server.Status,
                        server.ConsecutiveFailures,
                        server.AverageResponseTimeMs,
                        server.ActiveConnections,
                        server.LastCheckedAt
                    ));
                }
            }

            return snapshots.AsReadOnly();
        }

        /// <summary>
        /// Gets all unhealthy upstreams across all pools.
        /// </summary>
        /// <param name="tracker">The health tracker instance.</param>
        /// <returns>Read-only collection of health snapshots for unhealthy upstreams, or empty collection if none found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tracker"/> is null.</exception>
        public static async Task<IReadOnlyList<UpstreamHealthSnapshot>> GetUnhealthyUpstreamsAsync(this UpstreamHealthTracker tracker)
        {
            ArgumentNullException.ThrowIfNull(tracker);

            var allSnapshots = await tracker.GetAllSnapshotsAsync();
            return allSnapshots
                .Where(s => !s.IsHealthy)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets all healthy upstreams across all pools.
        /// </summary>
        /// <param name="tracker">The health tracker instance.</param>
        /// <returns>Read-only collection of health snapshots for healthy upstreams, or empty collection if none found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tracker"/> is null.</exception>
        public static async Task<IReadOnlyList<UpstreamHealthSnapshot>> GetHealthyUpstreamsAsync(this UpstreamHealthTracker tracker)
        {
            ArgumentNullException.ThrowIfNull(tracker);

            var allSnapshots = await tracker.GetAllSnapshotsAsync();
            return allSnapshots
                .Where(s => s.IsHealthy)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Drains an upstream with a default timeout of 30 seconds.
        /// </summary>
        /// <param name="tracker">The health tracker instance.</param>
        /// <param name="upstreamId">The upstream identifier to drain.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tracker"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="upstreamId"/> is null or empty.</exception>
        public static Task DrainAsync(this UpstreamHealthTracker tracker, string upstreamId)
        {
            ArgumentNullException.ThrowIfNull(tracker);
            ArgumentException.ThrowIfNullOrEmpty(upstreamId);

            return tracker.DrainAsync(upstreamId, TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Gets the health status summary for all pools.
        /// </summary>
        /// <param name="tracker">The health tracker instance.</param>
        /// <returns>Collection of pool health summaries.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tracker"/> is null.</exception>
        public static async Task<IReadOnlyList<PoolHealthSummary>> GetPoolHealthSummariesAsync(this UpstreamHealthTracker tracker)
        {
            ArgumentNullException.ThrowIfNull(tracker);

            var pools = await tracker.GetAllPoolsAsync();
            var summaries = new List<PoolHealthSummary>();

            foreach (var pool in pools)
            {
                var active = pool.Servers.Count(s => s.Status == UpstreamServerStatus.Active);
                var unhealthy = pool.Servers.Count(s => s.Status == UpstreamServerStatus.Unhealthy);
                var draining = pool.Servers.Count(s => s.Status == UpstreamServerStatus.Draining);
                var disabled = pool.Servers.Count(s => s.Status == UpstreamServerStatus.Disabled);

                summaries.Add(new PoolHealthSummary(
                    pool.Id,
                    pool.Name,
                    active,
                    unhealthy,
                    draining,
                    disabled,
                    pool.Servers.Count,
                    pool.UnhealthyThreshold,
                    pool.HealthyThreshold
                ));
            }

            return summaries.AsReadOnly();
        }

        /// <summary>
        /// Gets the overall system health summary across all pools.
        /// </summary>
        /// <param name="tracker">The health tracker instance.</param>
        /// <returns>System health summary.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tracker"/> is null.</exception>
        public static async Task<SystemHealthSummary> GetSystemHealthSummaryAsync(this UpstreamHealthTracker tracker)
        {
            ArgumentNullException.ThrowIfNull(tracker);

            var poolSummaries = await tracker.GetPoolHealthSummariesAsync();
            var totalUpstreams = poolSummaries.Sum(p => p.TotalServers);
            var totalActive = poolSummaries.Sum(p => p.ActiveServers);
            var totalUnhealthy = poolSummaries.Sum(p => p.UnhealthyServers);
            var totalDraining = poolSummaries.Sum(p => p.DrainingServers);
            var totalDisabled = poolSummaries.Sum(p => p.DisabledServers);

            var unhealthyPools = poolSummaries.Count(p => p.UnhealthyServers > 0);
            var healthyPools = poolSummaries.Count(p => p.UnhealthyServers == 0);

            return new SystemHealthSummary(
                totalUpstreams,
                totalActive,
                totalUnhealthy,
                totalDraining,
                totalDisabled,
                unhealthyPools,
                healthyPools,
                DateTime.UtcNow
            );
        }

        /// <summary>
        /// Waits for an upstream to become healthy within a specified timeout.
        /// </summary>
        /// <param name="tracker">The health tracker instance.</param>
        /// <param name="upstreamId">The upstream identifier to wait for.</param>
        /// <param name="timeout">Maximum time to wait for the upstream to become healthy.</param>
        /// <param name="pollInterval">Interval between health checks.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the asynchronous operation. Returns true if upstream became healthy, false if timeout was reached.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tracker"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="upstreamId"/> is null or empty.</exception>
        public static async Task<bool> WaitForHealthyAsync(this UpstreamHealthTracker tracker, string upstreamId, TimeSpan timeout, TimeSpan pollInterval, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(tracker);
            ArgumentException.ThrowIfNullOrEmpty(upstreamId);

            var deadline = DateTime.UtcNow.Add(timeout);

            while (DateTime.UtcNow < deadline)
            {
                var snapshot = await tracker.GetSnapshotAsync(upstreamId);
                if (snapshot?.IsHealthy == true)
                {
                    return true;
                }

                await Task.Delay(pollInterval, cancellationToken);
            }

            return false;
        }

        private static async Task<IReadOnlyList<UpstreamPool>> GetAllPoolsAsync(this UpstreamHealthTracker tracker)
        {
            // This is a helper method to access the internal repository
            // We use reflection to get the private field
            var field = typeof(UpstreamHealthTracker).GetField("_poolRepository", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(tracker) is IUpstreamPoolRepository repository)
            {
                return await repository.GetAllAsync();
            }

            return Array.Empty<UpstreamPool>();
        }
    }

    /// <summary>
    /// Represents a probe result for batch processing.
    /// </summary>
    public sealed class UpstreamProbeResult
    {
        public string UpstreamId { get; }
        public string PoolId { get; }
        public bool ProbeSucceeded { get; }
        public int ResponseTimeMs { get; }

        public UpstreamProbeResult(string upstreamId, string poolId, bool probeSucceeded, int responseTimeMs = 0)
        {
            UpstreamId = upstreamId ?? throw new ArgumentNullException(nameof(upstreamId));
            PoolId = poolId ?? throw new ArgumentNullException(nameof(poolId));
            ProbeSucceeded = probeSucceeded;
            ResponseTimeMs = responseTimeMs;
        }
    }

    /// <summary>
    /// Summary of health status for a pool.
    /// </summary>
    public sealed class PoolHealthSummary
    {
        public string PoolId { get; }
        public string PoolName { get; }
        public int ActiveServers { get; }
        public int UnhealthyServers { get; }
        public int DrainingServers { get; }
        public int DisabledServers { get; }
        public int TotalServers { get; }
        public int UnhealthyThreshold { get; }
        public int HealthyThreshold { get; }

        public PoolHealthSummary(string poolId, string poolName, int activeServers, int unhealthyServers, int drainingServers, int disabledServers, int totalServers, int unhealthyThreshold, int healthyThreshold)
        {
            PoolId = poolId ?? throw new ArgumentNullException(nameof(poolId));
            PoolName = poolName ?? throw new ArgumentNullException(nameof(poolName));
            ActiveServers = activeServers;
            UnhealthyServers = unhealthyServers;
            DrainingServers = drainingServers;
            DisabledServers = disabledServers;
            TotalServers = totalServers;
            UnhealthyThreshold = unhealthyThreshold;
            HealthyThreshold = healthyThreshold;
        }
    }

    /// <summary>
    /// Summary of health status for the entire system.
    /// </summary>
    public sealed class SystemHealthSummary
    {
        public int TotalUpstreams { get; }
        public int TotalActive { get; }
        public int TotalUnhealthy { get; }
        public int TotalDraining { get; }
        public int TotalDisabled { get; }
        public int UnhealthyPools { get; }
        public int HealthyPools { get; }
        public DateTime Timestamp { get; }

        public SystemHealthSummary(int totalUpstreams, int totalActive, int totalUnhealthy, int totalDraining, int totalDisabled, int unhealthyPools, int healthyPools, DateTime timestamp)
        {
            TotalUpstreams = totalUpstreams;
            TotalActive = totalActive;
            TotalUnhealthy = totalUnhealthy;
            TotalDraining = totalDraining;
            TotalDisabled = totalDisabled;
            UnhealthyPools = unhealthyPools;
            HealthyPools = healthyPools;
            Timestamp = timestamp;
        }
    }
}