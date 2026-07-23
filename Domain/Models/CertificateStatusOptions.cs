#nullable enable

using System;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Configuration options for SSL certificate status determination.
    /// Allows customization of warning and critical expiry thresholds.
    /// </summary>
    public sealed class CertificateStatusOptions
    {
        /// <summary>
        /// Number of days before expiry that triggers a warning status.
        /// Default is 30 days, suitable for Let's Encrypt certificates (90-day lifetime, renew at 30 days).
        /// </summary>
        public int WarningDays { get; set; } = 30;

        /// <summary>
        /// Number of days before expiry that triggers a critical status.
        /// Default is 7 days, suitable for Let's Encrypt certificates.
        /// </summary>
        public int CriticalDays { get; set; } = 7;

        /// <summary>
        /// Validates the options to ensure thresholds are logically consistent.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if WarningDays is less than CriticalDays.</exception>
        public void Validate()
        {
            if (WarningDays < CriticalDays)
            {
                throw new ArgumentException(
                    $"WarningDays ({WarningDays}) cannot be less than CriticalDays ({CriticalDays}). " +
                    "Critical threshold must trigger before warning threshold.",
                    nameof(WarningDays));
            }

            if (WarningDays <= 0)
            {
                throw new ArgumentException(
                    "WarningDays must be a positive integer.",
                    nameof(WarningDays));
            }

            if (CriticalDays <= 0)
            {
                throw new ArgumentException(
                    "CriticalDays must be a positive integer.",
                    nameof(CriticalDays));
            }
        }
    }
}
