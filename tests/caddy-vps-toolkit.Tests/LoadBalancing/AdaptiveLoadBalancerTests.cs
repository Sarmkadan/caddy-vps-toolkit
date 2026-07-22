// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// xUnit tests for AdaptiveLoadBalancer
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CaddyVpsToolkit.Configuration;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.LoadBalancing;
using CaddyVpsToolkit.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CaddyVpsToolkit.Tests.LoadBalancing
{
    public class AdaptiveLoadBalancerTests
    {
        private readonly UpstreamManagerService _upstreamManager;
        private readonly IMetricsAggregator _metricsAggregator;
        private readonly UpstreamManagementOptions _options;
        private readonly AdaptiveLoadBalancer _loadBalancer;

        public AdaptiveLoadBalancerTests()
        {
            _upstreamManager = Substitute.For<UpstreamManagerService>(
                Substitute.For<ServiceManagementService>(),
                Substitute.For<HealthMonitoringService>(),
                Substitute.For<CaddyConfigurationService>(),
                Substitute.For<LoadBalancingOptions>());

            _metricsAggregator = Substitute.For<IMetricsAggregator>();
            _options = new UpstreamManagementOptions
            {
                LatencyWeight = 0.4,
                ErrorRateWeight = 0.4,
                ConnectionWeight = 0.2,
                TargetLatencyMs = 200.0,
                MaxExpectedConnections = 100,
                WeightAdaptationAlpha = 0.15,
                PenaltyMultiplier = 0.3,
                PenaltyDecaySeconds = 60.0
            };

            _loadBalancer = new AdaptiveLoadBalancer(_upstreamManager, _metricsAggregator, _options);
        }

        [Fact]
        public async Task EvaluatePoolAsync_WithEmptyPool_ReturnsEmptyEvaluation()
        {
            // Arrange
            var poolId = Guid.NewGuid().ToString();
            var context = new UpstreamSelectionContext(poolId);

            _upstreamManager.GetPoolAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(null));

            // Act
            var result = await _loadBalancer.EvaluatePoolAsync(context);

            // Assert
            result.PoolId.Should().Be(poolId);
            result.Scores.Should().BeEmpty();
            result.SelectedUpstreamId.Should().BeNull();
            result.HasEligibleUpstream.Should().BeFalse();
        }

        [Fact]
        public async Task EvaluatePoolAsync_WithNoServers_ReturnsEmptyEvaluation()
        {
            // Arrange
            var poolId = Guid.NewGuid().ToString();
            var context = new UpstreamSelectionContext(poolId);

            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = Guid.NewGuid().ToString(),
                Servers = new List<UpstreamServer>()
            };

            _upstreamManager.GetPoolAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            var result = await _loadBalancer.EvaluatePoolAsync(context);

            // Assert
            result.PoolId.Should().Be(poolId);
            result.Scores.Should().BeEmpty();
            result.SelectedUpstreamId.Should().BeNull();
            result.HasEligibleUpstream.Should().BeFalse();
        }

        [Fact]
        public async Task EvaluatePoolAsync_WithSingleServer_ReturnsThatServerAsSelected()
        {
            // Arrange
            var poolId = Guid.NewGuid().ToString();
            var serverId = Guid.NewGuid().ToString();
            var context = new UpstreamSelectionContext(poolId);

            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = Guid.NewGuid().ToString(),
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = serverId,
                        Address = "127.0.0.1",
                        Port = 8080,
                        Weight = 1,
                        Status = UpstreamServerStatus.Active,
                        IsHealthy = true,
                        ActiveConnections = 0
                    }
                }
            };

            _upstreamManager.GetPoolAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            var result = await _loadBalancer.EvaluatePoolAsync(context);

            // Assert
            result.PoolId.Should().Be(poolId);
            result.Scores.Should().HaveCount(1);
            result.SelectedUpstreamId.Should().Be(serverId);
            result.HasEligibleUpstream.Should().BeTrue();
            result.Scores[0].UpstreamId.Should().Be(serverId);
            result.Scores[0].IsEligible.Should().BeTrue();
        }

        [Fact]
        public async Task EvaluatePoolAsync_WithMultipleServers_ReturnsRankedScores()
        {
            // Arrange
            var poolId = Guid.NewGuid().ToString();
            var context = new UpstreamSelectionContext(poolId);

            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = Guid.NewGuid().ToString(),
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = "server-1",
                        Address = "127.0.0.1",
                        Port = 8080,
                        Weight = 1,
                        Status = UpstreamServerStatus.Active,
                        IsHealthy = true,
                        ActiveConnections = 0
                    },
                    new UpstreamServer
                    {
                        Id = "server-2",
                        Address = "127.0.0.2",
                        Port = 8081,
                        Weight = 2,
                        Status = UpstreamServerStatus.Active,
                        IsHealthy = true,
                        ActiveConnections = 0
                    },
                    new UpstreamServer
                    {
                        Id = "server-3",
                        Address = "127.0.0.3",
                        Port = 8082,
                        Weight = 1,
                        Status = UpstreamServerStatus.Active,
                        IsHealthy = true,
                        ActiveConnections = 0
                    }
                }
            };

            _upstreamManager.GetPoolAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            var result = await _loadBalancer.EvaluatePoolAsync(context);

            // Assert
            result.PoolId.Should().Be(poolId);
            result.Scores.Should().HaveCount(3);
            result.SelectedUpstreamId.Should().NotBeNull();
            result.HasEligibleUpstream.Should().BeTrue();

            // Scores should be ordered by NormalizedScore descending
            var scores = result.Scores.ToList();
            scores[0].NormalizedScore.Should().BeGreaterOrEqualTo(scores[1].NormalizedScore);
            scores[1].NormalizedScore.Should().BeGreaterOrEqualTo(scores[2].NormalizedScore);
        }

        [Fact]
        public async Task EvaluatePoolAsync_WithUnhealthyServer_MarksAsIneligible()
        {
            // Arrange
            var poolId = Guid.NewGuid().ToString();
            var context = new UpstreamSelectionContext(poolId);

            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = Guid.NewGuid().ToString(),
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = "healthy-server",
                        Address = "127.0.0.1",
                        Port = 8080,
                        Weight = 1,
                        Status = UpstreamServerStatus.Active,
                        IsHealthy = true,
                        ActiveConnections = 0
                    },
                    new UpstreamServer
                    {
                        Id = "unhealthy-server",
                        Address = "127.0.0.2",
                        Port = 8081,
                        Weight = 1,
                        Status = UpstreamServerStatus.Active,
                        IsHealthy = false, // Unhealthy
                        ActiveConnections = 0
                    }
                }
            };

            _upstreamManager.GetPoolAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            var result = await _loadBalancer.EvaluatePoolAsync(context);

            // Assert
            result.PoolId.Should().Be(poolId);
            result.Scores.Should().HaveCount(2);
            result.SelectedUpstreamId.Should().Be("healthy-server");

            var healthyScore = result.Scores.First(s => s.UpstreamId == "healthy-server");
            var unhealthyScore = result.Scores.First(s => s.UpstreamId == "unhealthy-server");

            healthyScore.IsEligible.Should().BeTrue();
            unhealthyScore.IsEligible.Should().BeFalse();
        }

        [Fact]
        public async Task EvaluatePoolAsync_WithDisabledServer_MarksAsIneligible()
        {
            // Arrange
            var poolId = Guid.NewGuid().ToString();
            var context = new UpstreamSelectionContext(poolId);

            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = Guid.NewGuid().ToString(),
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = "active-server",
                        Address = "127.0.0.1",
                        Port = 8080,
                        Weight = 1,
                        Status = UpstreamServerStatus.Active,
                        IsHealthy = true,
                        ActiveConnections = 0
                    },
                    new UpstreamServer
                    {
                        Id = "disabled-server",
                        Address = "127.0.0.2",
                        Port = 8081,
                        Weight = 1,
                        Status = UpstreamServerStatus.Disabled, // Disabled
                        IsHealthy = true,
                        ActiveConnections = 0
                    }
                }
            };

            _upstreamManager.GetPoolAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            var result = await _loadBalancer.EvaluatePoolAsync(context);

            // Assert
            result.PoolId.Should().Be(poolId);
            result.Scores.Should().HaveCount(2);
            result.SelectedUpstreamId.Should().Be("active-server");

            var activeScore = result.Scores.First(s => s.UpstreamId == "active-server");
            var disabledScore = result.Scores.First(s => s.UpstreamId == "disabled-server");

            activeScore.IsEligible.Should().BeTrue();
            disabledScore.IsEligible.Should().BeFalse();
        }

        [Fact]
        public async Task EvaluatePoolAsync_WithAllUnhealthyServers_ReturnsNullSelection()
        {
            // Arrange
            var poolId = Guid.NewGuid().ToString();
            var context = new UpstreamSelectionContext(poolId);

            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = Guid.NewGuid().ToString(),
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = "unhealthy-1",
                        Address = "127.0.0.1",
                        Port = 8080,
                        Weight = 1,
                        Status = UpstreamServerStatus.Active,
                        IsHealthy = false,
                        ActiveConnections = 0
                    },
                    new UpstreamServer
                    {
                        Id = "unhealthy-2",
                        Address = "127.0.0.2",
                        Port = 8081,
                        Weight = 1,
                        Status = UpstreamServerStatus.Active,
                        IsHealthy = false,
                        ActiveConnections = 0
                    }
                }
            };

            _upstreamManager.GetPoolAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            var result = await _loadBalancer.EvaluatePoolAsync(context);

            // Assert
            result.PoolId.Should().Be(poolId);
            result.Scores.Should().HaveCount(2);
            result.SelectedUpstreamId.Should().BeNull();
            result.HasEligibleUpstream.Should().BeFalse();

            // All scores should be marked as ineligible
            result.Scores.Should().AllSatisfy(s => s.IsEligible.Should().BeFalse());
        }

        [Fact]
        public void RecordOutcomeAsync_WithSuccessfulRequest_UpdatesMetrics()
        {
            // Arrange
            var poolId = Guid.NewGuid().ToString();
            var upstreamId = Guid.NewGuid().ToString();

            // Act
            _loadBalancer.RecordOutcomeAsync(poolId, upstreamId, 150, true).GetAwaiter().GetResult();

            // Assert
            _metricsAggregator.Received(1).Record(upstreamId, 150, true);
        }

        [Fact]
        public void RecordOutcomeAsync_WithFailedRequest_UpdatesMetrics()
        {
            // Arrange
            var poolId = Guid.NewGuid().ToString();
            var upstreamId = Guid.NewGuid().ToString();

            // Act
            _loadBalancer.RecordOutcomeAsync(poolId, upstreamId, 500, false).GetAwaiter().GetResult();

            // Assert
            _metricsAggregator.Received(1).Record(upstreamId, 500, false);
        }

        [Fact]
        public async Task GetEffectiveWeightAsync_WithNoAdaptiveWeight_ReturnsBaseWeight()
        {
            // Arrange
            var upstreamId = Guid.NewGuid().ToString();

            // Act
            var weight = await _loadBalancer.GetEffectiveWeightAsync(upstreamId);

            // Assert
            weight.Should().Be(100); // Base weight of 1 * 100 = 100
        }

        [Fact]
        public async Task RecalibratePoolAsync_ResetsMetricsForAllServers()
        {
            // Arrange
            var poolId = Guid.NewGuid().ToString();
            var server1 = new UpstreamServer
            {
                Id = "server-1",
                Address = "127.0.0.1",
                Port = 8080,
                Weight = 1,
                Status = UpstreamServerStatus.Active,
                IsHealthy = true,
                ActiveConnections = 0
            };
            var server2 = new UpstreamServer
            {
                Id = "server-2",
                Address = "127.0.0.2",
                Port = 8081,
                Weight = 2,
                Status = UpstreamServerStatus.Active,
                IsHealthy = true,
                ActiveConnections = 0
            };

            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = Guid.NewGuid().ToString(),
                Servers = new List<UpstreamServer> { server1, server2 }
            };

            _upstreamManager.GetPoolAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            await _loadBalancer.RecalibratePoolAsync(poolId);

            // Assert
            _metricsAggregator.Received(1).Reset("server-1");
            _metricsAggregator.Received(1).Reset("server-2");
        }

        [Fact]
        public async Task EvaluatePoolAsync_PrioritizesLowerLatencyServers()
        {
            // Arrange
            var poolId = Guid.NewGuid().ToString();
            var context = new UpstreamSelectionContext(poolId);

            // Mock metrics to return different latencies
            var summary1 = new UpstreamMetricsSummary(
                UpstreamId: "server-1",
                SampleCount: 100,
                P50LatencyMs: 50,
                P95LatencyMs: 90,
                P99LatencyMs: 100, // Better (lower) latency
                MeanLatencyMs: 75,
                ErrorRate: 0.0,
                ThroughputRps: 10.0,
                WindowStartUtc: DateTime.UtcNow.AddMinutes(-1),
                WindowEndUtc: DateTime.UtcNow
            );

            var summary2 = new UpstreamMetricsSummary(
                UpstreamId: "server-2",
                SampleCount: 100,
                P50LatencyMs: 150,
                P95LatencyMs: 250,
                P99LatencyMs: 300, // Worse (higher) latency
                MeanLatencyMs: 200,
                ErrorRate: 0.0,
                ThroughputRps: 10.0,
                WindowStartUtc: DateTime.UtcNow.AddMinutes(-1),
                WindowEndUtc: DateTime.UtcNow
            );

            _metricsAggregator.GetSummary("server-1").Returns(summary1);
            _metricsAggregator.GetSummary("server-2").Returns(summary2);

            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = Guid.NewGuid().ToString(),
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = "server-1",
                        Address = "127.0.0.1",
                        Port = 8080,
                        Weight = 1,
                        Status = UpstreamServerStatus.Active,
                        IsHealthy = true,
                        ActiveConnections = 0
                    },
                    new UpstreamServer
                    {
                        Id = "server-2",
                        Address = "127.0.0.2",
                        Port = 8081,
                        Weight = 1,
                        Status = UpstreamServerStatus.Active,
                        IsHealthy = true,
                        ActiveConnections = 0
                    }
                }
            };

            _upstreamManager.GetPoolAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            var result = await _loadBalancer.EvaluatePoolAsync(context);

            // Assert
            result.SelectedUpstreamId.Should().Be("server-1"); // Lower latency should be selected
            result.Scores[0].UpstreamId.Should().Be("server-1");
        }

        [Fact]
        public async Task EvaluatePoolAsync_WithEqualScores_FallsBackToEffectiveWeight()
        {
            // Arrange
            var poolId = Guid.NewGuid().ToString();
            var context = new UpstreamSelectionContext(poolId);

            // Mock metrics to return equal scores
            var summary1 = new UpstreamMetricsSummary(
                UpstreamId: "server-1",
                SampleCount: 100,
                P50LatencyMs: 150,
                P95LatencyMs: 190,
                P99LatencyMs: 200,
                MeanLatencyMs: 175,
                ErrorRate: 0.0,
                ThroughputRps: 10.0,
                WindowStartUtc: DateTime.UtcNow.AddMinutes(-1),
                WindowEndUtc: DateTime.UtcNow
            );

            var summary2 = new UpstreamMetricsSummary(
                UpstreamId: "server-2",
                SampleCount: 100,
                P50LatencyMs: 150,
                P95LatencyMs: 190,
                P99LatencyMs: 200,
                MeanLatencyMs: 175,
                ErrorRate: 0.0,
                ThroughputRps: 10.0,
                WindowStartUtc: DateTime.UtcNow.AddMinutes(-1),
                WindowEndUtc: DateTime.UtcNow
            );

            _metricsAggregator.GetSummary("server-1").Returns(summary1);
            _metricsAggregator.GetSummary("server-2").Returns(summary2);

            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = Guid.NewGuid().ToString(),
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = "server-1",
                        Address = "127.0.0.1",
                        Port = 8080,
                        Weight = 1, // Lower base weight
                        Status = UpstreamServerStatus.Active,
                        IsHealthy = true,
                        ActiveConnections = 0
                    },
                    new UpstreamServer
                    {
                        Id = "server-2",
                        Address = "127.0.0.2",
                        Port = 8081,
                        Weight = 2, // Higher base weight
                        Status = UpstreamServerStatus.Active,
                        IsHealthy = true,
                        ActiveConnections = 0
                    }
                }
            };

            _upstreamManager.GetPoolAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            var result = await _loadBalancer.EvaluatePoolAsync(context);

            // Assert
            result.SelectedUpstreamId.Should().Be("server-2"); // Higher weight should break tie
        }
    }
}
