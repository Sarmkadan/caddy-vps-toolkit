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
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Services
{
    /// <summary>
    /// Unit tests for the <see cref="ServiceManagementService"/> class.
    /// </summary>
    public sealed class ServiceManagementServiceTests
    {
        private readonly IServiceRepository _repositoryMock;
        private readonly ServiceManagementService _sut;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceManagementServiceTests"/> class.
        /// </summary>
        public ServiceManagementServiceTests()
        {
            _repositoryMock = Substitute.For<IServiceRepository>();
            _sut = new ServiceManagementService(_repositoryMock);
        }

        [Fact]
        /// <summary>
        /// Verifies that <see cref="ServiceManagementService.CreateServiceAsync"/> returns the service ID when provided with a valid service.
        /// </summary>
        public async Task CreateServiceAsync_WithValidService_ShouldReturnId()
        {
            // Arrange
            var service = new ManagedService
            {
                Name = "TestService",
                ExecutablePath = "/usr/bin/test",
                WorkingDirectory = "/tmp",
                Port = 8080
            };
            _repositoryMock.GetByNameAsync(service.Name).ReturnsNull();
            _repositoryMock.AddAsync(service).Returns(service.Id);

            // Act
            var result = await _sut.CreateServiceAsync(service);

            // Assert
            result.Should().Be(service.Id);
            await _repositoryMock.Received(1).AddAsync(service);
        }

        [Fact]
        /// <summary>
        /// Verifies that <see cref="ServiceManagementService.CreateServiceAsync"/> throws an <see cref="ArgumentNullException"/> when the service is null.
        /// </summary>
        public async Task CreateServiceAsync_WithNullService_ShouldThrowArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await _sut.CreateServiceAsync(null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        /// <summary>
        /// Verifies that <see cref="ServiceManagementService.CreateServiceAsync"/> throws a <see cref="ServiceConfigurationException"/> when a service with the same name already exists.
        /// </summary>
        public async Task CreateServiceAsync_WithExistingName_ShouldThrowServiceConfigurationException()
        {
            // Arrange
            var service = new ManagedService { Name = "Existing", ExecutablePath = "/bin", WorkingDirectory = "/", Port = 80 };
            _repositoryMock.GetByNameAsync(service.Name).Returns(service);

            // Act
            Func<Task> act = async () => await _sut.CreateServiceAsync(service);

            // Assert
            await act.Should().ThrowAsync<ServiceConfigurationException>();
        }

        [Fact]
        /// <summary>
        /// Verifies that <see cref="ServiceManagementService.GetServiceAsync"/> returns the expected service when provided with a valid ID.
        /// </summary>
        public async Task GetServiceAsync_WithValidId_ShouldReturnService()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var service = new ManagedService { Id = id, Name = "Test", ExecutablePath = "/bin", WorkingDirectory = "/", Port = 80 };
            _repositoryMock.GetByIdAsync(id).Returns(service);

            // Act
            var result = await _sut.GetServiceAsync(id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(id);
        }

        [Fact]
        /// <summary>
        /// Verifies that <see cref="ServiceManagementService.DeleteServiceAsync"/> throws a <see cref="ServiceConfigurationException"/> when attempting to delete a running service.
        /// </summary>
        public async Task DeleteServiceAsync_WhenServiceIsRunning_ShouldThrowServiceConfigurationException()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var service = new ManagedService { Id = id, Status = ServiceStatus.Running, Name = "Run", ExecutablePath = "/bin", WorkingDirectory = "/", Port = 80 };
            _repositoryMock.GetByIdAsync(id).Returns(service);

            // Act
            Func<Task> act = async () => await _sut.DeleteServiceAsync(id);

            // Assert
            await act.Should().ThrowAsync<ServiceConfigurationException>();
            await _repositoryMock.DidNotReceive().DeleteAsync(Arg.Any<string>());
        }

        [Fact]
        /// <summary>
        /// Verifies that <see cref="ServiceManagementService.UpdateServiceStatusAsync"/> successfully updates the service status and returns true.
        /// </summary>
        public async Task UpdateServiceStatusAsync_WithValidId_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var service = new ManagedService { Id = id, Status = ServiceStatus.Stopped, Name = "App", ExecutablePath = "/bin", WorkingDirectory = "/", Port = 80 };
            _repositoryMock.GetByIdAsync(id).Returns(service);
            _repositoryMock.UpdateAsync(service).Returns(true);

            // Act
            var result = await _sut.UpdateServiceStatusAsync(id, ServiceStatus.Running);

            // Assert
            result.Should().BeTrue();
            service.Status.Should().Be(ServiceStatus.Running);
            await _repositoryMock.Received(1).UpdateAsync(service);
        }
    }
}
