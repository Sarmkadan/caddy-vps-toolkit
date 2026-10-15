#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Configuration;
using CaddyVpsToolkit.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace CaddyVpsToolkit.Tests.DependencyInjection
{
    /// <summary>
    /// Verifies that <see cref="CaddyVpsToolkitServiceCollectionExtensions.AddCaddyVpsToolkit"/> rejects invalid
    /// <c>CaddyClient</c> and <c>CertificateMonitor</c> settings when the host starts.
    /// </summary>
    [Trait("Category", "Unit")]
    public sealed class CaddyVpsToolkitOptionsValidationTests
    {
        /// <summary>
        /// Builds a host from in-memory settings. The host is not started; each test starts it to trigger validation.
        /// </summary>
        private static IHost BuildHost(IDictionary<string, string?> settings)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            return new HostBuilder()
                .ConfigureServices(services => services.AddCaddyVpsToolkit(configuration))
                .Build();
        }

        /// <summary>
        /// Settings that pass every <c>CertificateMonitor</c> rule, so a test can break one value at a time.
        /// </summary>
        private static Dictionary<string, string?> ValidCertificateMonitorSettings() => new()
        {
            ["CertificateMonitor:WarningThresholdDays"] = "30",
            ["CertificateMonitor:CheckInterval"] = "01:00:00",
            ["CertificateMonitor:Port"] = "443",
            ["CertificateMonitor:Hosts:0"] = "app.example.com",
            ["CertificateMonitor:Hosts:1"] = "api.example.com"
        };

        /// <summary>
        /// Verifies that valid settings start the host and bind the configured values.
        /// </summary>
        [Fact]
        public async Task StartAsync_WithValidSettings_StartsAndBindsValues()
        {
            var settings = ValidCertificateMonitorSettings();
            settings["CaddyClient:AdminEndpoint"] = "https://caddy.internal:2019";
            settings["CaddyClient:Timeout"] = "00:00:45";

            using var host = BuildHost(settings);

            await host.StartAsync();

            var caddy = host.Services.GetRequiredService<IOptions<CaddyClientOptions>>().Value;
            caddy.AdminEndpoint.Should().Be("https://caddy.internal:2019");
            caddy.Timeout.Should().Be(TimeSpan.FromSeconds(45));

            var monitor = host.Services.GetRequiredService<IOptions<CertificateMonitorOptions>>().Value;
            monitor.Hosts.Should().Equal("app.example.com", "api.example.com");

            await host.StopAsync();
        }

        /// <summary>
        /// Verifies that an invalid <c>CaddyClient</c> section fails host startup and names both failing properties.
        /// </summary>
        [Fact]
        public async Task StartAsync_WithInvalidCaddyClientSettings_ThrowsListingFailingProperties()
        {
            var settings = ValidCertificateMonitorSettings();
            settings["CaddyClient:AdminEndpoint"] = "ftp://caddy.internal";
            settings["CaddyClient:Timeout"] = "00:06:00";

            using var host = BuildHost(settings);

            var act = () => host.StartAsync();

            var exception = (await act.Should().ThrowAsync<OptionsValidationException>()).Which;
            exception.Failures.Should().HaveCount(2);
            exception.Failures.Should().Contain(f => f.Contains("AdminEndpoint"));
            exception.Failures.Should().Contain(f => f.Contains("Timeout"));
        }

        /// <summary>
        /// Verifies that a zero <c>CaddyClient</c> timeout is rejected, because the timeout must be greater than zero.
        /// </summary>
        [Fact]
        public async Task StartAsync_WithZeroCaddyClientTimeout_ThrowsListingTimeout()
        {
            var settings = ValidCertificateMonitorSettings();
            settings["CaddyClient:Timeout"] = "00:00:00";

            using var host = BuildHost(settings);

            var act = () => host.StartAsync();

            var exception = (await act.Should().ThrowAsync<OptionsValidationException>()).Which;
            exception.Failures.Should().ContainSingle(f => f.Contains("Timeout"));
        }

        /// <summary>
        /// Verifies that every invalid <c>CertificateMonitor</c> property is listed by name.
        /// </summary>
        [Fact]
        public async Task StartAsync_WithInvalidCertificateMonitorSettings_ThrowsListingFailingProperties()
        {
            var settings = new Dictionary<string, string?>
            {
                ["CertificateMonitor:WarningThresholdDays"] = "0",
                ["CertificateMonitor:CheckInterval"] = "00:00:30",
                ["CertificateMonitor:Port"] = "70000",
                ["CertificateMonitor:Hosts:0"] = "app.example.com",
                ["CertificateMonitor:Hosts:1"] = "APP.example.com"
            };

            using var host = BuildHost(settings);

            var act = () => host.StartAsync();

            var exception = (await act.Should().ThrowAsync<OptionsValidationException>()).Which;
            exception.Failures.Should().HaveCount(4);
            exception.Failures.Should().Contain(f => f.Contains("WarningThresholdDays"));
            exception.Failures.Should().Contain(f => f.Contains("CheckInterval"));
            exception.Failures.Should().Contain(f => f.Contains("Port"));
            exception.Failures.Should().Contain(f => f.Contains("Hosts"));
        }

        /// <summary>
        /// Verifies that a <c>CertificateMonitor</c> section with no hosts fails host startup.
        /// </summary>
        [Fact]
        public async Task StartAsync_WithNoCertificateMonitorHosts_ThrowsListingHosts()
        {
            var settings = new Dictionary<string, string?>
            {
                ["CertificateMonitor:WarningThresholdDays"] = "30"
            };

            using var host = BuildHost(settings);

            var act = () => host.StartAsync();

            var exception = (await act.Should().ThrowAsync<OptionsValidationException>()).Which;
            exception.Failures.Should().ContainSingle(f => f.Contains("Hosts"));
        }

        /// <summary>
        /// Verifies that the <c>CertificateMonitor</c> check interval must be at least one minute.
        /// </summary>
        [Fact]
        public async Task StartAsync_WithCheckIntervalBelowOneMinute_ThrowsListingCheckInterval()
        {
            var settings = ValidCertificateMonitorSettings();
            settings["CertificateMonitor:CheckInterval"] = "00:00:59";

            using var host = BuildHost(settings);

            var act = () => host.StartAsync();

            var exception = (await act.Should().ThrowAsync<OptionsValidationException>()).Which;
            exception.Failures.Should().ContainSingle(f => f.Contains("CheckInterval"));
        }
    }
}
