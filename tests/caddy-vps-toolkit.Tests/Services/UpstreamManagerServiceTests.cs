using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.LoadBalancing;
using CaddyVpsToolkit.Services;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Services
{
    /// <summary>
    /// Contains unit tests for <see cref="UpstreamManagerService"/>.
    /// </summary>
    public sealed class UpstreamManagerServiceTests
    {
        private readonly IServiceRepository _serviceRepo;
        private readonly UpstreamManagerService _sut;

        /// <summary>
        /// Initializes test dependencies and the system under test.
        /// </summary>
        public UpstreamManagerServiceTests()
        {
            _serviceRepo = Substitute.For<IServiceRepository>();
            var healthRepo = Substitute.For<IHealthCheckRepository>();

            var serviceManager = new ServiceManagementService(_serviceRepo);
            var healthMonitor = new HealthMonitoringService(healthRepo, serviceManager);
            var caddyConfig = new CaddyConfigurationService(serviceManager);

            var options = new LoadBalancingOptions();
            
            _sut = new UpstreamManagerService(serviceManager, healthMonitor, caddyConfig, options);
        }

        /// <summary>
        /// Verifies that registering a null pool throws <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task RegisterPoolAsync_NullPool_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.RegisterPoolAsync(null!));
        }

        /// <summary>
        /// Verifies that registering an invalid pool throws <see cref="ServiceConfigurationException"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task RegisterPoolAsync_InvalidPool_ThrowsServiceConfigurationException()
        {
            var pool = new UpstreamPool { Id = "pool1", Name = "pool1", ServiceId = "svc1" };
            // Simulate validation failure (UpstreamPool.Validate throws if invalid)
            pool.Servers = new List<UpstreamServer>(); 

            await Assert.ThrowsAsync<ServiceConfigurationException>(() => _sut.RegisterPoolAsync(pool));
        }

        /// <summary>
        /// Verifies that registering a pool with a non‑existent service throws <see cref="ServiceNotFoundException"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task RegisterPoolAsync_ServiceNotFound_ThrowsServiceNotFoundException()
        {
            var pool = new UpstreamPool { Id = "pool1", Name = "pool1", ServiceId = "nonexistent", Servers = new List<UpstreamServer> { new UpstreamServer { Address = "127.0.0.1", Port = 80 } } };
            
            _serviceRepo.GetByIdAsync("nonexistent").Returns(Task.FromResult<ManagedService?>(null));

            await Assert.ThrowsAsync<ServiceNotFoundException>(() => _sut.RegisterPoolAsync(pool));
        }

        /// <summary>
        /// Verifies that registering a valid pool returns the pool identifier.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task RegisterPoolAsync_ValidPool_ReturnsPoolId()
        {
            var pool = new UpstreamPool { Id = "pool1", Name = "pool1", ServiceId = "svc1", Servers = new List<UpstreamServer> { new UpstreamServer { Address = "127.0.0.1", Port = 80 } } };
            
            _serviceRepo.GetByIdAsync("svc1").Returns(Task.FromResult<ManagedService?>(new ManagedService { Id = "svc1" }));

            var result = await _sut.RegisterPoolAsync(pool);

            result.Should().Be("pool1");
        }

        /// <summary>
        /// Verifies that retrieving an existing pool returns the correct pool instance.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task GetPoolAsync_ExistingPool_ReturnsPool()
        {
            var pool = new UpstreamPool { Id = "pool1", Name = "pool1", ServiceId = "svc1", Servers = new List<UpstreamServer> { new UpstreamServer { Address = "127.0.0.1", Port = 80 } } };
            _serviceRepo.GetByIdAsync("svc1").Returns(Task.FromResult<ManagedService?>(new ManagedService { Id = "svc1" }));
            await _sut.RegisterPoolAsync(pool);

            var result = await _sut.GetPoolAsync("pool1");

            result.Should().NotBeNull();
            result!.Id.Should().Be("pool1");
        }

        /// <summary>
        /// Verifies that retrieving a non‑existent pool returns <c>null</c>.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task GetPoolAsync_NonexistentPool_ReturnsNull()
        {
            var result = await _sut.GetPoolAsync("unknown");

            result.Should().BeNull();
        }
    }
}
