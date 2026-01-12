// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using CaddyVpsToolkit.Services;

namespace CaddyVpsToolkit.Extensions
{
    /// <summary>
    /// Extension methods for registering SSL certificate monitoring into the DI container.
    /// </summary>
    public static class SslCertificateExtensions
    {
        /// <summary>
        /// Registers <see cref="SslCertificateMonitoringService"/> and
        /// <see cref="ISslCertificateMonitoringService"/> as singletons.
        /// </summary>
        public static IServiceCollection AddSslCertificateMonitoring(this IServiceCollection services)
        {
            services.AddSingleton<ISslCertificateMonitoringService, SslCertificateMonitoringService>();
            return services;
        }
    }
}
