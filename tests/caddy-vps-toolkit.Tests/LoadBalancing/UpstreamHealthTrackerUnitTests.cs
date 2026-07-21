#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.LoadBalancing;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CaddyVpsToolkit.Tests.LoadBalancing
{
    /// <summary>
    /// Unit tests for <see cref="UpstreamHealthTracker"/>.
    /// Covers the public API including happy-path scenarios, edge cases, and error conditions.
    /// </summary>
    public sealed class UpstreamHealthTrackerUnitTests
    {
        private readonly IUpstreamPoolRepository _mockRepository;
        private readonly UpstreamHealthTracker _tracker;

        public UpstreamHealthTrackerUnitTests()
        {
            _mockRepository = Substitute.For<IUpstreamPoolRepository>();
            _tracker = new UpstreamHealthTracker(_mockRepository);
        }

        [Fact]
        public void RecordProbeResultAsync_WithNullPoolRepository_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new UpstreamHealthTracker(null!));
        }

        [Fact]
        public async Task RecordProbeResultAsync_WithNonExistentPool_ReturnsWithoutError()
        {
            // Arrange
            const string poolId = "non-existent-pool";
            const string upstreamId = "server-1";
            _mockRepository.GetByIdAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(null));

            // Act
            await _tracker.RecordProbeResultAsync(upstreamId, poolId, true);

            // Assert
            await _mockRepository.Received(1).GetByIdAsync(poolId);
            await _mockRepository.DidNotReceive().UpdateAsync(Arg.Any<UpstreamPool>());
        }

        [Fact]
        public async Task RecordProbeResultAsync_WithNonExistentUpstream_ReturnsWithoutError()
        {
            // Arrange
            const string poolId = "pool-1";
            const string upstreamId = "non-existent-server";
            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = "service-1",
                Servers = new List<UpstreamServer>()
            };
            _mockRepository.GetByIdAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            await _tracker.RecordProbeResultAsync(upstreamId, poolId, true);

            // Assert
            await _mockRepository.Received(1).GetByIdAsync(poolId);
            await _mockRepository.DidNotReceive().UpdateAsync(Arg.Any<UpstreamPool>());
        }

        [Fact]
        public async Task RecordProbeResultAsync_WithSuccessfulProbe_IncrementsSuccessCounter()
        {
            // Arrange
            const string poolId = "pool-1";
            const string upstreamId = "server-1";
            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = "service-1",
                UnhealthyThreshold = 3,
                HealthyThreshold = 2,
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = upstreamId,
                        Address = "127.0.0.1",
                        Port = 8080,
                        Status = UpstreamServerStatus.Unhealthy,
                        ConsecutiveFailures = 5,
                        ConsecutiveSuccesses = 0,
                        IsHealthy = false
                    }
                }
            };
            _mockRepository.GetByIdAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            await _tracker.RecordProbeResultAsync(upstreamId, poolId, true);

            // Assert
            var server = pool.Servers[0];
            server.ConsecutiveSuccesses.Should().Be(1);
            server.ConsecutiveFailures.Should().Be(0);
            server.IsHealthy.Should().BeTrue();
            server.LastCheckedAt.Should().NotBeNull();
            await _mockRepository.Received(1).UpdateAsync(pool);
        }

        [Fact]
        public async Task RecordProbeResultAsync_WithFailedProbe_IncrementsFailureCounter()
        {
            // Arrange
            const string poolId = "pool-1";
            const string upstreamId = "server-1";
            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = "service-1",
                UnhealthyThreshold = 3,
                HealthyThreshold = 2,
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = upstreamId,
                        Address = "127.0.0.1",
                        Port = 8080,
                        Status = UpstreamServerStatus.Active,
                        ConsecutiveFailures = 0,
                        ConsecutiveSuccesses = 5,
                        IsHealthy = true
                    }
                }
            };
            _mockRepository.GetByIdAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            await _tracker.RecordProbeResultAsync(upstreamId, poolId, false);

            // Assert
            var server = pool.Servers[0];
            server.ConsecutiveFailures.Should().Be(1);
            server.ConsecutiveSuccesses.Should().Be(0);
            server.IsHealthy.Should().BeFalse();
            server.LastCheckedAt.Should().NotBeNull();
            await _mockRepository.Received(1).UpdateAsync(pool);
        }

        [Fact]
        public async Task RecordProbeResultAsync_WithConsecutiveFailuresReachingThreshold_MarksServerUnhealthy()
        {
            // Arrange
            const string poolId = "pool-1";
            const string upstreamId = "server-1";
            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = "service-1",
                UnhealthyThreshold = 3,
                HealthyThreshold = 2,
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = upstreamId,
                        Address = "127.0.0.1",
                        Port = 8080,
                        Status = UpstreamServerStatus.Active,
                        ConsecutiveFailures = 2,
                        ConsecutiveSuccesses = 0,
                        IsHealthy = true
                    }
                }
            };
            _mockRepository.GetByIdAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            await _tracker.RecordProbeResultAsync(upstreamId, poolId, false);

            // Assert
            var server = pool.Servers[0];
            server.Status.Should().Be(UpstreamServerStatus.Unhealthy);
            server.ConsecutiveFailures.Should().Be(3);
            await _mockRepository.Received(1).UpdateAsync(pool);
        }

        [Fact]
        public async Task RecordProbeResultAsync_WithConsecutiveSuccessesReachingThreshold_PromotesServerFromUnhealthyToActive()
        {
            // Arrange
            const string poolId = "pool-1";
            const string upstreamId = "server-1";
            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = "service-1",
                UnhealthyThreshold = 3,
                HealthyThreshold = 2,
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = upstreamId,
                        Address = "127.0.0.1",
                        Port = 8080,
                        Status = UpstreamServerStatus.Unhealthy,
                        ConsecutiveFailures = 5,
                        ConsecutiveSuccesses = 1,
                        IsHealthy = false
                    }
                }
            };
            _mockRepository.GetByIdAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            await _tracker.RecordProbeResultAsync(upstreamId, poolId, true);

            // Assert
            var server = pool.Servers[0];
            server.Status.Should().Be(UpstreamServerStatus.Active);
            server.ConsecutiveSuccesses.Should().Be(2);
            server.IsHealthy.Should().BeTrue();
            await _mockRepository.Received(1).UpdateAsync(pool);
        }

        [Fact]
        public async Task RecordProbeResultAsync_WithResponseTimeMs_UpdatesAverageResponseTime()
        {
            // Arrange
            const string poolId = "pool-1";
            const string upstreamId = "server-1";
            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = "service-1",
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = upstreamId,
                        Address = "127.0.0.1",
                        Port = 8080,
                        Status = UpstreamServerStatus.Active,
                        AverageResponseTimeMs = 0
                    }
                }
            };
            _mockRepository.GetByIdAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            await _tracker.RecordProbeResultAsync(upstreamId, poolId, true, responseTimeMs: 100);

            // Assert
            var server = pool.Servers[0];
            server.AverageResponseTimeMs.Should().Be(100);
            await _mockRepository.Received(1).UpdateAsync(pool);
        }

        [Fact]
        public async Task GetSnapshotAsync_WithNonExistentUpstream_ReturnsNull()
        {
            // Arrange
            const string upstreamId = "non-existent-server";
            _mockRepository.GetAllAsync().Returns(Task.FromResult(new List<UpstreamPool>()));

            // Act
            var snapshot = await _tracker.GetSnapshotAsync(upstreamId);

            // Assert
            snapshot.Should().BeNull();
        }

        [Fact]
        public async Task GetSnapshotAsync_WithExistingUpstream_ReturnsCorrectSnapshot()
        {
            // Arrange
            const string upstreamId = "server-1";
            const string poolId = "pool-1";
            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = "service-1",
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = upstreamId,
                        Address = "192.168.1.100",
                        Port = 80,
                        Status = UpstreamServerStatus.Active,
                        IsHealthy = true,
                        ConsecutiveFailures = 0,
                        ConsecutiveSuccesses = 10,
                        AverageResponseTimeMs = 45,
                        ActiveConnections = 5,
                        LastCheckedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
                    }
                }
            };
            _mockRepository.GetAllAsync().Returns(Task.FromResult(new List<UpstreamPool> { pool }));

            // Act
            var snapshot = await _tracker.GetSnapshotAsync(upstreamId);

            // Assert
            snapshot.Should().NotBeNull();
            snapshot!.UpstreamId.Should().Be(upstreamId);
            snapshot.Address.Should().Be("192.168.1.100:80");
            snapshot.IsHealthy.Should().BeTrue();
            snapshot.Status.Should().Be(UpstreamServerStatus.Active);
            snapshot.ConsecutiveFailures.Should().Be(0);
            snapshot.AverageResponseTimeMs.Should().Be(45);
            snapshot.ActiveConnections.Should().Be(5);
            snapshot.LastCheckedAt.Should().Be(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public async Task DrainAsync_WithNonExistentUpstream_DoesNotThrow()
        {
            // Arrange
            const string upstreamId = "non-existent-server";
            var cts = new CancellationTokenSource();
            _mockRepository.GetAllAsync().Returns(Task.FromResult(new List<UpstreamPool>()));

            // Act
            await _tracker.DrainAsync(upstreamId, TimeSpan.FromSeconds(1), cts.Token);

            // Assert
            await _mockRepository.Received(1).GetAllAsync();
            await _mockRepository.DidNotReceive().UpdateAsync(Arg.Any<UpstreamPool>());
        }

        [Fact]
        public async Task DrainAsync_WithExistingUpstream_SetsStatusToDrainingThenDisabled()
        {
            // Arrange
            const string upstreamId = "server-1";
            const string poolId = "pool-1";
            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = "service-1",
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = upstreamId,
                        Address = "127.0.0.1",
                        Port = 8080,
                        Status = UpstreamServerStatus.Active,
                        ActiveConnections = 3
                    }
                }
            };
            _mockRepository.GetAllAsync().Returns(Task.FromResult(new List<UpstreamPool> { pool }));
            _mockRepository.GetByIdAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            await _tracker.DrainAsync(upstreamId, TimeSpan.FromSeconds(1));

            // Assert
            var server = pool.Servers[0];
            server.Status.Should().Be(UpstreamServerStatus.Disabled);
            await _mockRepository.Received(2).UpdateAsync(pool); // Once for Draining, once for Disabled
        }

        [Fact]
        public async Task DrainAsync_WithNoActiveConnections_ImmediatelySetsDisabled()
        {
            // Arrange
            const string upstreamId = "server-1";
            const string poolId = "pool-1";
            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = "service-1",
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = upstreamId,
                        Address = "127.0.0.1",
                        Port = 8080,
                        Status = UpstreamServerStatus.Active,
                        ActiveConnections = 0
                    }
                }
            };
            _mockRepository.GetAllAsync().Returns(Task.FromResult(new List<UpstreamPool> { pool }));
            _mockRepository.GetByIdAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            // Act
            await _tracker.DrainAsync(upstreamId, TimeSpan.FromSeconds(1));

            // Assert
            var server = pool.Servers[0];
            server.Status.Should().Be(UpstreamServerStatus.Disabled);
            await _mockRepository.Received(2).UpdateAsync(pool);
        }

        [Fact]
        public async Task DrainAsync_WithCancellationToken_CancelsWaiting()
        {
            // Arrange
            const string upstreamId = "server-1";
            const string poolId = "pool-1";
            var pool = new UpstreamPool
            {
                Id = poolId,
                Name = "Test Pool",
                ServiceId = "service-1",
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = upstreamId,
                        Address = "127.0.0.1",
                        Port = 8080,
                        Status = UpstreamServerStatus.Active,
                        ActiveConnections = 100 // Large number to ensure waiting
                    }
                }
            };
            _mockRepository.GetAllAsync().Returns(Task.FromResult(new List<UpstreamPool> { pool }));
            _mockRepository.GetByIdAsync(poolId).Returns(Task.FromResult<UpstreamPool?>(pool));

            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));

            // Act & Assert - should throw TaskCanceledException when cancellation occurs
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                _tracker.DrainAsync(upstreamId, TimeSpan.FromSeconds(10), cts.Token));

            // Assert that status was set to Draining before cancellation
            var server = pool.Servers[0];
            server.Status.Should().Be(UpstreamServerStatus.Draining);
            await _mockRepository.Received(1).UpdateAsync(pool);
        }

        [Fact]
        public async Task DrainAsync_WithMultiplePools_FindsAndDrainsCorrectUpstream()
        {
            // Arrange
            const string upstreamId = "server-1";
            const string poolId1 = "pool-1";
            const string poolId2 = "pool-2";
            var pool1 = new UpstreamPool
            {
                Id = poolId1,
                Name = "Pool 1",
                ServiceId = "service-1",
                Servers = new List<UpstreamServer>()
            };
            var pool2 = new UpstreamPool
            {
                Id = poolId2,
                Name = "Pool 2",
                ServiceId = "service-2",
                Servers = new List<UpstreamServer>
                {
                    new UpstreamServer
                    {
                        Id = upstreamId,
                        Address = "127.0.0.1",
                        Port = 8080,
                        Status = UpstreamServerStatus.Active,
                        ActiveConnections = 2
                    }
                }
            };
            _mockRepository.GetAllAsync().Returns(Task.FromResult(new List<UpstreamPool> { pool1, pool2 }));
            _mockRepository.GetByIdAsync(poolId2).Returns(Task.FromResult<UpstreamPool?>(pool2));

            // Act
            await _tracker.DrainAsync(upstreamId, TimeSpan.FromSeconds(1));

            // Assert
            var server = pool2.Servers[0];
            server.Status.Should().Be(UpstreamServerStatus.Disabled);
            await _mockRepository.Received(2).UpdateAsync(pool2);
            await _mockRepository.DidNotReceive().UpdateAsync(pool1);
        }
    }
}
