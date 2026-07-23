#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.ComponentModel.DataAnnotations;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Describes the health state of an SSL/TLS certificate.
    /// </summary>
    public enum SslCertificateStatus
    {
        /// <summary>Certificate is valid and not approaching expiry.</summary>
        Valid = 0,

        /// <summary>Certificate expires within the warning threshold (default 30 days).</summary>
        ExpiringSoon = 1,

        /// <summary>Certificate expires within the critical threshold (default 7 days).</summary>
        Critical = 2,

        /// <summary>Certificate has already passed its expiry date.</summary>
        Expired = 3,

        /// <summary>Certificate could not be retrieved due to a network or TLS error.</summary>
        Error = 4
    }

    /// <summary>
    /// Represents the health status of an SSL certificate with additional context.
    /// </summary>
    /// <param name="Health">The health status of the certificate.</param>
    /// <param name="DaysRemaining">Number of days remaining until expiry, or negative if expired.</param>
    /// <param name="Message">Human-readable description of the certificate status.</param>
    public readonly record struct CertificateStatusResult(
        SslCertificateStatus Health,
        int DaysRemaining,
        string Message)
    {
        /// <summary>
        /// Gets a value indicating whether the certificate is in a healthy state.
        /// </summary>
        public bool IsHealthy => Health == SslCertificateStatus.Valid;

        /// <summary>
        /// Gets a value indicating whether the certificate requires immediate attention (critical or expired).
        /// </summary>
        public bool RequiresAttention => Health == SslCertificateStatus.Critical || Health == SslCertificateStatus.Expired;

        /// <summary>
        /// Gets a value indicating whether the certificate is expired.
        /// </summary>
        public bool IsExpired => Health == SslCertificateStatus.Expired;

        /// <summary>
        /// Gets a value indicating whether the certificate is approaching expiry (within warning threshold).
        /// </summary>
        public bool IsExpiringSoon => Health == SslCertificateStatus.ExpiringSoon;
    }

    /// <summary>
    /// Metadata for an SSL/TLS certificate retrieved from a remote domain.
    /// </summary>
    public sealed class SslCertificateInfo
    {
        /// <summary>Domain name the certificate was retrieved from.</summary>
        [Required]
        public string Domain { get; set; } = string.Empty;

        /// <summary>Certificate subject distinguished name.</summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>Issuing authority of the certificate.</summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>UTC date/time the certificate became valid.</summary>
        public DateTime IssuedAt { get; set; }

        /// <summary>UTC date/time the certificate expires.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Number of whole days remaining until the certificate expires. Returns zero once expired.</summary>
        public int DaysUntilExpiry
        {
            get
            {
                // Normalize both dates to UTC to avoid timezone-related issues
                var expiresAtUtc = ExpiresAt.Kind == DateTimeKind.Utc ? ExpiresAt : ExpiresAt.ToUniversalTime();
                var nowUtc = DateTime.UtcNow;
                var days = (int)(expiresAtUtc - nowUtc).TotalDays;
                return Math.Max(0, days);
            }
        }

        /// <summary>Whether the certificate is currently within its stated validity window.</summary>
        public bool IsValid
        {
            get
            {
                // Normalize all dates to UTC for comparison
                var issuedAtUtc = IssuedAt.Kind == DateTimeKind.Utc ? IssuedAt : IssuedAt.ToUniversalTime();
                var expiresAtUtc = ExpiresAt.Kind == DateTimeKind.Utc ? ExpiresAt : ExpiresAt.ToUniversalTime();
                var nowUtc = DateTime.UtcNow;
                return nowUtc >= issuedAtUtc && nowUtc < expiresAtUtc;
            }
        }
    }

    /// <summary>
    /// Outcome of an SSL certificate check for a single domain.
    /// </summary>
    public sealed class SslCertificateCheckResult
    {
        /// <summary>Domain that was inspected.</summary>
        [Required]
        public string Domain { get; set; } = string.Empty;

        /// <summary>Determined certificate health status.</summary>
        public SslCertificateStatus Status { get; set; }

        /// <summary>Certificate metadata when retrieval succeeded; null on network or TLS error.</summary>
        public SslCertificateInfo? Certificate { get; set; }

        /// <summary>Human-readable description of the check outcome.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>UTC timestamp at which this check was performed.</summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Creates a result for a healthy, non-expiring certificate.</summary>
        public static SslCertificateCheckResult CreateValid(string domain, SslCertificateInfo cert) =>
        new SslCertificateCheckResult
        {
            Domain = domain,
            Status = SslCertificateStatus.Valid,
            Certificate = cert,
            Message = $"Certificate valid for {cert.DaysUntilExpiry} day(s)."
        };

        /// <summary>Creates a result for a certificate approaching its expiry date.</summary>
        public static SslCertificateCheckResult CreateExpiringSoon(string domain, SslCertificateInfo cert, bool isCritical) =>
        new SslCertificateCheckResult
        {
            Domain = domain,
            Status = isCritical ? SslCertificateStatus.Critical : SslCertificateStatus.ExpiringSoon,
            Certificate = cert,
            Message = $"Certificate expires in {cert.DaysUntilExpiry} day(s) on {cert.ExpiresAt:yyyy-MM-dd}."
        };

        /// <summary>Creates a result for a certificate that has already expired.</summary>
        public static SslCertificateCheckResult CreateExpired(string domain, SslCertificateInfo cert) =>
        new SslCertificateCheckResult
        {
            Domain = domain,
            Status = SslCertificateStatus.Expired,
            Certificate = cert,
            Message = $"Certificate expired on {cert.ExpiresAt:yyyy-MM-dd}."
        };

        /// <summary>Creates a result when the certificate could not be retrieved.</summary>
        public static SslCertificateCheckResult CreateError(string domain, string error) =>
        new SslCertificateCheckResult
        {
            Domain = domain,
            Status = SslCertificateStatus.Error,
            Message = error
        };
    }
}