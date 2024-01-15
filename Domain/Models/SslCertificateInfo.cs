// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    /// Metadata for an SSL/TLS certificate retrieved from a remote domain.
    /// </summary>
    public class SslCertificateInfo
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
        public int DaysUntilExpiry => Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalDays);

        /// <summary>Whether the certificate is currently within its stated validity window.</summary>
        public bool IsValid => DateTime.UtcNow >= IssuedAt && DateTime.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// Outcome of an SSL certificate check for a single domain.
    /// </summary>
    public class SslCertificateCheckResult
    {
        /// <summary>Domain that was inspected.</summary>
        [Required]
        public string Domain { get; set; } = string.Empty;

        /// <summary>Determined certificate health status.</summary>
        public SslCertificateStatus Status { get; set; }

        /// <summary>Certificate metadata when retrieval succeeded; null on network or TLS error.</summary>
        public SslCertificateInfo Certificate { get; set; }

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
