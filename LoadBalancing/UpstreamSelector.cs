#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.LoadBalancing
{
    /// <summary>
    /// Stateless implementation of <see cref="IUpstreamSelector"/> that supports round-robin
    /// and IP-hash load-balancing strategies.
    /// <para>
    /// When <see cref="UpstreamSelectionContext.ClientIp"/> is set the selector hashes the IP to
    /// produce a stable, deterministic upstream assignment. Otherwise a per-pool atomic round-robin
    /// cursor distributes requests evenly across all candidates.
    /// </para>
    /// </summary>
    public sealed class UpstreamSelector : IUpstreamSelector
    {
        private readonly ConcurrentDictionary<string, int> _rrCursors = new();

        /// <inheritdoc/>
        public UpstreamServer? Select(IReadOnlyList<UpstreamServer> servers, UpstreamSelectionContext context)
        {
            if (servers is null || servers.Count == 0)
                return null;

            if (servers.Count == 1)
                return servers[0];

            // If we have an IP address and we assume IP Hash might be requested, we can use it.
            // But without knowing the explicit strategy from context, we default to Round Robin,
            // or we could use the context's ClientIp as a hint. We will default to Round Robin 
            // per poolId as it's the most common default.
            
            if (!string.IsNullOrEmpty(context.ClientIp))
            {
                return SelectByIpHash(servers, context.ClientIp);
            }

            return SelectRoundRobin(servers, context.PoolId);
        }

        private UpstreamServer SelectRoundRobin(IReadOnlyList<UpstreamServer> servers, string poolId)
        {
            var idx = _rrCursors.AddOrUpdate(
                poolId,
                addValue: 0,
                updateValueFactory: (_, current) => (current + 1) % servers.Count);
            
            return servers[idx % servers.Count];
        }

        private UpstreamServer SelectByIpHash(IReadOnlyList<UpstreamServer> servers, string clientIp)
        {
            var hash = Math.Abs(clientIp.GetHashCode(StringComparison.Ordinal));
            return servers[hash % servers.Count];
        }
    }
}
