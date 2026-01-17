#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CaddyVpsToolkit.Tests.Services
{
    public sealed class HealthMonitoringServiceTests
    {
        private readonly IHealthCheckRepository _repositoryMock;
        private readonly IServiceRepository _serviceRepositoryMock;
        private readonly ServiceManagementService _serviceManager;
        private readonly HealthMonitoringService _sut;

        public HealthMonitoringServiceTests()
        {
            _repositoryMock = Substitute.For<IHealthCheckRepository>();
            _serviceRepositoryMock = Substitute.For<IServiceRepository>();
            _serviceManager = new ServiceManagementService(_serviceRepositoryMock);
            _sut = new HealthMonitoringService(_repositoryMock, _serviceManager);
        }

        [Fact]
        public async Task CheckServiceHealthAsync_WhenServiceHasNoHealthCheck_ShouldThrowHealthCheckException()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var service = new ManagedService { Id = id, Name = "Test", ExecutablePath = "/bin", WorkingDirectory = "/", Port = 80, HealthCheck = null };
            _serviceRepositoryMock.GetByIdAsync(id).Returns(service);

            // Act
            Func<Task> act = async () => await _sut.CheckServiceHealthAsync(id);

            // Assert
            await act.Should().ThrowAsync<HealthCheckException>();
        }

        [Fact]
        public async Task GetLatestHealthStatusAsync_ShouldReturnFromRepository()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var expectedResult = new HealthCheckResult { ServiceId = id, IsHealthy = true };
            _repositoryMock.GetLatestAsync(id).Returns(expectedResult);

            // Act
            var result = await _sut.GetLatestHealthStatusAsync(id);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task GetHealthHistoryAsync_WithValidHours_ShouldReturnList()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var list = new List<HealthCheckResult> { new HealthCheckResult() };
            _repositoryMock.GetRecentAsync(id, 24).Returns(list);

            // Act
            var result = await _sut.GetHealthHistoryAsync(id, 24);

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetHealthHistoryAsync_WithInvalidHours_ShouldThrowArgumentException()
        {
            // Act
            Func<Task> act = async () => await _sut.GetHealthHistoryAsync("id", 0);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CleanupOldRecordsAsync_WithValidDays_ShouldReturnTrue()
        {
            // Arrange
            _repositoryMock.DeleteOlderThanAsync(Arg.Any<DateTime>()).Returns(true);

            // Act
            var result = await _sut.CleanupOldRecordsAsync(30);

            // Assert
            result.Should().BeTrue();
            await _repositoryMock.Received(1).DeleteOlderThanAsync(Arg.Any<DateTime>());
        }

        [Fact]
        public async Task CleanupOldRecordsAsync_WithInvalidDays_ShouldThrowArgumentException()
        {
            // Act
            Func<Task> act = async () => await _sut.CleanupOldRecordsAsync(0);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
