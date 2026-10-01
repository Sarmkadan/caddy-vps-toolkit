#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Services;

namespace CaddyVpsToolkit.Monitoring
{
    /// <summary>
    /// Periodically checks TLS certificates for monitored domains and reports
    /// expiration warnings based on configurable thresholds.
    /// </summary>
    public sealed class CertificateMonitor : IDisposable
    {
        private readonly SslCertificateMonitoringOptions _options;
        private readonly List<string> _domains = new();
        private bool _disposed;

        /// <summary>
        /// Fired when a certificate crosses the warning or critical threshold.
        /// </summary>
        public event Action<CertificateStatusResult>? CertificateAlert;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateMonitor"/> class.
        /// </summary>
        /// <param name="options">Monitoring thresholds and interval settings.</param>
        public CertificateMonitor(SslCertificateMonitoringOptions? options = null)
        {
            _options = options ?? new SslCertificateMonitoringOptions();
        }

        /// <summary>
        /// Adds a domain to the watch list.
        /// </summary>
        /// <param name="domain">Fully qualified domain name to monitor.</param>
        public void AddDomain(string domain)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(domain))
                throw new ArgumentException("Domain must not be empty.", nameof(domain));

            var normalized = domain.Trim().ToLowerInvariant();
            if (!_domains.Contains(normalized))
                _domains.Add(normalized);
        }

        /// <summary>
        /// Removes a domain from the watch list.
        /// </summary>
        /// <param name="domain">Domain to stop monitoring.</param>
        /// <returns><c>true</c> if the domain was found and removed.</returns>
        public bool RemoveDomain(string domain)
        {
            ThrowIfDisposed();
            return _domains.Remove(domain.Trim().ToLowerInvariant());
        }

        /// <summary>
        /// Checks the TLS certificate for a single domain and returns its status.
        /// </summary>
        /// <param name="domain">The domain to probe.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="CertificateStatusResult"/> describing the certificate health.</returns>
        public async Task<CertificateStatusResult> CheckDomainAsync(string domain, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(domain, AppConstants.DefaultHttpsPort, ct);

                using var ssl = new SslStream(tcp.GetStream(), leaveInnerStreamOpen: false,
                    (_, _, _, _) => true);

                await ssl.AuthenticateAsClientAsync(domain);

                var cert = ssl.RemoteCertificate as X509Certificate2
                    ?? new X509Certificate2(ssl.RemoteCertificate!);

                var daysRemaining = (int)(cert.NotAfter - DateTime.UtcNow).TotalDays;

                var status = daysRemaining switch
                {
                    < 0 => SslCertificateStatus.Expired,
                    <= 7 => SslCertificateStatus.Critical,
                    <= 30 => SslCertificateStatus.ExpiringSoon,
                    _ => SslCertificateStatus.Valid
                };

                return new CertificateStatusResult(status, daysRemaining,
                    $"{domain}: {status}, {daysRemaining} days remaining");
            }
            catch (Exception ex)
            {
                return new CertificateStatusResult(SslCertificateStatus.Error, 0,
                    $"{domain}: error - {ex.Message}");
            }
        }

        /// <summary>
        /// Runs a full check across all registered domains and fires alerts for non-valid ones.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A list of results for every monitored domain.</returns>
        public async Task<IReadOnlyList<CertificateStatusResult>> CheckAllAsync(CancellationToken ct = default)
        {
            ThrowIfDisposed();
            var results = new List<CertificateStatusResult>();

            foreach (var domain in _domains)
            {
                var result = await CheckDomainAsync(domain, ct);
                results.Add(result);

                if (result.Health != SslCertificateStatus.Valid)
                    CertificateAlert?.Invoke(result);
            }

            return results.AsReadOnly();
        }

        /// <summary>
        /// Returns the list of currently monitored domains.
        /// </summary>
        public IReadOnlyList<string> GetMonitoredDomains() => _domains.AsReadOnly();

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _domains.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CertificateMonitor));
        }
    }
}
