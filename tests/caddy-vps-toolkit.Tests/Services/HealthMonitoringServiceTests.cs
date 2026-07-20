#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Configuration;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CaddyVpsToolkit.Tests.Services
{
    /// <summary>
    /// Tests for the HealthMonitoringService class.
    /// </summary>
    public sealed class HealthMonitoringServiceTests
    {
        private readonly IHealthCheckRepository _repositoryMock;
        private readonly IServiceRepository _serviceRepositoryMock;
        private readonly ServiceManagementService _serviceManager;
        private readonly HealthMonitoringService _sut;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthMonitoringServiceTests"/> class.
        /// </summary>
        public HealthMonitoringServiceTests()
        {
            _repositoryMock = Substitute.For<IHealthCheckRepository>();
            _serviceRepositoryMock = Substitute.For<IServiceRepository>();
            _serviceManager = new ServiceManagementService(_serviceRepositoryMock);
            var upstreamOptions = new UpstreamManagementOptions();
        _sut = new HealthMonitoringService(_repositoryMock, _serviceManager, upstreamOptions);
        }

        /// <summary>
        /// Verifies that CheckServiceHealthAsync throws a HealthCheckException when the service has no health check.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
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

        /// <summary>
        /// Verifies that GetLatestHealthStatusAsync returns the latest health status from the repository.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
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

        /// <summary>
        /// Verifies that GetHealthHistoryAsync returns a list of health check results for the specified service and time range.
        /// </summary>
        /// <param name="hours">The number of hours to retrieve health check results for.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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

        /// <summary>
        /// Verifies that GetHealthHistoryAsync throws an ArgumentException when the number of hours is invalid.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task GetHealthHistoryAsync_WithInvalidHours_ShouldThrowArgumentException()
        {
            // Act
            Func<Task> act = async () => await _sut.GetHealthHistoryAsync("id", 0);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        /// <summary>
        /// Verifies that CleanupOldRecordsAsync removes old health check records and returns true.
        /// </summary>
        /// <param name="days">The number of days to keep health check records.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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

        /// <summary>
        /// Verifies that CleanupOldRecordsAsync throws an ArgumentException when the number of days is invalid.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
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
