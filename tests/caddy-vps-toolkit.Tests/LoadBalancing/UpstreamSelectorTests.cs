#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.LoadBalancing;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.LoadBalancing
{
    /// <summary>
    /// Tests for the <see cref="UpstreamSelector"/> class.
    /// Covers empty pool, single upstream, and selection strategy behavior.
    /// </summary>
    public sealed class UpstreamSelectorTests
    {
        private readonly UpstreamSelector _selector = new();

        [Fact]
        public void Select_EmptyPool_ReturnsNull()
        {
            // Arrange
            var emptyServers = new List<UpstreamServer>();
            var context = new UpstreamSelectionContext("test-pool");

            // Act
            var result = _selector.Select(emptyServers, context);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Select_SingleUpstream_ReturnsThatUpstream()
        {
            // Arrange
            var server = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.1",
                Port = 8080,
                Weight = 1
            };
            var servers = new List<UpstreamServer> { server };
            var context = new UpstreamSelectionContext("test-pool");

            // Act
            var result = _selector.Select(servers, context);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(server);
        }

        [Fact]
        public void Select_SingleUpstreamWithDifferentStrategies_ReturnsThatUpstream()
        {
            // Arrange
            var server = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.1",
                Port = 8080,
                Weight = 1
            };
            var servers = new List<UpstreamServer> { server };

            // Test all strategies
            var strategies = Enum.GetValues<LoadBalancingStrategy>();
            var context = new UpstreamSelectionContext("test-pool");

            foreach (var strategy in strategies)
            {
                var strategyContext = context with { Strategy = strategy };

                // Act
                var result = _selector.Select(servers, strategyContext);

                // Assert
                result.Should().NotBeNull();
                result.Should().BeSameAs(server);
            }
        }

        [Fact]
        public void Select_RoundRobinStrategy_SelectsServersInSequence()
        {
            // Arrange
            var server1 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.1",
                Port = 8080,
                Weight = 1
            };
            var server2 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.2",
                Port = 8080,
                Weight = 1
            };
            var server3 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.3",
                Port = 8080,
                Weight = 1
            };
            var servers = new List<UpstreamServer> { server1, server2, server3 };
            var context = new UpstreamSelectionContext("test-pool")
            {
                Strategy = LoadBalancingStrategy.RoundRobin
            };

            // Act - select multiple times to test round-robin behavior
            var result1 = _selector.Select(servers, context);
            var result2 = _selector.Select(servers, context);
            var result3 = _selector.Select(servers, context);
            var result4 = _selector.Select(servers, context);
            var result5 = _selector.Select(servers, context);

            // Assert
            result1.Should().BeSameAs(server1);
            result2.Should().BeSameAs(server2);
            result3.Should().BeSameAs(server3);
            result4.Should().BeSameAs(server1); // Wraps around
            result5.Should().BeSameAs(server2);
        }

        [Fact]
        public void Select_RoundRobinWithDifferentPoolIds_UsesSeparateCursors()
        {
            // Arrange
            var server1 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.1",
                Port = 8080,
                Weight = 1
            };
            var server2 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.2",
                Port = 8080,
                Weight = 1
            };
            var servers = new List<UpstreamServer> { server1, server2 };
            var context1 = new UpstreamSelectionContext("pool-1")
            {
                Strategy = LoadBalancingStrategy.RoundRobin
            };
            var context2 = new UpstreamSelectionContext("pool-2")
            {
                Strategy = LoadBalancingStrategy.RoundRobin
            };

            // Act
            var result1_1 = _selector.Select(servers, context1);
            var result2_1 = _selector.Select(servers, context2);
            var result1_2 = _selector.Select(servers, context1);
            var result2_2 = _selector.Select(servers, context2);

            // Assert - Each pool should have its own cursor
            result1_1.Should().BeSameAs(server1);
            result2_1.Should().BeSameAs(server1); // pool-2 starts fresh
            result1_2.Should().BeSameAs(server2);
            result2_2.Should().BeSameAs(server2);
        }

        [Fact]
        public void Select_LeastConnectionsStrategy_SelectsServerWithFewestConnections()
        {
            // Arrange
            var server1 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.1",
                Port = 8080,
                Weight = 1
            };
            var server2 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.2",
                Port = 8080,
                Weight = 1
            };
            var server3 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.3",
                Port = 8080,
                Weight = 1
            };

            // Set different active connection counts
            server1.ActiveConnections = 5;
            server2.ActiveConnections = 2;
            server3.ActiveConnections = 8;

            var servers = new List<UpstreamServer> { server1, server2, server3 };
            var context = new UpstreamSelectionContext("test-pool")
            {
                Strategy = LoadBalancingStrategy.LeastConnections
            };

            // Act
            var result = _selector.Select(servers, context);

            // Assert
            result.Should().BeSameAs(server2); // Has only 2 connections (minimum)
        }

        [Fact]
        public void Select_LeastConnectionsWithTie_ReturnsFirstServerWithMinimum()
        {
            // Arrange
            var server1 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.1",
                Port = 8080,
                Weight = 1
            };
            var server2 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.2",
                Port = 8080,
                Weight = 1
            };

            // Both have same connection count (tie)
            server1.ActiveConnections = 3;
            server2.ActiveConnections = 3;

            var servers = new List<UpstreamServer> { server1, server2 };
            var context = new UpstreamSelectionContext("test-pool")
            {
                Strategy = LoadBalancingStrategy.LeastConnections
            };

            // Act
            var result = _selector.Select(servers, context);

            // Assert
            result.Should().BeSameAs(server1); // First server with minimum connections
        }

        [Fact]
        public void Select_LeastConnectionsWithAllZeroConnections_ReturnsFirstServer()
        {
            // Arrange
            var server1 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.1",
                Port = 8080,
                Weight = 1
            };
            var server2 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.2",
                Port = 8080,
                Weight = 1
            };

            // Both have zero connections
            server1.ActiveConnections = 0;
            server2.ActiveConnections = 0;

            var servers = new List<UpstreamServer> { server1, server2 };
            var context = new UpstreamSelectionContext("test-pool")
            {
                Strategy = LoadBalancingStrategy.LeastConnections
            };

            // Act
            var result = _selector.Select(servers, context);

            // Assert
            result.Should().BeSameAs(server1);
        }

        [Fact]
        public void Select_RandomStrategy_SelectsDifferentServersAcrossMultipleCalls()
        {
            // Arrange
            var server1 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.1",
                Port = 8080,
                Weight = 1
            };
            var server2 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.2",
                Port = 8080,
                Weight = 1
            };
            var servers = new List<UpstreamServer> { server1, server2 };
            var context = new UpstreamSelectionContext("test-pool")
            {
                Strategy = LoadBalancingStrategy.Random
            };

            // Act - select multiple times
            var results = new List<UpstreamServer>();
            for (int i = 0; i < 20; i++)
            {
                results.Add(_selector.Select(servers, context)!);
            }

            // Assert - Should get both servers in random distribution
            results.Should().Contain(server1);
            results.Should().Contain(server2);
            // Note: We can't assert exact distribution as it's random,
            // but we verify both servers are selected at least once
        }

        [Fact]
        public void Select_WeightedRandomStrategy_RespectsServerWeights()
        {
            // Arrange
            var server1 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.1",
                Port = 8080,
                Weight = 1
            };
            var server2 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.2",
                Port = 8080,
                Weight = 3
            };
            var server3 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.3",
                Port = 8080,
                Weight = 6
            };

            var servers = new List<UpstreamServer> { server1, server2, server3 };
            var context = new UpstreamSelectionContext("test-pool")
            {
                Strategy = LoadBalancingStrategy.WeightedRandom
            };

            // Act - select many times to get distribution
            var results = new List<UpstreamServer>();
            for (int i = 0; i < 1000; i++)
            {
                results.Add(_selector.Select(servers, context)!);
            }

            // Assert - Should roughly match weight distribution (1:3:6 ratio)
            var count1 = results.Count(s => s.Id == server1.Id);
            var count2 = results.Count(s => s.Id == server2.Id);
            var count3 = results.Count(s => s.Id == server3.Id);

            // Allow some variance in distribution
            count1.Should().BeGreaterThan(0);
            count2.Should().BeGreaterThan(0);
            count3.Should().BeGreaterThan(0);

            // server3 (weight 6) should be selected more than server2 (weight 3)
            count3.Should().BeGreaterThan(count2);
            // server2 (weight 3) should be selected more than server1 (weight 1)
            count2.Should().BeGreaterThan(count1);
        }

        [Fact]
        public void Select_WeightedRandomWithZeroTotalWeight_FallsBackToRandom()
        {
            // Arrange
            var server1 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.1",
                Port = 8080,
                Weight = 0
            };
            var server2 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.2",
                Port = 8080,
                Weight = 0
            };

            var servers = new List<UpstreamServer> { server1, server2 };
            var context = new UpstreamSelectionContext("test-pool")
            {
                Strategy = LoadBalancingStrategy.WeightedRandom
            };

            // Act - should not throw
            var result = _selector.Select(servers, context);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOneOf(server1, server2);
        }

        [Fact]
        public void Select_IpHashStrategy_WithClientIp_ReturnsDeterministicServer()
        {
            // Arrange
            var server1 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.1",
                Port = 8080,
                Weight = 1
            };
            var server2 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.2",
                Port = 8080,
                Weight = 1
            };
            var server3 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.3",
                Port = 8080,
                Weight = 1
            };
            var servers = new List<UpstreamServer> { server1, server2, server3 };

            // Same client IP should always select same server
            var clientIp = "192.168.1.100";
            var context = new UpstreamSelectionContext("test-pool", ClientIp: clientIp)
            {
                Strategy = LoadBalancingStrategy.IpHash
            };

            // Act - select multiple times with same IP
            var result1 = _selector.Select(servers, context);
            var result2 = _selector.Select(servers, context);
            var result3 = _selector.Select(servers, context);

            // Assert
            result1.Should().NotBeNull();
            result2.Should().BeSameAs(result1);
            result3.Should().BeSameAs(result1);
        }

        [Fact]
        public void Select_IpHashStrategy_DifferentClientIps_SelectsDifferentServers()
        {
            // Arrange
            var server1 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.1",
                Port = 8080,
                Weight = 1
            };
            var server2 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.2",
                Port = 8080,
                Weight = 1
            };
            var servers = new List<UpstreamServer> { server1, server2 };

            // Different client IPs
            var context1 = new UpstreamSelectionContext("test-pool", ClientIp: "192.168.1.100")
            {
                Strategy = LoadBalancingStrategy.IpHash
            };
            var context2 = new UpstreamSelectionContext("test-pool", ClientIp: "10.0.0.50")
            {
                Strategy = LoadBalancingStrategy.IpHash
            };

            // Act
            var result1 = _selector.Select(servers, context1);
            var result2 = _selector.Select(servers, context2);

            // Assert - Different IPs should map to different servers (likely)
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1.Should().NotBeSameAs(result2);

            // Verify both servers were selected at least once across multiple calls
            var allResults = new List<UpstreamServer> { result1!, result2! };
            allResults.Should().Contain(server1);
            allResults.Should().Contain(server2);
        }

        [Fact]
        public void Select_DefaultStrategy_UsesRoundRobin()
        {
            // Arrange
            var server1 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.1",
                Port = 8080,
                Weight = 1
            };
            var server2 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.2",
                Port = 8080,
                Weight = 1
            };
            var servers = new List<UpstreamServer> { server1, server2 };
            var context = new UpstreamSelectionContext("test-pool"); // No strategy specified

            // Act
            var result1 = _selector.Select(servers, context);
            var result2 = _selector.Select(servers, context);

            // Assert - Should use round-robin as default
            result1.Should().BeSameAs(server1);
            result2.Should().BeSameAs(server2);
        }

        [Fact]
        public void Select_UnknownStrategy_FallsBackToRoundRobin()
        {
            // Arrange
            var server1 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.1",
                Port = 8080,
                Weight = 1
            };
            var server2 = new UpstreamServer
            {
                Id = Guid.NewGuid().ToString(),
                Address = "192.168.1.2",
                Port = 8080,
                Weight = 1
            };
            var servers = new List<UpstreamServer> { server1, server2 };
            var context = new UpstreamSelectionContext("test-pool")
            {
                Strategy = (LoadBalancingStrategy)999 // Unknown strategy
            };

            // Act
            var result1 = _selector.Select(servers, context);
            var result2 = _selector.Select(servers, context);

            // Assert - Should fall back to round-robin
            result1.Should().BeSameAs(server1);
            result2.Should().BeSameAs(server2);
        }

        [Fact]
        public void Select_NullServersList_ReturnsNull()
        {
            // Arrange
            List<UpstreamServer>? servers = null;
            var context = new UpstreamSelectionContext("test-pool");

            // Act
            var result = _selector.Select(servers!, context);

            // Assert
            result.Should().BeNull();
        }
    }
}