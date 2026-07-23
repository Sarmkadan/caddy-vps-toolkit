// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            ArgumentNullException.ThrowIfNull(servers);
            ArgumentNullException.ThrowIfNull(context);

            if (servers.Count == 0)
                return null;

            if (servers.Count == 1)
                return servers[0];

            // Determine strategy from context or use default
            var strategy = context.Strategy ?? LoadBalancingStrategy.RoundRobin;

            return strategy switch
            {
                LoadBalancingStrategy.RoundRobin => SelectRoundRobin(servers, context.PoolId),
                LoadBalancingStrategy.LeastConnections => SelectLeastConnections(servers),
                LoadBalancingStrategy.Random => SelectRandom(servers),
                LoadBalancingStrategy.WeightedRandom => SelectWeightedRandom(servers),
                LoadBalancingStrategy.IpHash => !string.IsNullOrEmpty(context.ClientIp)
                    ? SelectByIpHash(servers, context.ClientIp)
                    : SelectRoundRobin(servers, context.PoolId),
                _ => SelectRoundRobin(servers, context.PoolId)
            };
        }

        private UpstreamServer SelectRoundRobin(IReadOnlyList<UpstreamServer> servers, string poolId)
        {
            // Use atomic lock to prevent race conditions when updating the cursor
            lock (_rrCursors)
            {
                var idx = _rrCursors.AddOrUpdate(
                    poolId,
                    addValue: 0,
                    updateValueFactory: (_, current) => (current + 1) % servers.Count);

                return servers[idx % servers.Count];
            }
        }

        private UpstreamServer SelectByIpHash(IReadOnlyList<UpstreamServer> servers, string clientIp)
        {
            var hash = Math.Abs(clientIp.GetHashCode(StringComparison.Ordinal));
            return servers[hash % servers.Count];
        }

        private UpstreamServer SelectLeastConnections(IReadOnlyList<UpstreamServer> servers)
        {
            // Find the server with the fewest active connections
            // If multiple servers have the same minimum, return the first one
            // ActiveConnections is an int, so reading it is atomic and thread-safe
            var minConnections = int.MaxValue;
            UpstreamServer? selected = null;

            foreach (var server in servers)
            {
                var active = server.ActiveConnections;
                if (active < minConnections)
                {
                    minConnections = active;
                    selected = server;
                }
            }

            return selected ?? servers[0];
        }

        private UpstreamServer SelectRandom(IReadOnlyList<UpstreamServer> servers)
        {
            // Select a server uniformly at random
            // Using Random.Shared for thread-safety
            var index = Random.Shared.Next(servers.Count);
            return servers[index];
        }

        private UpstreamServer SelectWeightedRandom(IReadOnlyList<UpstreamServer> servers)
        {
            // Weighted random selection based on UpstreamServer.Weight
            // Higher weights increase the probability of selection
            // ActiveConnections is an int, so reading it is atomic and thread-safe
            var totalWeight = 0;
            foreach (var server in servers)
            {
                totalWeight += server.Weight;
            }

            if (totalWeight <= 0)
            {
                // Fallback to uniform random if weights are invalid
                var index = Random.Shared.Next(servers.Count);
                return servers[index];
            }

            var randomValue = Random.Shared.Next(0, totalWeight);
            var cumulativeWeight = 0;

            foreach (var server in servers)
            {
                cumulativeWeight += server.Weight;
                if (randomValue < cumulativeWeight)
                {
                    return server;
                }
            }

            // Fallback: return the last server if something went wrong
            return servers[^1];
        }
    }
}
