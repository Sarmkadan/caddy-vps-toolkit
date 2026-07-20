// =============================================================================
// Author: Test Generator
// Tests for weighted random upstream selection
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.LoadBalancing;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Services
{
    /// <summary>
    /// Contains unit tests for <see cref="UpstreamSelector"/> weighted random selection.
    /// </summary>
    public sealed class UpstreamSelectorTests
    {
        private readonly UpstreamSelector _selector = new();

        /// <summary>
        /// Verifies that WeightedRandom strategy selects servers according to their weights.
        /// </summary>
        [Fact]
        public void Select_WeightedRandom_RespectsWeights()
        {
            // Create servers with different weights
            var servers = new List<UpstreamServer>
            {
                new UpstreamServer { Address = "127.0.0.1", Port = 8080, Weight = 1 },
                new UpstreamServer { Address = "127.0.0.1", Port = 8081, Weight = 2 },
                new UpstreamServer { Address = "127.0.0.1", Port = 8082, Weight = 3 }
            };

            var context = new UpstreamSelectionContext(
                "test-pool",
                null,
                null,
                LoadBalancingStrategy.WeightedRandom
            );

            // Run multiple selections and verify distribution approximates weights
            var results = new List<UpstreamServer>();
            var iterations = 10000;

            for (int i = 0; i < iterations; i++)
            {
                var selected = _selector.Select(servers, context);
                results.Add(selected);
            }

            // Calculate actual distribution
            var count1 = results.Count(s => s.Port == 8080);
            var count2 = results.Count(s => s.Port == 8081);
            var count3 = results.Count(s => s.Port == 8082);

            // Expected distribution based on weights (1:2:3 ratio)
            var totalWeight = 1 + 2 + 3;
            var expectedRatio1 = 1.0 / totalWeight;
            var expectedRatio2 = 2.0 / totalWeight;
            var expectedRatio3 = 3.0 / totalWeight;

            // Allow 15% tolerance for random distribution
            var tolerance = 0.15;

            count1.Should().BeInRange((int)(expectedRatio1 * iterations * (1 - tolerance)), (int)(expectedRatio1 * iterations * (1 + tolerance)),
                "Server with weight 1 should be selected approximately 1/6 of the time");
            count2.Should().BeInRange((int)(expectedRatio2 * iterations * (1 - tolerance)), (int)(expectedRatio2 * iterations * (1 + tolerance)),
                "Server with weight 2 should be selected approximately 2/6 of the time");
            count3.Should().BeInRange((int)(expectedRatio3 * iterations * (1 - tolerance)), (int)(expectedRatio3 * iterations * (1 + tolerance)),
                "Server with weight 3 should be selected approximately 3/6 of the time");
        }

        /// <summary>
        /// Verifies that WeightedRandom falls back to uniform random when total weight is 0.
        /// </summary>
        [Fact]
        public void Select_WeightedRandom_WithZeroTotalWeight_FallsBackToUniform()
        {
            var servers = new List<UpstreamServer>
            {
                new UpstreamServer { Address = "127.0.0.1", Port = 8080, Weight = 0 },
                new UpstreamServer { Address = "127.0.0.1", Port = 8081, Weight = 0 }
            };

            var context = new UpstreamSelectionContext(
                "test-pool",
                null,
                null,
                LoadBalancingStrategy.WeightedRandom
            );

            var results = new List<UpstreamServer>();
            var iterations = 1000;

            for (int i = 0; i < iterations; i++)
            {
                var selected = _selector.Select(servers, context);
                results.Add(selected);
            }

            // Both servers should be selected roughly equally
            var count1 = results.Count(s => s.Port == 8080);
            var count2 = results.Count(s => s.Port == 8081);

            count1.Should().BeInRange((int)(iterations * 0.35), (int)(iterations * 0.65));
            count2.Should().BeInRange((int)(iterations * 0.35), (int)(iterations * 0.65));
        }

        /// <summary>
        /// Verifies that WeightedRandom with single server always returns that server.
        /// </summary>
        [Fact]
        public void Select_WeightedRandom_WithSingleServer_ReturnsThatServer()
        {
            var server = new UpstreamServer { Address = "127.0.0.1", Port = 8080, Weight = 100 };
            var servers = new List<UpstreamServer> { server };

            var context = new UpstreamSelectionContext(
                "test-pool",
                null,
                null,
                LoadBalancingStrategy.WeightedRandom
            );

            var results = new List<UpstreamServer>();
            var iterations = 100;

            for (int i = 0; i < iterations; i++)
            {
                var selected = _selector.Select(servers, context);
                results.Add(selected);
            }

            results.Should().AllBeEquivalentTo(server);
        }

        /// <summary>
        /// Verifies that WeightedRandom with empty list returns null.
        /// </summary>
        [Fact]
        public void Select_WeightedRandom_WithEmptyList_ReturnsNull()
        {
            var servers = new List<UpstreamServer>();
            var context = new UpstreamSelectionContext(
                "test-pool",
                null,
                null,
                LoadBalancingStrategy.WeightedRandom
            );

            var result = _selector.Select(servers, context);
            result.Should().BeNull();
        }

        /// <summary>
        /// Verifies that LeastConnections strategy selects server with fewest active connections.
        /// </summary>
        [Fact]
        public void Select_LeastConnections_SelectsServerWithFewestConnections()
        {
            var servers = new List<UpstreamServer>
            {
                new UpstreamServer { Address = "127.0.0.1", Port = 8080, ActiveConnections = 10 },
                new UpstreamServer { Address = "127.0.0.1", Port = 8081, ActiveConnections = 5 },
                new UpstreamServer { Address = "127.0.0.1", Port = 8082, ActiveConnections = 8 }
            };

            var context = new UpstreamSelectionContext(
                "test-pool",
                null,
                null,
                LoadBalancingStrategy.LeastConnections
            );

            var selected = _selector.Select(servers, context);
            selected.Port.Should().Be(8081, "Server with 5 connections should be selected");
        }

        /// <summary>
        /// Verifies that Random strategy selects servers uniformly.
        /// </summary>
        [Fact]
        public void Select_Random_SelectsUniformly()
        {
            var servers = new List<UpstreamServer>
            {
                new UpstreamServer { Address = "127.0.0.1", Port = 8080 },
                new UpstreamServer { Address = "127.0.0.1", Port = 8081 },
                new UpstreamServer { Address = "127.0.0.1", Port = 8082 }
            };

            var context = new UpstreamSelectionContext(
                "test-pool",
                null,
                null,
                LoadBalancingStrategy.Random
            );

            var results = new List<UpstreamServer>();
            var iterations = 1000;

            for (int i = 0; i < iterations; i++)
            {
                var selected = _selector.Select(servers, context);
                results.Add(selected);
            }

            // All servers should be selected roughly equally
            var count1 = results.Count(s => s.Port == 8080);
            var count2 = results.Count(s => s.Port == 8081);
            var count3 = results.Count(s => s.Port == 8082);

            count1.Should().BeInRange((int)(iterations * 0.2), (int)(iterations * 0.4));
            count2.Should().BeInRange((int)(iterations * 0.2), (int)(iterations * 0.4));
            count3.Should().BeInRange((int)(iterations * 0.2), (int)(iterations * 0.4));
        }

        /// <summary>
        /// Verifies that RoundRobin cycles through servers in order.
        /// </summary>
        [Fact]
        public void Select_RoundRobin_CyclesThroughServers()
        {
            var servers = new List<UpstreamServer>
            {
                new UpstreamServer { Address = "127.0.0.1", Port = 8080 },
                new UpstreamServer { Address = "127.0.0.1", Port = 8081 },
                new UpstreamServer { Address = "127.0.0.1", Port = 8082 }
            };

            var context = new UpstreamSelectionContext(
                "test-pool",
                null,
                null,
                LoadBalancingStrategy.RoundRobin
            );

            var results = new List<UpstreamServer>();
            var iterations = 9;

            for (int i = 0; i < iterations; i++)
            {
                var selected = _selector.Select(servers, context);
                results.Add(selected);
            }

            // Should cycle through servers: 0, 1, 2, 0, 1, 2, 0, 1, 2
            results[0].Port.Should().Be(8080);
            results[1].Port.Should().Be(8081);
            results[2].Port.Should().Be(8082);
            results[3].Port.Should().Be(8080);
            results[4].Port.Should().Be(8081);
            results[5].Port.Should().Be(8082);
            results[6].Port.Should().Be(8080);
            results[7].Port.Should().Be(8081);
            results[8].Port.Should().Be(8082);
        }

        /// <summary>
        /// Verifies that IpHash pins client to same server deterministically.
        /// </summary>
        [Fact]
        public void Select_IpHash_PinsClientToSameServer()
        {
            var servers = new List<UpstreamServer>
            {
                new UpstreamServer { Address = "127.0.0.1", Port = 8080 },
                new UpstreamServer { Address = "127.0.0.1", Port = 8081 },
                new UpstreamServer { Address = "127.0.0.1", Port = 8082 }
            };

            var clientIp = "192.168.1.100";
            var context = new UpstreamSelectionContext(
                "test-pool",
                clientIp,
                null,
                LoadBalancingStrategy.IpHash
            );

            // Same IP should always select same server
            var firstSelection = _selector.Select(servers, context);
            for (int i = 0; i < 10; i++)
            {
                var selected = _selector.Select(servers, context);
                selected.Should().BeSameAs(firstSelection, "Same client IP should always select same server");
            }

            // Different IP should select different server (or at least not guaranteed to be same)
            var differentIp = "10.0.0.1";
            var differentContext = new UpstreamSelectionContext(
                "test-pool",
                differentIp,
                null,
                LoadBalancingStrategy.IpHash
            );

            var differentSelection = _selector.Select(servers, differentContext);
            differentSelection.Should().NotBeSameAs(firstSelection, "Different client IP should typically select different server");
        }
    }
}
