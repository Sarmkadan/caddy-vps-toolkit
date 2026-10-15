#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Core;

namespace CaddyVpsToolkit.Configuration
{
    /// <summary>
    /// Settings for certificate expiry monitoring. Bound from the <c>CertificateMonitor</c> section of
    /// configuration and validated at host startup by
    /// <see cref="CaddyVpsToolkit.DependencyInjection.CaddyVpsToolkitServiceCollectionExtensions.AddCaddyVpsToolkit"/>.
    /// </summary>
    public sealed class CertificateMonitorOptions
    {
        /// <summary>Configuration section name this class binds to.</summary>
        public const string SectionName = "CertificateMonitor";

        /// <summary>
        /// Days before expiry at which a certificate is reported as expiring soon. Must be between 1 and 365.
        /// </summary>
        [Range(1, 365, ErrorMessage = "WarningThresholdDays must be between 1 and 365.")]
        public int WarningThresholdDays { get; set; } = 30;

        /// <summary>
        /// How often the monitored certificates are checked. Must be at least 1 minute.
        /// </summary>
        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// TCP port used for the TLS handshake. Must be between 1 and 65535.
        /// </summary>
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
        public int Port { get; set; } = AppConstants.DefaultHttpsPort;

        /// <summary>
        /// Domains to monitor. Must contain at least one host, with no duplicates (case-insensitive).
        /// </summary>
        public List<string> Hosts { get; set; } = new();
    }
}
