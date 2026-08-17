#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// Extension methods for SslCertificateInfo
// =============================================================================

using System;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Provides fluent extension methods for <see cref="SslCertificateInfo"/>.
    /// </summary>
    public static class SslCertificateInfoFluentExtensions
    {
        /// <summary>
        /// Calculates the number of whole days remaining until the certificate expires,
        /// using the supplied reference <paramref name="now"/> instead of <c>DateTime.UtcNow</c>.
        /// </summary>
        /// <param name="info">The <see cref="SslCertificateInfo"/> instance.</param>
        /// <param name="now">The point in time to calculate the remaining days from.</param>
        /// <returns>
        /// The number of whole days until expiry. Returns <c>0</c> if the certificate has already expired.
        /// </returns>
        public static int DaysUntilExpiry(this SslCertificateInfo info, DateTime now)
        {
            // Ensure both dates are in UTC to avoid timezone issues.
            var expiresAtUtc = info.ExpiresAt.Kind == DateTimeKind.Utc
                ? info.ExpiresAt
                : info.ExpiresAt.ToUniversalTime();

            var nowUtc = now.Kind == DateTimeKind.Utc ? now : now.ToUniversalTime();

            var days = (int)(expiresAtUtc - nowUtc).TotalDays;
            return Math.Max(0, days);
        }

        /// <summary>
        /// Determines whether the certificate will expire within the specified <paramref name="timeSpan"/>
        /// from the current moment (<c>DateTime.UtcNow</c>).
        /// </summary>
        /// <param name="info">The <see cref="SslCertificateInfo"/> instance.</param>
        /// <param name="timeSpan">The time window to check against.</param>
        /// <returns>
        /// <c>true</c> if the certificate is still valid and its remaining days are less than or equal to
        /// <paramref name="timeSpan"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsExpiringWithin(this SslCertificateInfo info, TimeSpan timeSpan)
        {
            // If the certificate is already invalid, it cannot be "expiring within" a future window.
            if (!info.IsValid)
                return false;

            var daysRemaining = info.DaysUntilExpiry;
            return daysRemaining <= timeSpan.TotalDays;
        }

        /// <summary>
        /// Returns a concise status string for the certificate based on its remaining validity.
        /// The logic mirrors the thresholds used elsewhere in the project:
        /// <list type="bullet">
        ///   <item><description>Critical – expires within 7 days.</description></item>
        ///   <item><description>ExpiringSoon – expires within 30 days.</description></item>
        ///   <item><description>Expired – already past its expiry date.</description></item>
        ///   <item><description>Valid – otherwise.</description></item>
        /// </list>
        /// </summary>
        /// <param name="info">The <see cref="SslCertificateInfo"/> instance.</param>
        /// <returns>A string representing the certificate's health status.</returns>
        public static string ToStatusString(this SslCertificateInfo info)
        {
            // Use the existing properties to infer status.
            if (!info.IsValid)
                return "Expired";

            var days = info.DaysUntilExpiry;

            if (days <= 7)
                return "Critical";

            if (days <= 30)
                return "ExpiringSoon";

            return "Valid";
        }
    }
}
