#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Services;

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
        private readonly IBackupService _backup;
        private readonly ILogAggregationService _logAggregation;
        private readonly ISslCertificateMonitoringService _sslMonitor;

        public CliCommandHandler(
            ServiceManagementService serviceManager,
            HealthMonitoringService healthMonitor,
            CaddyConfigurationService caddyConfig,
            SystemdUnitService systemdUnit,
            ConfigurationService config,
            IBackupService backup,
            ILogAggregationService logAggregation,
            ISslCertificateMonitoringService sslMonitor)
        {
            _serviceManager = serviceManager;
            _healthMonitor = healthMonitor;
            _caddyConfig = caddyConfig;
            _systemdUnit = systemdUnit;
            _config = config;
            _backup = backup;
            _logAggregation = logAggregation;
            _sslMonitor = sslMonitor;
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
                        await ListServicesAsync(args);
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

                    case "backup-create":
                        await BackupCreateAsync(args);
                        break;

                    case "backup-restore":
                        if (args.Length < 2)
                            throw new ArgumentException("Backup file path required");
                        await BackupRestoreAsync(args[1]);
                        break;

                    case "backup-list":
                        await BackupListAsync(args);
                        break;

                    case "logs-view":
                        await LogsViewAsync(args);
                        break;

                    case "ssl-check":
                        await SslCheckAsync(args);
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

        private async Task ListServicesAsync(string[] args)
        {
            var useJson = Array.Exists(args, a => a.Equals("--json", StringComparison.OrdinalIgnoreCase));
            var services = await _serviceManager.GetAllServicesAsync();

            if (useJson)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(services,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(json);
                return;
            }

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

        private async Task BackupCreateAsync(string[] args)
        {
            string? outputPath = null;
            string description = "";

            for (int i = 1; i < args.Length - 1; i++)
            {
                if (args[i].Equals("--output", StringComparison.OrdinalIgnoreCase))
                    outputPath = args[i + 1];
                else if (args[i].Equals("--description", StringComparison.OrdinalIgnoreCase))
                    description = args[i + 1];
            }

            var filePath = await _backup.CreateBackupAsync(outputPath, description);
            Console.WriteLine($"Backup created: {filePath}");
        }

        private async Task BackupRestoreAsync(string backupFilePath)
        {
            var manifest = await _backup.RestoreBackupAsync(backupFilePath);
            Console.WriteLine($"Backup restored successfully.");
            Console.WriteLine($"  Backup ID:  {manifest.BackupId}");
            Console.WriteLine($"  Created At: {manifest.CreatedAt:O}");
            Console.WriteLine($"  Services:   {manifest.ServiceCount}");
            Console.WriteLine($"  Caddyfile:  {(manifest.CaddyfileContent is not null ? "restored" : "not included")}");
            if (!string.IsNullOrWhiteSpace(manifest.Description))
                Console.WriteLine($"  Note:       {manifest.Description}");
        }

        private async Task BackupListAsync(string[] args)
        {
            string? directory = null;
            for (int i = 1; i < args.Length - 1; i++)
            {
                if (args[i].Equals("--dir", StringComparison.OrdinalIgnoreCase))
                    directory = args[i + 1];
            }

            var files = await _backup.ListBackupsAsync(directory);

            if (files.Count == 0)
            {
                Console.WriteLine("No backups found.");
                return;
            }

            Console.WriteLine($"Available backups ({files.Count}):");
            Console.WriteLine(new string('-', 60));
            foreach (var file in files)
                Console.WriteLine($"  {file}");
        }

        private async Task LogsViewAsync(string[] args)
        {
            var options = new LogQueryOptions();

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].Equals("--lines", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out var lines) && lines > 0)
                        options.Lines = lines;
                }
                else if (args[i].Equals("--level", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    options.MinLevel = args[++i];
                }
                else if (args[i].Equals("--service", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    options.ServiceId = args[++i];
                }
                else if (args[i].Equals("--since", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (DateTime.TryParse(args[++i], CultureInfo.InvariantCulture, DateTimeStyles.None, out var since))
                        options.Since = since.ToUniversalTime();
                }
            }

            var entries = await _logAggregation.GetLogsAsync(options);

            if (entries.Count == 0)
            {
                Console.WriteLine("No log entries found matching the given filters.");
                return;
            }

            Console.WriteLine($"{"Timestamp",-30} {"Level",-8} {"Source",-20} Message");
            Console.WriteLine(new string('-', 100));

            foreach (var entry in entries)
            {
                Console.WriteLine($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff,-30} {entry.Level,-8} {entry.Source,-20} {entry.Message}");
            }
        }

        private async Task SslCheckAsync(string[] args)
        {
            // If a domain is specified check that one, otherwise check all enabled services
            if (args.Length >= 2 && !args[1].StartsWith("--"))
            {
                var domain = args[1];
                var outcome = await _sslMonitor.CheckCertificateAsync(domain);

                if (!outcome.IsSuccess)
                {
                    Console.Error.WriteLine($"[ERROR] {outcome.ErrorMessage}");
                    return;
                }

                PrintSslResult(outcome.Data);
                return;
            }

            // Check all services
            var services = await _serviceManager.GetEnabledServicesAsync();
            var results = await _sslMonitor.CheckAllServicesAsync(services);

            if (results.Count == 0)
            {
                Console.WriteLine("No services with public domains found to check.");
                return;
            }

            Console.WriteLine($"{"Domain",-30} {"Status",-14} {"Expires",-12} Message");
            Console.WriteLine(new string('-', 90));

            foreach (var result in results)
            {
                var expiry = result.Certificate is not null
                    ? result.Certificate.ExpiresAt.ToString("yyyy-MM-dd")
                    : "N/A";
                Console.WriteLine($"{result.Domain,-30} {result.Status,-14} {expiry,-12} {result.Message}");
            }
        }

        private static void PrintSslResult(SslCertificateCheckResult result)
        {
            Console.WriteLine($"Domain:  {result.Domain}");
            Console.WriteLine($"Status:  {result.Status}");
            Console.WriteLine($"Message: {result.Message}");
            Console.WriteLine($"Checked: {result.CheckedAt:O}");

            if (result.Certificate is not null)
            {
                Console.WriteLine($"Subject: {result.Certificate.Subject}");
                Console.WriteLine($"Issuer:  {result.Certificate.Issuer}");
                Console.WriteLine($"Issued:  {result.Certificate.IssuedAt:yyyy-MM-dd}");
                Console.WriteLine($"Expires: {result.Certificate.ExpiresAt:yyyy-MM-dd} ({result.Certificate.DaysUntilExpiry} days)");
            }
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
            Console.WriteLine("  version                   Show version information");
            Console.WriteLine("  help                      Show help information");
            Console.WriteLine("  list-services             List all services (--json for JSON output)");
            Console.WriteLine("  service-status <id>       Get service status");
            Console.WriteLine("  health-check <id>         Perform health check");
            Console.WriteLine("  health-summary            Get overall health summary");
            Console.WriteLine("  config-get <key>          Get configuration value");
            Console.WriteLine("  config-set <k> <v>        Set configuration value");
            Console.WriteLine("  backup-create             Create a configuration backup (--output <path>)");
            Console.WriteLine("  backup-restore <path>     Restore configuration from a backup file");
            Console.WriteLine("  backup-list               List available backups (--dir <path>)");
            Console.WriteLine("  logs-view                 View aggregated logs (--lines N, --level L, --service ID)");
            Console.WriteLine("  ssl-check [<domain>]      Check SSL certificate status for a domain or all services");
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
            Console.WriteLine("  - Backup and restore of all service configurations");
            Console.WriteLine("  - Aggregated log viewer across all service log files");
            Console.WriteLine("  - SSL certificate status checker with expiry alerts");
            Console.WriteLine("\nFor more information, visit: https://github.com/vladyslav-zaiets/caddy-vps-toolkit");
        }
    }
}
