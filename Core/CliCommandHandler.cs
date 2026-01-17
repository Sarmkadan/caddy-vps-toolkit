#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Services;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Core
{
    /// <summary>
    /// CLI command handler for the application
    /// </summary>
    public sealed class CliCommandHandler
    {
        private readonly ServiceManagementService _serviceManager;
        private readonly HealthMonitoringService _healthMonitor;
        private readonly CaddyConfigurationService _caddyConfig;
        private readonly SystemdUnitService _systemdUnit;
        private readonly ConfigurationService _config;

        public CliCommandHandler(
            ServiceManagementService serviceManager,
            HealthMonitoringService healthMonitor,
            CaddyConfigurationService caddyConfig,
            SystemdUnitService systemdUnit,
            ConfigurationService config)
        {
            _serviceManager = serviceManager;
            _healthMonitor = healthMonitor;
            _caddyConfig = caddyConfig;
            _systemdUnit = systemdUnit;
            _config = config;
        }

        /// <summary>
        /// Handle CLI command
        /// </summary>
        public async Task HandleAsync(string[] args)
        {
            if (args is null || args.Length == 0)
            {
                PrintUsage();
                return;
            }

            var command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "version":
                        PrintVersion();
                        break;

                    case "help":
                        PrintHelp();
                        break;

                    case "list-services":
                        await ListServicesAsync();
                        break;

                    case "service-status":
                        if (args.Length < 2)
                            throw new ArgumentException("Service ID required");
                        await GetServiceStatusAsync(args[1]);
                        break;

                    case "health-check":
                        if (args.Length < 2)
                            throw new ArgumentException("Service ID required");
                        await PerformHealthCheckAsync(args[1]);
                        break;

                    case "health-summary":
                        await GetHealthSummaryAsync();
                        break;

                    case "config-get":
                        if (args.Length < 2)
                            throw new ArgumentException("Configuration key required");
                        await GetConfigAsync(args[1]);
                        break;

                    case "config-set":
                        if (args.Length < 3)
                            throw new ArgumentException("Configuration key and value required");
                        await SetConfigAsync(args[1], args[2]);
                        break;

                    default:
                        Console.Error.WriteLine($"Unknown command: {command}");
                        PrintUsage();
                        Environment.Exit(1);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] {ex.Message}");
                Environment.Exit(1);
            }
        }

        private async Task ListServicesAsync()
        {
            var services = await _serviceManager.GetAllServicesAsync();
            Console.WriteLine($"Total Services: {services.Count}");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"{"ID",-36} {"Name",-20} {"Type",-15} {"Status",-12} {"Port",-6}");
            Console.WriteLine(new string('-', 80));

            foreach (var service in services)
            {
                Console.WriteLine($"{service.Id,-36} {service.Name,-20} {service.Type,-15} {service.Status,-12} {service.Port,-6}");
            }
        }

        private async Task GetServiceStatusAsync(string serviceId)
        {
            var service = await _serviceManager.GetServiceAsync(serviceId);
            Console.WriteLine($"Service: {service.Name}");
            Console.WriteLine($"Status: {service.Status}");
            Console.WriteLine($"Type: {service.Type}");
            Console.WriteLine($"Executable: {service.ExecutablePath}");
            Console.WriteLine($"Port: {service.Port}");
            Console.WriteLine($"Auto-start: {service.AutoStart}");
            Console.WriteLine($"Enabled: {service.IsEnabled}");
            Console.WriteLine($"Created: {service.CreatedAt:O}");
            Console.WriteLine($"Updated: {service.UpdatedAt:O}");
        }

        private async Task PerformHealthCheckAsync(string serviceId)
        {
            try
            {
                var result = await _healthMonitor.CheckServiceHealthAsync(serviceId);
                Console.WriteLine($"Health Check Result:");
                Console.WriteLine($"Status: {(result.IsHealthy ? "HEALTHY" : "UNHEALTHY")}");
                Console.WriteLine($"Response Time: {result.ResponseTimeMs}ms");
                Console.WriteLine($"HTTP Status: {result.HttpStatusCode}");
                Console.WriteLine($"Checked At: {result.CheckedAt:O}");

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                    Console.WriteLine($"Error: {result.ErrorMessage}");
            }
            catch (HealthCheckException ex)
            {
                Console.Error.WriteLine($"Health check failed: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private async Task GetHealthSummaryAsync()
        {
            var summary = await _healthMonitor.GetHealthSummaryAsync();
            Console.WriteLine($"Health Summary:");
            Console.WriteLine($"Total Services: {summary.TotalServices}");
            Console.WriteLine($"Healthy: {summary.HealthyServices}");
            Console.WriteLine($"Unhealthy: {summary.UnhealthyServices}");
            Console.WriteLine($"Unchecked: {summary.UncheckedServices}");
            Console.WriteLine($"Disabled: {summary.DisabledServices}");
            Console.WriteLine($"Health Percentage: {summary.HealthPercentage:F2}%");
        }

        private async Task GetConfigAsync(string key)
        {
            var value = await _config.GetValueAsync(key);
            if (value is not null)
                Console.WriteLine($"{key}={value}");
            else
                Console.WriteLine($"Configuration key '{key}' not found");
        }

        private async Task SetConfigAsync(string key, string value)
        {
            await _config.SetValueAsync(key, value);
            Console.WriteLine($"Configuration updated: {key}={value}");
        }

        private void PrintVersion()
        {
            Console.WriteLine($"{AppConstants.AppName} v{AppConstants.AppVersion}");
            Console.WriteLine($"Author: Vladyslav Zaiets");
            Console.WriteLine($"Repository: https://github.com/vladyslav-zaiets/caddy-vps-toolkit");
        }

        private void PrintUsage()
        {
            Console.WriteLine($"{AppConstants.AppName} v{AppConstants.AppVersion}");
            Console.WriteLine("\nUsage: caddy-vps-toolkit <command> [options]");
            Console.WriteLine("\nCommands:");
            Console.WriteLine("  version              Show version information");
            Console.WriteLine("  help                 Show help information");
            Console.WriteLine("  list-services        List all services");
            Console.WriteLine("  service-status <id>  Get service status");
            Console.WriteLine("  health-check <id>    Perform health check");
            Console.WriteLine("  health-summary       Get overall health summary");
            Console.WriteLine("  config-get <key>     Get configuration value");
            Console.WriteLine("  config-set <k> <v>   Set configuration value");
        }

        private void PrintHelp()
        {
            Console.WriteLine("Caddy VPS Toolkit - Help");
            Console.WriteLine("\nThis tool helps manage multiple services on a single VPS using Caddy and systemd.");
            Console.WriteLine("\nFeatures:");
            Console.WriteLine("  - Service management (create, update, delete)");
            Console.WriteLine("  - Health monitoring with HTTP/TCP checks");
            Console.WriteLine("  - Caddy reverse proxy configuration");
            Console.WriteLine("  - Systemd unit management");
            Console.WriteLine("  - Configuration management");
            Console.WriteLine("\nFor more information, visit: https://github.com/vladyslav-zaiets/caddy-vps-toolkit");
        }
    }
}
