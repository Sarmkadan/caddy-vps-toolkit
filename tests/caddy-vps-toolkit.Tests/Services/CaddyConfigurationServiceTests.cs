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
    public class CaddyConfigurationServiceTests
    {
        private readonly IServiceRepository _serviceRepositoryMock;
        private readonly ServiceManagementService _serviceManager;
        private readonly CaddyConfigurationService _sut;

        public CaddyConfigurationServiceTests()
        {
            _serviceRepositoryMock = Substitute.For<IServiceRepository>();
            _serviceManager = new ServiceManagementService(_serviceRepositoryMock);
            _sut = new CaddyConfigurationService(_serviceManager);
        }

        [Fact]
        public async Task GenerateCaddyfileAsync_WithNullGlobalConfig_ShouldThrowArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await _sut.GenerateCaddyfileAsync(null!, new List<CaddyRoute>());

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GenerateCaddyfileAsync_WithValidInputs_ShouldReturnString()
        {
            // Arrange
            var config = new CaddyConfig { AdminEmail = "admin@example.com" };
            var routes = new List<CaddyRoute>
            {
                new CaddyRoute { Domain = "test.com", UpstreamUrl = "http://localhost:8080", IsActive = true }
            };

            // Act
            var result = await _sut.GenerateCaddyfileAsync(config, routes);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().Contain("test.com {");
            result.Should().Contain("reverse_proxy http://localhost:8080");
        }

        [Fact]
        public void GenerateRouteBlock_WithNullRoute_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => _sut.GenerateRouteBlock(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GenerateRouteForService_WithValidService_ShouldCreateRoute()
        {
            // Arrange
            var service = new ManagedService { Id = Guid.NewGuid().ToString(), HostBinding = "127.0.0.1", Port = 5000 };
            
            // Act
            var result = _sut.GenerateRouteForService(service, "app.test.com");

            // Assert
            result.Should().NotBeNull();
            result.Domain.Should().Be("app.test.com");
            result.UpstreamUrl.Should().Be("http://127.0.0.1:5000");
        }

        [Fact]
        public void GenerateRouteForService_WithNullService_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => _sut.GenerateRouteForService(null!, "domain.com");

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task ValidateCaddyfileAsync_WithEmptyContent_ShouldThrowArgumentException()
        {
            // Act
            Func<Task> act = async () => await _sut.ValidateCaddyfileAsync(string.Empty);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
