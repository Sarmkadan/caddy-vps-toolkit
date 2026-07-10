#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Provides extension methods for <see cref="SslCertificateInfo"/> to simplify common certificate operations.
    /// </summary>
    public static class SslCertificateInfoExtensions
    {
        /// <summary>
        /// Determines if the certificate is currently valid and not approaching expiry.
        /// </summary>
        /// <param name="certificate">The certificate to check.</param>
        /// <param name="expiryWarningThresholdDays">Number of days before expiry to consider the certificate as "expiring soon". Default is 30 days.</param>
        /// <param name="criticalThresholdDays">Number of days before expiry to consider the certificate as "critical". Default is 7 days.</param>
        /// <returns>A tuple containing the status and a human-readable message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="certificate"/> is <see langword="null"/></exception>
        public static (SslCertificateStatus Status, string Message) GetStatus(
            this SslCertificateInfo certificate,
            int expiryWarningThresholdDays = 30,
            int criticalThresholdDays = 7)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            var daysUntilExpiry = certificate.DaysUntilExpiry;
            var now = DateTime.UtcNow;

            if (daysUntilExpiry < 0)
            {
                return (SslCertificateStatus.Expired,
                    $"Certificate expired on {certificate.ExpiresAt:yyyy-MM-dd} ({Math.Abs(daysUntilExpiry)} days ago).");
            }

            if (daysUntilExpiry <= criticalThresholdDays)
            {
                return (daysUntilExpiry <= 0 ? SslCertificateStatus.Expired : SslCertificateStatus.Critical,
                    $"Certificate expires in {daysUntilExpiry} day(s) on {certificate.ExpiresAt:yyyy-MM-dd} (CRITICAL).");
            }

            if (daysUntilExpiry <= expiryWarningThresholdDays)
            {
                return (SslCertificateStatus.ExpiringSoon,
                    $"Certificate expires in {daysUntilExpiry} day(s) on {certificate.ExpiresAt:yyyy-MM-dd}.");
            }

            return (SslCertificateStatus.Valid,
                $"Certificate valid for {daysUntilExpiry} day(s).");
        }

        /// <summary>
        /// Formats the certificate's validity period as a human-readable string.
        /// </summary>
        /// <param name="certificate">The certificate to format.</param>
        /// <returns>A formatted string showing the validity period.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="certificate"/> is <see langword="null"/></exception>
        public static string FormatValidityPeriod(this SslCertificateInfo certificate)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            return $"{certificate.IssuedAt:yyyy-MM-dd} to {certificate.ExpiresAt:yyyy-MM-dd}";
        }

        /// <summary>
        /// Gets the remaining validity period in days as a formatted string.
        /// </summary>
        /// <param name="certificate">The certificate to check.</param>
        /// <returns>A string showing days remaining, or "Expired" if already expired.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="certificate"/> is <see langword="null"/></exception>
        public static string FormatDaysUntilExpiry(this SslCertificateInfo certificate)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            var days = certificate.DaysUntilExpiry;
            return days <= 0 ? "Expired" : $"{days} day(s)";
        }

        /// <summary>
        /// Determines if the certificate was issued by a specific certificate authority.
        /// </summary>
        /// <param name="certificate">The certificate to check.</param>
        /// <param name="issuerName">The issuer name to match (case-insensitive).</param>
        /// <returns><see langword="true"/> if the issuer matches; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="certificate"/> or <paramref name="issuerName"/> is <see langword="null"/></exception>
        public static bool IsIssuedBy(
            this SslCertificateInfo certificate,
            string issuerName)
        {
            ArgumentNullException.ThrowIfNull(certificate);
            ArgumentException.ThrowIfNullOrEmpty(issuerName);

            return certificate.Issuer.Contains(issuerName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets all expired certificates from a collection.
        /// </summary>
        /// <param name="certificates">The collection of certificates to filter.</param>
        /// <returns>An enumerable of expired certificates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="certificates"/> is <see langword="null"/></exception>
        public static IEnumerable<SslCertificateInfo> GetExpiredCertificates(
            this IEnumerable<SslCertificateInfo> certificates)
        {
            ArgumentNullException.ThrowIfNull(certificates);

            return certificates.Where(c => c.DaysUntilExpiry < 0);
        }

        /// <summary>
        /// Gets all certificates that are expiring soon (within the warning threshold).
        /// </summary>
        /// <param name="certificates">The collection of certificates to filter.</param>
        /// <param name="thresholdDays">Number of days before expiry to consider as "expiring soon".</param>
        /// <returns>An enumerable of expiring certificates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="certificates"/> is <see langword="null"/></exception>
        public static IEnumerable<SslCertificateInfo> GetExpiringCertificates(
            this IEnumerable<SslCertificateInfo> certificates,
            int thresholdDays = 30)
        {
            ArgumentNullException.ThrowIfNull(certificates);

            var thresholdDate = DateTime.UtcNow.AddDays(thresholdDays);
            return certificates.Where(c => c.DaysUntilExpiry > 0 && c.ExpiresAt <= thresholdDate);
        }

        /// <summary>
        /// Gets the issuer organization name from the certificate subject.
        /// </summary>
        /// <param name="certificate">The certificate to parse.</param>
        /// <returns>The issuer organization name, or an empty string if not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="certificate"/> is <see langword="null"/></exception>
        public static string GetIssuerOrganization(this SslCertificateInfo certificate)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            if (string.IsNullOrWhiteSpace(certificate.Issuer))
            {
                return string.Empty;
            }

            // Extract organization from issuer string (format: "CN=example.com, O=Organization Name, ...")
            var parts = certificate.Issuer.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("O=", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed[2..].Trim();
                }
            }

            return string.Empty;
        }
    }
}