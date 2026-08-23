#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.LoadBalancing
{
    /// <summary>
    /// Default implementation of <see cref="IUpstreamHealthTracker"/> that persists health state
    /// through the <see cref="IUpstreamPoolRepository"/>. Probe results are applied against the
    /// unhealthy/healthy thresholds configured on each <see cref="UpstreamPool"/> and the updated
    /// pool state is flushed to the repository after every state transition.
    /// </summary>
    public sealed class UpstreamHealthTracker : IUpstreamHealthTracker
    {
        private readonly IUpstreamPoolRepository _poolRepository;
        private readonly object _healthLock = new object();

        /// <summary>
        /// Gets the pool repository used by this tracker.
        /// </summary>
        /// <returns>Read-only list of all upstream pools.</returns>
        internal async Task<IReadOnlyList<UpstreamPool>> GetAllPoolsAsync()
        {
            return (await _poolRepository.GetAllAsync()).AsReadOnly();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpstreamHealthTracker"/> class.
        /// </summary>
        /// <param name="poolRepository">The <see cref="IUpstreamPoolRepository"/> to use for persisting health state.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="poolRepository"/> is null.</exception>
        public UpstreamHealthTracker(IUpstreamPoolRepository poolRepository)
        {
            _poolRepository = poolRepository ?? throw new ArgumentNullException(nameof(poolRepository));
        }

        /// <summary>
        /// Records the result of a health probe for a specific server within an upstream pool.
        /// </summary>
        /// <param name="upstreamId">The unique identifier of the upstream server.</param>
        /// <param name="poolId">The unique identifier of the upstream pool.</param>
        /// <param name="probeSucceeded">A value indicating whether the probe succeeded.</param>
        /// <param name="responseTimeMs">The response time of the probe in milliseconds.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RecordProbeResultAsync(string upstreamId, string poolId, bool probeSucceeded, int responseTimeMs = 0)
        {
            var pool = await _poolRepository.GetByIdAsync(poolId);
            if (pool is null) return;

            var server = pool.Servers.Find(s => s.Id == upstreamId);
            if (server is null) return;

            lock (_healthLock)
            {
                server.RecordHealthProbeResult(probeSucceeded, responseTimeMs);

                if (!probeSucceeded && server.ConsecutiveFailures >= pool.UnhealthyThreshold)
                {
                    server.Status = UpstreamServerStatus.Unhealthy;
                }
                else if (probeSucceeded && server.ConsecutiveSuccesses >= pool.HealthyThreshold && server.Status == UpstreamServerStatus.Unhealthy)
                {
                    // Start half-open recovery state instead of immediately promoting to Active
            server.Status = UpstreamServerStatus.HalfOpen;
            server.HalfOpenSuccesses = 0; // Reset counter for half-open state
                }
            }

            await _poolRepository.UpdateAsync(pool);
        }

        /// <summary>
        /// Retrieves a snapshot of the health status for a specific upstream server.
        /// </summary>
        /// <param name="upstreamId">The unique identifier of the upstream server.</param>
        /// <returns>A <see cref="UpstreamHealthSnapshot"/> if the server exists; otherwise, null.</returns>
        public async Task<UpstreamHealthSnapshot?> GetSnapshotAsync(string upstreamId)
        {
            // We need to find the server across all pools since we only have upstreamId.
            var pools = await _poolRepository.GetAllAsync();
            foreach (var pool in pools)
            {
                var server = pool.Servers.Find(s => s.Id == upstreamId);
                if (server is not null)
                {
                    return new UpstreamHealthSnapshot(
                        server.Id,
                        server.GetUpstreamAddress(),
                        server.IsHealthy,
                        server.Status,
                        server.ConsecutiveFailures,
                        server.AverageResponseTimeMs,
                        server.ActiveConnections,
                        server.LastCheckedAt
                    );
                }
            }
            return null;
        }

        /// <summary>
        /// Drains active connections from an upstream server before disabling it.
        /// </summary>
        /// <param name="upstreamId">The unique identifier of the upstream server.</param>
        /// <param name="drainTimeout">The maximum time to wait for connections to drain.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for drainage.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DrainAsync(string upstreamId, TimeSpan drainTimeout, CancellationToken cancellationToken = default)
        {
            var pools = await _poolRepository.GetAllAsync();
            foreach (var pool in pools)
            {
                var server = pool.Servers.Find(s => s.Id == upstreamId);
                if (server is not null)
                {
                    lock (_healthLock)
                    {
                        server.Status = UpstreamServerStatus.Draining;
                    }
                    await _poolRepository.UpdateAsync(pool);

                    // Track the pool instance the current server reference belongs to,
                    // so the final UpdateAsync persists the object we actually mutated
                    // (persisting the original stale pool would drop the Disabled state).
                    var currentPool = pool;
                    var deadline = DateTime.UtcNow.Add(drainTimeout);
                    while (server.ActiveConnections > 0 && DateTime.UtcNow < deadline)
                    {
                        await Task.Delay(250, cancellationToken);

                        // Re-fetch to get updated connections if they were updated externally
                        var updatedPool = await _poolRepository.GetByIdAsync(pool.Id);
                        server = updatedPool?.Servers.Find(s => s.Id == upstreamId);
                        if (server is null) break;
                        currentPool = updatedPool;
                    }

                    if (server is not null)
                    {
                        lock (_healthLock)
                        {
                            server.Status = UpstreamServerStatus.Disabled;
                        }
                        await _poolRepository.UpdateAsync(currentPool);
                    }
                    break;
                }
            }
        }
    }
}