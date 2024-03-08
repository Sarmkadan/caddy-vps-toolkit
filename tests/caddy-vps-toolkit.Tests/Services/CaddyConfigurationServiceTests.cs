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
    /// <summary>
    /// Tests for the <see cref="CaddyConfigurationService"/> class.
    /// </summary>
    public sealed class CaddyConfigurationServiceTests
    {
        private readonly IServiceRepository _serviceRepositoryMock;
        private readonly ServiceManagementService _serviceManager;
        private readonly CaddyConfigurationService _sut;

        /// <summary>
        /// Initializes a new instance of the <see cref="CaddyConfigurationServiceTests"/> class.
        /// Sets up the necessary mocks and service instances for testing.
        /// </summary>
        public CaddyConfigurationServiceTests()
        {
            _serviceRepositoryMock = Substitute.For<IServiceRepository>();
            _serviceManager = new ServiceManagementService(_serviceRepositoryMock);
            _sut = new CaddyConfigurationService(_serviceManager);
        }

        /// <summary>
        /// Verifies that <see cref="CaddyConfigurationService.GenerateCaddyfileAsync(CaddyConfig, IEnumerable{CaddyRoute})"/>
        /// throws an <see cref="ArgumentNullException"/> when the global configuration is null.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GenerateCaddyfileAsync_WithNullGlobalConfig_ShouldThrowArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await _sut.GenerateCaddyfileAsync(null!, new List<CaddyRoute>());

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Verifies that <see cref="CaddyConfigurationService.GenerateCaddyfileAsync(CaddyConfig, IEnumerable{CaddyRoute})"/>
        /// returns a non-empty Caddyfile string containing the expected route block and reverse proxy configuration
        /// when provided with valid inputs.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Verifies that <see cref="CaddyConfigurationService.GenerateRouteBlock(CaddyRoute)"/>
        /// throws an <see cref="ArgumentNullException"/> when the route argument is null.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public void GenerateRouteBlock_WithNullRoute_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => _sut.GenerateRouteBlock(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Verifies that <see cref="CaddyConfigurationService.GenerateRouteForService(ManagedService, string)"/>
        /// correctly creates a <see cref="CaddyRoute"/> with the expected domain and upstream URL
        /// when provided with a valid service and domain.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Verifies that <see cref="CaddyConfigurationService.GenerateRouteForService(ManagedService, string)"/>
        /// throws an <see cref="ArgumentNullException"/> when the service argument is null.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public void GenerateRouteForService_WithNullService_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => _sut.GenerateRouteForService(null!, "domain.com");

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Verifies that <see cref="CaddyConfigurationService.ValidateCaddyfileAsync(string)"/>
        /// throws an <see cref="ArgumentException"/> when the provided Caddyfile content is empty.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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
