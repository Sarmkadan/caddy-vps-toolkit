#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.Caching;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Events;
using CaddyVpsToolkit.Results;
using CaddyVpsToolkit.Services;
using CaddyVpsToolkit.Utilities;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Integration
{
    /// <summary>
    /// End-to-end workflow: configure a service, generate a Caddy config, monitor health.
    /// </summary>
    public sealed class ServiceLifecycleIntegrationTests
    {
        private readonly IServiceRepository _serviceRepo;
        private readonly IHealthCheckRepository _healthRepo;
        private readonly ServiceManagementService _serviceManager;
        private readonly HealthMonitoringService _healthMonitor;
        private readonly CaddyConfigurationService _caddyService;

        public ServiceLifecycleIntegrationTests()
        {
            _serviceRepo = Substitute.For<IServiceRepository>();
            _healthRepo = Substitute.For<IHealthCheckRepository>();
            _serviceManager = new ServiceManagementService(_serviceRepo);
            _healthMonitor = new HealthMonitoringService(_healthRepo, _serviceManager);
            _caddyService = new CaddyConfigurationService(_serviceManager);
        }

        [Fact]
        public async Task FullWorkflow_CreateService_ThenGenerateCaddyConfig_ProducesValidOutput()
        {
            // Arrange – service creation
            var service = new ManagedService
            {
                Name = "api-backend",
                Description = "Backend API",
                ExecutablePath = "/usr/bin/dotnet",
                WorkingDirectory = "/opt/api",
                Port = 5000
            };
            _serviceRepo.GetByNameAsync(service.Name).ReturnsNull();
            _serviceRepo.AddAsync(service).Returns(service.Id);

            // Act – create service
            var createdId = await _serviceManager.CreateServiceAsync(service);
            createdId.Should().Be(service.Id);

            // Arrange – Caddy config generation
            var globalConfig = new CaddyConfig { AdminEmail = "ops@example.com" };
            var routes = new List<CaddyRoute>
            {
                new()
                {
                    Domain = "api.example.com",
                    UpstreamUrl = $"http://localhost:{service.Port}",
                    IsActive = true
                }
            };

            // Act – generate Caddyfile
            var caddyfile = await _caddyService.GenerateCaddyfileAsync(globalConfig, routes);

            // Assert
            caddyfile.Should().NotBeNullOrEmpty();
            caddyfile.Should().Contain("api.example.com");
            caddyfile.Should().Contain($"localhost:{service.Port}");
        }

        [Fact]
        public async Task ServiceStatusTransition_StoppedToRunning_UpdatesCorrectly()
        {
            var id = Guid.NewGuid().ToString();
            var service = new ManagedService
            {
                Id = id,
                Name = "web",
                ExecutablePath = "/bin/web",
                WorkingDirectory = "/opt/web",
                Port = 3000,
                Status = ServiceStatus.Stopped
            };
            _serviceRepo.GetByIdAsync(id).Returns(service);
            _serviceRepo.UpdateAsync(service).Returns(true);

            var updated = await _serviceManager.UpdateServiceStatusAsync(id, ServiceStatus.Running);

            updated.Should().BeTrue();
            service.Status.Should().Be(ServiceStatus.Running);
        }

        [Fact]
        public async Task ServiceLifecycle_CreateAndDelete_WorksEndToEnd()
        {
            var service = new ManagedService
            {
                Name = "worker",
                ExecutablePath = "/bin/worker",
                WorkingDirectory = "/opt/worker",
                Port = 9000,
                Status = ServiceStatus.Stopped
            };
            _serviceRepo.GetByNameAsync(service.Name).ReturnsNull();
            _serviceRepo.AddAsync(service).Returns(service.Id);
            _serviceRepo.GetByIdAsync(service.Id).Returns(service);
            _serviceRepo.DeleteAsync(service.Id).Returns(true);

            var id = await _serviceManager.CreateServiceAsync(service);
            var deleted = await _serviceManager.DeleteServiceAsync(id);

            deleted.Should().BeTrue();
            await _serviceRepo.Received(1).DeleteAsync(service.Id);
        }

        [Fact]
        public async Task HealthHistory_GetLast24Hours_ReturnsResults()
        {
            var id = Guid.NewGuid().ToString();
            var expected = Enumerable.Range(0, 5)
                .Select(i => new HealthCheckResult { ServiceId = id, IsHealthy = i % 2 == 0 })
                .ToList();
            _healthRepo.GetRecentAsync(id, 24).Returns(expected);

            var history = await _healthMonitor.GetHealthHistoryAsync(id, 24);

            history.Should().HaveCount(5);
        }
    }

    /// <summary>
    /// Concurrency test: multiple threads operating on shared utilities simultaneously.
    /// </summary>
    public sealed class ConcurrencyIntegrationTests
    {
        [Fact]
        public async Task MemoryCache_ConcurrentReadWrites_AllSucceed()
        {
            var cache = new MemoryCache();
            int threads = 20;
            var tasks = Enumerable.Range(0, threads).Select(i =>
                Task.Run(async () =>
                {
                    await cache.SetAsync($"key-{i}", i);
                    var val = await cache.GetAsync<int>($"key-{i}");
                    val.Should().Be(i);
                })).ToList();

            await Task.WhenAll(tasks);

            cache.GetCacheSize().Should().Be(threads);
        }

        [Fact]
        public async Task EventBus_ConcurrentPublishes_HandlersInvokedCorrectCount()
        {
            var bus = new EventBus();
            int count = 0;
            var handler = Substitute.For<IEventHandler<ServiceCreatedEvent>>();
            handler
                .When(h => h.HandleAsync(Arg.Any<ServiceCreatedEvent>()))
                .Do(_ => Interlocked.Increment(ref count));
            bus.Subscribe(handler);

            int publishes = 50;
            var tasks = Enumerable.Range(0, publishes)
                .Select(i => bus.PublishAsync(new ServiceCreatedEvent { ServiceName = $"svc-{i}", Port = 8000 + i }))
                .ToList();

            await Task.WhenAll(tasks);

            count.Should().Be(publishes);
        }

        [Fact]
        public async Task RetryPolicy_ConcurrentOperations_AllComplete()
        {
            var policy = new ExponentialBackoffRetryPolicy(maxRetries: 2, initialDelayMs: 1);
            int threads = 10;

            var tasks = Enumerable.Range(0, threads)
                .Select(i => policy.ExecuteAsync(() => Task.FromResult(i * 2)))
                .ToList();

            var results = await Task.WhenAll(tasks);

            results.Should().HaveCount(threads);
            results.Should().OnlyContain(r => r >= 0);
        }

        [Fact]
        public void StateMachine_IndependentInstances_DoNotInterfere()
        {
            var machines = Enumerable.Range(0, 10).Select(_ =>
            {
                var sm = new StateMachine<string, string>("idle");
                sm.Configure("idle", "start", "running");
                sm.Configure("running", "stop", "stopped");
                return sm;
            }).ToList();

            // Fire different transitions on different machines
            machines[0].Fire("start");
            machines[1].Fire("start");
            machines[1].Fire("stop");

            machines[0].GetCurrentState().Should().Be("running");
            machines[1].GetCurrentState().Should().Be("stopped");
            machines[2].GetCurrentState().Should().Be("idle"); // untouched
        }
    }

    /// <summary>
    /// Configuration combination tests: verifies different config options work together.
    /// </summary>
    public sealed class ConfigurationIntegrationTests
    {
        private readonly IServiceRepository _serviceRepo = Substitute.For<IServiceRepository>();

        [Fact]
        public async Task CaddyConfig_WithMultipleActiveRoutes_GeneratesAllRouteBlocks()
        {
            var serviceManager = new ServiceManagementService(_serviceRepo);
            var caddyService = new CaddyConfigurationService(serviceManager);
            var globalConfig = new CaddyConfig { AdminEmail = "admin@example.com" };
            var routes = new List<CaddyRoute>
            {
                new() { Domain = "api.example.com", UpstreamUrl = "http://localhost:5000", IsActive = true },
                new() { Domain = "web.example.com", UpstreamUrl = "http://localhost:3000", IsActive = true },
                new() { Domain = "inactive.example.com", UpstreamUrl = "http://localhost:9000", IsActive = false }
            };

            var caddyfile = await caddyService.GenerateCaddyfileAsync(globalConfig, routes);

            caddyfile.Should().Contain("api.example.com");
            caddyfile.Should().Contain("web.example.com");
            caddyfile.Should().NotContain("inactive.example.com");
        }

        [Fact]
        public async Task CaddyConfig_WithNoActiveRoutes_IncludesPlaceholderComment()
        {
            var serviceManager = new ServiceManagementService(_serviceRepo);
            var caddyService = new CaddyConfigurationService(serviceManager);
            var globalConfig = new CaddyConfig { AdminEmail = "admin@example.com" };

            var caddyfile = await caddyService.GenerateCaddyfileAsync(globalConfig, new List<CaddyRoute>());

            caddyfile.Should().Contain("No active routes");
        }

        [Fact]
        public async Task CaddyConfig_WithNullRoutesList_TreatsAsEmpty()
        {
            var serviceManager = new ServiceManagementService(_serviceRepo);
            var caddyService = new CaddyConfigurationService(serviceManager);
            var globalConfig = new CaddyConfig { AdminEmail = "admin@example.com" };

            Func<Task<string>> act = () => caddyService.GenerateCaddyfileAsync(globalConfig, null!);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public void PaginationAndFiltering_CombinedQueryBuilder_ReturnsExpectedPage()
        {
            var services = Enumerable.Range(1, 50)
                .Select(i => new { Name = $"svc-{i:D3}", Port = 8000 + i })
                .ToList();

            var result = new QueryBuilder<dynamic>(services)
                .Where(s => s.Port > 8025)
                .Page(1)
                .PageSize(10)
                .Execute();

            result.TotalCount.Should().Be(25); // ports 8026-8050
            result.Items.Should().HaveCount(10);
        }

        [Fact]
        public async Task CacheAndRetryPolicy_CacheHit_SkipsRetryableOperation()
        {
            var cache = new MemoryCache();
            var policy = new NoRetryPolicy();
            int operationCalls = 0;

            await cache.SetAsync("cached-value", "existing");

            var result = await cache.GetOrSetAsync("cached-value", async () =>
            {
                operationCalls++;
                await Task.CompletedTask;
                return "computed";
            });

            result.Should().Be("existing");
            operationCalls.Should().Be(0);
        }

        [Fact]
        public void TemplateEngine_SystemdUnitTemplate_RendersCorrectly()
        {
            var engine = new TemplateEngine();
            engine.Set("service", "api-backend");
            engine.Set("executable", "/usr/bin/dotnet");
            engine.Set("workdir", "/opt/api");
            engine.Set("user", "www-data");

            const string template =
                "[Unit]\nDescription={{service}}\n\n[Service]\nExecStart={{executable}}\nWorkingDirectory={{workdir}}\nUser={{user}}\n\n[Install]\nWantedBy=multi-user.target";

            var result = engine.Render(template);

            result.Should().Contain("Description=api-backend");
            result.Should().Contain("ExecStart=/usr/bin/dotnet");
            result.Should().Contain("WorkingDirectory=/opt/api");
            result.Should().Contain("User=www-data");
        }
    }

    /// <summary>
    /// Demonstrates the main README use case: adding a service and generating its config files.
    /// </summary>
    public sealed class ReadmeUseCaseIntegrationTests
    {
        [Fact]
        public async Task ReadmeUseCase_AddServiceAndGenerateConfigs_ProducesExpectedOutput()
        {
            // Replicate the primary workflow documented in README:
            // 1. Create a managed service
            // 2. Generate a Caddyfile that reverse-proxies traffic to it
            // 3. Verify the generated configuration contains correct directives

            var repo = Substitute.For<IServiceRepository>();
            var service = new ManagedService
            {
                Name = "my-api",
                Description = "My REST API",
                ExecutablePath = "/usr/bin/dotnet",
                WorkingDirectory = "/opt/my-api",
                Port = 5000
            };
            repo.GetByNameAsync("my-api").ReturnsNull();
            repo.AddAsync(service).Returns(service.Id);

            var serviceManager = new ServiceManagementService(repo);
            var caddyService = new CaddyConfigurationService(serviceManager);

            // Step 1: Create service
            var id = await serviceManager.CreateServiceAsync(service);
            id.Should().NotBeNullOrEmpty();

            // Step 2: Build Caddy route
            var route = new CaddyRoute
            {
                Domain = "api.myapp.com",
                UpstreamUrl = $"http://localhost:{service.Port}",
                IsActive = true,
                EnableHttps = true
            };

            // Step 3: Generate Caddyfile
            var globalConfig = new CaddyConfig { AdminEmail = "admin@myapp.com" };
            var caddyfile = await caddyService.GenerateCaddyfileAsync(globalConfig, new List<CaddyRoute> { route });

            // Assert: config references the domain and upstream
            caddyfile.Should().Contain("api.myapp.com");
            caddyfile.Should().Contain("localhost:5000");
        }

        [Fact]
        public void ReadmeUseCase_PaginateServiceList_ReturnsCorrectPage()
        {
            // Demonstrate service list pagination as shown in the README
            var services = Enumerable.Range(1, 30)
                .Select(i => new ManagedService
                {
                    Name = $"svc-{i:D2}",
                    ExecutablePath = "/bin/app",
                    WorkingDirectory = "/opt",
                    Port = 8000 + i
                }).ToList();

            var page = PaginationHelper.Paginate(services, page: 2, pageSize: 10);

            page.Page.Should().Be(2);
            page.Items.Should().HaveCount(10);
            page.TotalCount.Should().Be(30);
            page.HasNextPage.Should().BeTrue();
            page.HasPreviousPage.Should().BeTrue();
            page.Items[0].Name.Should().Be("svc-11");
        }
    }
}
