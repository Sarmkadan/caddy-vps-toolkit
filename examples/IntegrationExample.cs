using Microsoft.Extensions.DependencyInjection;
using CaddyVpsToolkit.Services;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Notifications;

namespace CaddyVpsToolkit.Examples
{
    /// <summary>
    /// Demonstrates how to register the Caddy VPS Toolkit services
    /// into an ASP.NET Core Dependency Injection container.
    /// </summary>
    public static class IntegrationExample
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            // Register repositories
            services.AddSingleton<IServiceRepository, ServiceRepository>();
            services.AddSingleton<IHealthCheckRepository, HealthCheckRepository>();
            services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();

            // Register Core Services
            services.AddSingleton<ServiceManagementService>();
            services.AddSingleton<CaddyConfigurationService>();
            services.AddSingleton<SystemdUnitService>();
            services.AddSingleton<HealthMonitoringService>();
            services.AddSingleton<ConfigurationService>();

            // Register Supporting Services
            services.AddSingleton<NotificationService>();
            services.AddSingleton<IBackupService, BackupService>();
            services.AddSingleton<ILogAggregationService, LogAggregationService>();
            services.AddSingleton<ISslCertificateMonitoringService, SslCertificateMonitoringService>();
        }
    }
}
