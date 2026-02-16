#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Services;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Middleware;
using CaddyVpsToolkit.Notifications;

namespace CaddyVpsToolkit
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                var serviceProvider = services.BuildServiceProvider();

                var cli = serviceProvider.GetRequiredService<CliCommandHandler>();
                await cli.HandleAsync(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] {ex.Message}");
                Environment.Exit(1);
            }
        }

        static void ConfigureServices(IServiceCollection services)
        {
            // Configuration
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            services.AddSingleton<IConfiguration>(config);

            // Data Access
            services.AddSingleton<IServiceRepository, ServiceRepository>();
            services.AddSingleton<IHealthCheckRepository, HealthCheckRepository>();
            services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();

            // Services
            services.AddSingleton<ServiceManagementService>();
            services.AddSingleton<CaddyConfigurationService>();
            services.AddSingleton<SystemdUnitService>();
            services.AddSingleton<HealthMonitoringService>();
            services.AddSingleton<ConfigurationService>();

            // Logging (for SSL monitoring and notifications)
            services.AddSingleton<ILogger>(new MemoryLogger(LogLevel.Debug));
            services.AddSingleton<NotificationService>();

            // New feature services
            services.AddSingleton<IBackupService, BackupService>();
            services.AddSingleton<ILogAggregationService, LogAggregationService>();
            services.AddSingleton<ISslCertificateMonitoringService, SslCertificateMonitoringService>();

            // CLI
            services.AddSingleton<CliCommandHandler>();
        }
    }
}
