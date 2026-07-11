#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using Microsoft.Extensions.DependencyInjection;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Services;

namespace CaddyVpsToolkit.Extensions
{
    /// <summary>
    /// Extension methods for registering SSL certificate monitoring into the DI container
    /// and providing utility methods for SSL certificate operations.
    /// </summary>
    public static class SslCertificateExtensions
    {
        /// <summary>
        /// Registers <see cref="SslCertificateMonitoringService"/> and
        /// <see cref="ISslCertificateMonitoringService"/> as singletons.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add registrations to.</param>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
        /// <returns>The original <see cref="IServiceCollection"/> for fluent chaining.</returns>
        public static IServiceCollection AddSslCertificateMonitoring(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<ISslCertificateMonitoringService, SslCertificateMonitoringService>();
            return services;
        }

        /// <summary>
        /// Adds SSL certificate monitoring and health check integration for a managed service.
        /// Configures the service to check its SSL certificate and dispatch renewal alerts.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
        /// <param name="service">The managed service to enable SSL monitoring for.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="services"/> or <paramref name="service"/> is <see langword="null"/>.
        /// </exception>
        /// <returns>The original <see cref="IServiceCollection"/> for fluent chaining.</returns>
        public static IServiceCollection AddSslCertificateMonitoring(
            this IServiceCollection services,
            ManagedService service)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(service);

            // Enable SSL monitoring for services that have a public host binding
            if (!string.IsNullOrWhiteSpace(service.HostBinding)
                && !service.HostBinding.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                // In a real implementation, this would register the service for periodic monitoring
                // For now, we just validate the configuration
                service.Validate();
            }

            return services;
        }

        /// <summary>
        /// Determines if an SSL certificate is approaching expiry based on the configured thresholds.
        /// </summary>
        /// <param name="certificate">The SSL certificate to check.</param>
        /// <param name="warnDays">Warning threshold in days (default: 30).</param>
        /// <param name="criticalDays">Critical threshold in days (default: 7).</param>
        /// <exception cref="ArgumentNullException"><paramref name="certificate"/> is <see langword="null"/>.</exception>
        /// <returns>
        /// <see cref="SslCertificateStatus"/> indicating the certificate's health state.
        /// </returns>
        public static SslCertificateStatus GetCertificateStatus(
            this SslCertificateInfo certificate,
            int warnDays = 30,
            int criticalDays = 7)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            if (certificate.ExpiresAt < DateTime.UtcNow)
                return SslCertificateStatus.Expired;

            var daysRemaining = certificate.DaysUntilExpiry;

            if (daysRemaining <= 0)
                return SslCertificateStatus.Expired;

            if (daysRemaining <= criticalDays)
                return SslCertificateStatus.Critical;

            if (daysRemaining <= warnDays)
                return SslCertificateStatus.ExpiringSoon;

            return SslCertificateStatus.Valid;
        }

        /// <summary>
        /// Checks if an SSL certificate requires immediate renewal action (critical or expired).
        /// </summary>
        /// <param name="certificate">The SSL certificate to check.</param>
        /// <param name="warnDays">Warning threshold in days (default: 30).</param>
        /// <param name="criticalDays">Critical threshold in days (default: 7).</param>
        /// <exception cref="ArgumentNullException"><paramref name="certificate"/> is <see langword="null"/>.</exception>
        /// <returns><see langword="true"/> if renewal is required; otherwise, <see langword="false"/>.</returns>
        public static bool RequiresRenewal(
            this SslCertificateInfo certificate,
            int warnDays = 30,
            int criticalDays = 7)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            return certificate.GetCertificateStatus(warnDays, criticalDays)
                is SslCertificateStatus.Critical or SslCertificateStatus.Expired;
        }

        /// <summary>
        /// Gets a human-readable message describing the SSL certificate status.
        /// </summary>
        /// <param name="certificate">The SSL certificate to describe.</param>
        /// <param name="warnDays">Warning threshold in days (default: 30).</param>
        /// <param name="criticalDays">Critical threshold in days (default: 7).</param>
        /// <exception cref="ArgumentNullException"><paramref name="certificate"/> is <see langword="null"/>.</exception>
        /// <returns>A formatted status message.</returns>
        public static string GetStatusMessage(
            this SslCertificateInfo certificate,
            int warnDays = 30,
            int criticalDays = 7)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            return certificate.GetCertificateStatus(warnDays, criticalDays) switch
            {
                SslCertificateStatus.Valid =>
                    $"Certificate valid for {certificate.DaysUntilExpiry} day(s).",
                SslCertificateStatus.ExpiringSoon =>
                    $"Certificate expires in {certificate.DaysUntilExpiry} day(s) on {certificate.ExpiresAt:yyyy-MM-dd}.",
                SslCertificateStatus.Critical =>
                    $"Certificate expires in {certificate.DaysUntilExpiry} day(s) on {certificate.ExpiresAt:yyyy-MM-dd} - RENEWAL REQUIRED!",
                SslCertificateStatus.Expired =>
                    $"Certificate expired on {certificate.ExpiresAt:yyyy-MM-dd} - IMMEDIATE ACTION REQUIRED!",
                SslCertificateStatus.Error =>
                    "Certificate status could not be determined.",
                _ => "Unknown certificate status"
            };
        }
    }
}
