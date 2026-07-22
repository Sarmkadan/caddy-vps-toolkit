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

        public UpstreamHealthTracker(IUpstreamPoolRepository poolRepository)
        {
            _poolRepository = poolRepository ?? throw new ArgumentNullException(nameof(poolRepository));
        }

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
                    server.Status = UpstreamServerStatus.Active;
                }
            }

            await _poolRepository.UpdateAsync(pool);
        }

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

                    var deadline = DateTime.UtcNow.Add(drainTimeout);
                    while (server.ActiveConnections > 0 && DateTime.UtcNow < deadline)
                    {
                        await Task.Delay(250, cancellationToken);

                        // Re-fetch to get updated connections if they were updated externally
                        var updatedPool = await _poolRepository.GetByIdAsync(pool.Id);
                        server = updatedPool?.Servers.Find(s => s.Id == upstreamId);
                        if (server is null) break;
                    }

                    if (server is not null)
                    {
                        lock (_healthLock)
                        {
                            server.Status = UpstreamServerStatus.Disabled;
                        }
                        await _poolRepository.UpdateAsync(pool);
                    }
                    break;
                }
            }
        }
    }
}