#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using CaddyVpsToolkit.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CaddyVpsToolkit.DependencyInjection
{
    /// <summary>
    /// Extension methods that register CaddyVpsToolkit configuration in the DI container.
    /// Options are validated when the host starts, so a bad setting fails startup with an
    /// <see cref="Microsoft.Extensions.Options.OptionsValidationException"/> that lists every failing property,
    /// instead of failing on first use.
    /// </summary>
    public static class CaddyVpsToolkitServiceCollectionExtensions
    {
        /// <summary>
        /// Registers and validates all CaddyVpsToolkit options from <paramref name="configuration"/>.
        /// </summary>
        /// <param name="services">The service collection to add the options to.</param>
        /// <param name="configuration">Configuration that holds the <c>CaddyClient</c> and <c>CertificateMonitor</c> sections.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
        public static IServiceCollection AddCaddyVpsToolkit(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddCaddyClientOptions(configuration);
            services.AddCertificateMonitorOptions(configuration);

            return services;
        }

        /// <summary>
        /// Registers <see cref="CaddyClientOptions"/> from the <c>CaddyClient</c> section and validates it on startup.
        /// </summary>
        /// <param name="services">The service collection to add the options to.</param>
        /// <param name="configuration">Configuration that holds the <c>CaddyClient</c> section.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddCaddyClientOptions(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddOptions<CaddyClientOptions>()
                .Bind(configuration.GetSection(CaddyClientOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(o => IsAbsoluteHttpUri(o.AdminEndpoint),
                    "AdminEndpoint must be an absolute http or https URI.")
                .Validate(o => o.Timeout > TimeSpan.Zero && o.Timeout <= TimeSpan.FromMinutes(5),
                    "Timeout must be greater than zero and at most 5 minutes.")
                .ValidateOnStart();

            return services;
        }

        /// <summary>
        /// Registers <see cref="CertificateMonitorOptions"/> from the <c>CertificateMonitor</c> section and validates it on startup.
        /// </summary>
        /// <param name="services">The service collection to add the options to.</param>
        /// <param name="configuration">Configuration that holds the <c>CertificateMonitor</c> section.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddCertificateMonitorOptions(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddOptions<CertificateMonitorOptions>()
                .Bind(configuration.GetSection(CertificateMonitorOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(o => o.CheckInterval >= TimeSpan.FromMinutes(1),
                    "CheckInterval must be at least 1 minute.")
                .Validate(o => o.Hosts is { Count: > 0 },
                    "Hosts must contain at least one host.")
                .Validate(o => HasNoEmptyHosts(o.Hosts),
                    "Hosts must not contain empty or whitespace entries.")
                .Validate(o => HasNoDuplicateHosts(o.Hosts),
                    "Hosts must not contain duplicate entries (case-insensitive).")
                .ValidateOnStart();

            return services;
        }

        private static bool IsAbsoluteHttpUri(string? endpoint) =>
            Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        private static bool HasNoEmptyHosts(List<string>? hosts) =>
            hosts is null || hosts.All(h => !string.IsNullOrWhiteSpace(h));

        private static bool HasNoDuplicateHosts(List<string>? hosts)
        {
            if (hosts is null)
                return true;

            var names = hosts
                .Where(h => !string.IsNullOrWhiteSpace(h))
                .Select(h => h.Trim())
                .ToList();

            return names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Count;
        }
    }
}
