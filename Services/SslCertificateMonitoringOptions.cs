#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// Configuration options for SSL certificate monitoring.
// =============================================================================

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Options to control the behaviour of <see cref="SslCertificateMonitoringService"/>.
    /// </summary>
    public sealed class SslCertificateMonitoringOptions
    {
        /// <summary>
        /// Number of days before expiry that triggers a warning status.
        /// Default matches the historic hard‑coded value of 30 days.
        /// </summary>
        public int WarnDays { get; set; } = 30;

        /// <summary>
        /// Number of days before expiry that triggers a critical status.
        /// Default matches the historic hard‑coded value of 7 days.
        /// </summary>
        public int CriticalDays { get; set; } = 7;
    }
}
