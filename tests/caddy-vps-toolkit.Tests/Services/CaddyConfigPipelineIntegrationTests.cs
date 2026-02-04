#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
    /// Integration tests for the full Caddy configuration generation pipeline —
    /// from ManagedService + CaddyRoute inputs to final Caddyfile output.
    /// </summary>
    public sealed class CaddyConfigPipelineIntegrationTests
    {
        private readonly IServiceRepository _serviceRepositoryMock;
        private readonly ServiceManagementService _serviceManager;
        private readonly CaddyConfigurationService _sut;

        private static CaddyConfig DefaultConfig => new() { AdminEmail = "admin@example.com" };

        public CaddyConfigPipelineIntegrationTests()
        {
            _serviceRepositoryMock = Substitute.For<IServiceRepository>();
            _serviceManager = new ServiceManagementService(_serviceRepositoryMock);
            _sut = new CaddyConfigurationService(_serviceManager);
        }

        // ─── GenerateCaddyfileAsync — route combinations ───────────────────────

        [Fact]
        public async Task GenerateCaddyfileAsync_WithMultipleActiveRoutes_IncludesAllRoutes()
        {
            var routes = new List<CaddyRoute>
            {
                new() { Domain = "api.example.com", UpstreamUrl = "http://localhost:5001", IsActive = true },
                new() { Domain = "www.example.com", UpstreamUrl = "http://localhost:5002", IsActive = true },
            };

            var result = await _sut.GenerateCaddyfileAsync(DefaultConfig, routes);

            result.Should().Contain("api.example.com");
            result.Should().Contain("www.example.com");
            result.Should().Contain("http://localhost:5001");
            result.Should().Contain("http://localhost:5002");
        }

        [Fact]
        public async Task GenerateCaddyfileAsync_InactiveRoutesAreExcluded()
        {
            var routes = new List<CaddyRoute>
            {
                new() { Domain = "active.example.com",   UpstreamUrl = "http://localhost:5001", IsActive = true },
                new() { Domain = "inactive.example.com", UpstreamUrl = "http://localhost:5002", IsActive = false },
            };

            var result = await _sut.GenerateCaddyfileAsync(DefaultConfig, routes);

            result.Should().Contain("active.example.com");
            result.Should().NotContain("inactive.example.com");
        }

        [Fact]
        public async Task GenerateCaddyfileAsync_WithNullRoutesList_ReturnsFallbackComment()
        {
            var result = await _sut.GenerateCaddyfileAsync(DefaultConfig, null!);

            result.Should().Contain("# No active routes configured");
        }

        [Fact]
        public async Task GenerateCaddyfileAsync_WithEmptyRoutesList_ReturnsFallbackComment()
        {
            var result = await _sut.GenerateCaddyfileAsync(DefaultConfig, new List<CaddyRoute>());

            result.Should().Contain("# No active routes configured");
        }

        [Fact]
        public async Task GenerateCaddyfileAsync_GlobalsArePresentInOutput()
        {
            var result = await _sut.GenerateCaddyfileAsync(DefaultConfig, new List<CaddyRoute>());

            result.Should().Contain("admin localhost:2019");
            result.Should().Contain("http_port 80");
            result.Should().Contain("https_port 443");
        }

        // ─── GenerateRouteBlock — feature coverage ─────────────────────────────

        [Fact]
        public void GenerateRouteBlock_RootPath_GeneratesSimpleReverseProxy()
        {
            var route = new CaddyRoute
            {
                Domain       = "app.example.com",
                UpstreamUrl  = "http://localhost:3000",
                Path         = "/",
                IsActive     = true,
            };

            var block = _sut.GenerateRouteBlock(route);

            block.Should().Contain("app.example.com {");
            block.Should().Contain("reverse_proxy http://localhost:3000");
            block.Should().NotContain("@");   // no named matcher for root path
        }

        [Fact]
        public void GenerateRouteBlock_NonRootPath_GeneratesNamedMatcher()
        {
            var route = new CaddyRoute
            {
                ServiceId    = "my-service",
                Domain       = "app.example.com",
                UpstreamUrl  = "http://localhost:3000",
                Path         = "/api",
                IsActive     = true,
            };

            var block = _sut.GenerateRouteBlock(route);

            block.Should().Contain("@my_service path /api*");
            block.Should().Contain("handle @my_service {");
        }

        [Fact]
        public void GenerateRouteBlock_NonRootPath_HyphensReplacedInMatcherName()
        {
            var route = new CaddyRoute
            {
                ServiceId   = "my-cool-service",
                Domain      = "app.example.com",
                UpstreamUrl = "http://localhost:4000",
                Path        = "/v1",
                IsActive    = true,
            };

            var block = _sut.GenerateRouteBlock(route);

            // Caddy matcher identifiers must not contain hyphens
            block.Should().NotMatchRegex(@"@my-cool-service");
            block.Should().Contain("@my_cool_service");
        }

        [Fact]
        public void GenerateRouteBlock_WithCustomHeaders_EmitsHeaderDirectives()
        {
            var route = new CaddyRoute
            {
                Domain        = "app.example.com",
                UpstreamUrl   = "http://localhost:3000",
                CustomHeaders = new Dictionary<string, string>
                {
                    { "X-Real-IP",    "{remote_host}" },
                    { "X-Request-ID", "{uuid}" },
                },
            };

            var block = _sut.GenerateRouteBlock(route);

            block.Should().Contain("header +X-Real-IP {remote_host}");
            block.Should().Contain("header +X-Request-ID {uuid}");
        }

        [Fact]
        public void GenerateRouteBlock_WithRateLimitRule_EmitsRateLimitDirective()
        {
            var route = new CaddyRoute
            {
                Domain        = "api.example.com",
                UpstreamUrl   = "http://localhost:8080",
                RateLimitRule = "10r/s",
            };

            var block = _sut.GenerateRouteBlock(route);

            block.Should().Contain("rate_limit 10r/s");
        }

        [Fact]
        public void GenerateRouteBlock_WithBasicAuth_EmitsBasicauthBlock()
        {
            var route = new CaddyRoute
            {
                Domain                = "secure.example.com",
                UpstreamUrl           = "http://localhost:9000",
                BasicAuthEnabled      = true,
                BasicAuthUsername     = "admin",
                BasicAuthPasswordHash = "$2a$14$hashed",
            };

            var block = _sut.GenerateRouteBlock(route);

            block.Should().Contain("basicauth {");
            block.Should().Contain("admin $2a$14$hashed");
        }

        [Fact]
        public void GenerateRouteBlock_WithTlsDnsProvider_EmitsTlsDnsBlock()
        {
            var route = new CaddyRoute
            {
                Domain          = "*.example.com",
                UpstreamUrl     = "http://localhost:6000",
                TlsDnsProvider  = "cloudflare",
            };

            var block = _sut.GenerateRouteBlock(route);

            block.Should().Contain("tls {");
            block.Should().Contain("dns cloudflare");
        }

        [Fact]
        public void GenerateRouteBlock_WithStripPath_EmitsStripPrefixDirective()
        {
            var route = new CaddyRoute
            {
                Domain      = "app.example.com",
                UpstreamUrl = "http://localhost:3000",
                Path        = "/app",
                StripPath   = true,
            };

            var block = _sut.GenerateRouteBlock(route);

            block.Should().Contain("uri strip_prefix /app");
        }

        // ─── ValidateCaddyfileAsync ────────────────────────────────────────────

        [Fact]
        public async Task ValidateCaddyfileAsync_WellFormedContent_ReturnsTrue()
        {
            const string content = """
                app.example.com {
                    reverse_proxy http://localhost:3000
                }
                """;

            var result = await _sut.ValidateCaddyfileAsync(content);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateCaddyfileAsync_UnmatchedOpenBrace_ThrowsCaddyOperationException()
        {
            const string content = "app.example.com {\n    reverse_proxy http://localhost:3000\n";

            Func<Task> act = async () => await _sut.ValidateCaddyfileAsync(content);

            await act.Should().ThrowAsync<CaddyOperationException>()
                .WithMessage("*unmatched opening braces*");
        }

        [Fact]
        public async Task ValidateCaddyfileAsync_UnmatchedClosingBrace_ThrowsCaddyOperationException()
        {
            const string content = "app.example.com }\n    reverse_proxy http://localhost:3000\n";

            Func<Task> act = async () => await _sut.ValidateCaddyfileAsync(content);

            await act.Should().ThrowAsync<CaddyOperationException>()
                .WithMessage("*unmatched closing brace*");
        }

        // ─── GenerateCaddyJsonAsync ────────────────────────────────────────────

        [Fact]
        public void GenerateCaddyJsonAsync_WithActiveRoutes_ProducesValidJsonStructure()
        {
            var routes = new List<CaddyRoute>
            {
                new() { Domain = "api.example.com", UpstreamUrl = "http://localhost:5001", IsActive = true },
            };

            var json = _sut.GenerateCaddyJsonAsync(DefaultConfig, routes);

            json.Should().NotBeNullOrWhiteSpace();
            var doc = JsonDocument.Parse(json);
            doc.RootElement.TryGetProperty("apps", out var apps).Should().BeTrue();
            apps.TryGetProperty("http", out _).Should().BeTrue();
        }

        // ─── GenerateRouteForService ───────────────────────────────────────────

        [Fact]
        public void GenerateRouteForService_PopulatesAllExpectedFields()
        {
            var service = new ManagedService
            {
                Id          = Guid.NewGuid().ToString(),
                Name        = "my-api",
                Description = "Test API",
                ExecutablePath   = "/usr/bin/api",
                WorkingDirectory = "/opt/api",
                HostBinding = "127.0.0.1",
                Port        = 5000,
            };

            var route = _sut.GenerateRouteForService(service, "api.example.com", "cloudflare");

            route.Domain.Should().Be("api.example.com");
            route.UpstreamUrl.Should().Be("http://127.0.0.1:5000");
            route.EnableHttps.Should().BeTrue();
            route.AutoRedirectHttp.Should().BeTrue();
            route.PreserveHostHeader.Should().BeTrue();
            route.TlsDnsProvider.Should().Be("cloudflare");
            route.ServiceId.Should().Be(service.Id);
        }

        // ─── End-to-end pipeline ───────────────────────────────────────────────

        [Fact]
        public async Task FullPipeline_ServiceToValidCaddyfile_ProducesBalancedBraces()
        {
            var service = new ManagedService
            {
                Id               = Guid.NewGuid().ToString(),
                Name             = "web-app",
                Description      = "Web application",
                ExecutablePath   = "/usr/bin/webapp",
                WorkingDirectory = "/opt/webapp",
                HostBinding      = "127.0.0.1",
                Port             = 8080,
            };

            var route  = _sut.GenerateRouteForService(service, "www.example.com");
            var config = DefaultConfig;
            var output = await _sut.GenerateCaddyfileAsync(config, new List<CaddyRoute> { route });

            // The generated Caddyfile must pass syntax validation
            var isValid = await _sut.ValidateCaddyfileAsync(output);
            isValid.Should().BeTrue();
            output.Should().Contain("www.example.com");
            output.Should().Contain("http://127.0.0.1:8080");
        }
    }
}
