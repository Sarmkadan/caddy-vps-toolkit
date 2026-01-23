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
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Middleware;
using CaddyVpsToolkit.Notifications;
using CaddyVpsToolkit.Results;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Contract for SSL certificate monitoring and renewal alerting.
    /// </summary>
    public interface ISslCertificateMonitoringService
    {
        /// <summary>Checks the SSL certificate for a single domain and returns a classified result.</summary>
        Task<Result<SslCertificateCheckResult>> CheckCertificateAsync(string domain, CancellationToken cancellationToken = default);

        /// <summary>Checks SSL certificates for all provided managed services.</summary>
        Task<IReadOnlyList<SslCertificateCheckResult>> CheckAllServicesAsync(IEnumerable<ManagedService> services, CancellationToken cancellationToken = default);

        /// <summary>Dispatches renewal alert notifications for certificates that are expiring or expired.</summary>
        Task SendRenewalAlertsAsync(IEnumerable<SslCertificateCheckResult> results, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Monitors SSL/TLS certificates for managed services and dispatches renewal alerts
    /// through the notification pipeline when certificates are approaching expiry or have expired.
    /// </summary>
    public sealed class SslCertificateMonitoringService : ISslCertificateMonitoringService
    {
        private const int DefaultHttpsPort = 443;

        /// <summary>Alert when fewer than this many days remain before expiry.</summary>
        private const int WarnDays = 30;

        /// <summary>Escalate to critical when fewer than this many days remain.</summary>
        private const int CriticalDays = 7;

        private const int TcpTimeoutMs = 10_000;

        private readonly ILogger _logger;
        private readonly NotificationService _notifications;

        /// <summary>
        /// Initializes a new instance of <see cref="SslCertificateMonitoringService"/>.
        /// </summary>
        public SslCertificateMonitoringService(ILogger logger, NotificationService notifications)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        }

        /// <inheritdoc/>
        public async Task<Result<SslCertificateCheckResult>> CheckCertificateAsync(
            string domain,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return Result<SslCertificateCheckResult>.Failure("Domain must not be empty.", "INVALID_DOMAIN");

            await _logger.LogDebugAsync($"Checking SSL certificate for {domain}");

            try
            {
                var cert = await FetchCertificateAsync(domain, cancellationToken);
                var result = ClassifyCertificate(domain, cert);
                await _logger.LogInfoAsync($"SSL check for {domain}: {result.Status} — {result.Message}");
                return Result<SslCertificateCheckResult>.Success(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync($"SSL check failed for {domain}: {ex.Message}");
                return Result<SslCertificateCheckResult>.Success(
                    SslCertificateCheckResult.CreateError(domain, ex.Message));
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<SslCertificateCheckResult>> CheckAllServicesAsync(
            IEnumerable<ManagedService> services,
            CancellationToken cancellationToken = default)
        {
            var results = new List<SslCertificateCheckResult>();

            foreach (var service in services)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(service.HostBinding) ||
                    service.HostBinding.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                    continue;

                var outcome = await CheckCertificateAsync(service.HostBinding, cancellationToken);
                if (outcome.IsSuccess)
                    results.Add(outcome.Data);
            }

            return results.AsReadOnly();
        }

        /// <inheritdoc/>
        public async Task SendRenewalAlertsAsync(
            IEnumerable<SslCertificateCheckResult> results,
            CancellationToken cancellationToken = default)
        {
            var alertable = results
                .Where(r => r.Status != SslCertificateStatus.Valid)
                .ToList();

            if (alertable.Count == 0)
            {
                await _logger.LogDebugAsync("No SSL renewal alerts required.");
                return;
            }

            foreach (var result in alertable)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var priority = result.Status switch
                {
                    SslCertificateStatus.Critical or SslCertificateStatus.Expired => NotificationPriority.Critical,
                    SslCertificateStatus.Error => NotificationPriority.High,
                    _ => NotificationPriority.Normal
                };

                var sent = await _notifications.SendAsync(
                    $"SSL Certificate Alert: {result.Domain}",
                    result.Message,
                    priority);

                if (!sent)
                    await _logger.LogWarningAsync($"Failed to dispatch SSL alert for {result.Domain}");
            }

            await _logger.LogInfoAsync($"Dispatched {alertable.Count} SSL renewal alert(s).");
        }

        // Connects via TLS and reads the remote certificate without validating it,
        // so expiry metadata can be retrieved even for already-expired certificates.
        private static async Task<SslCertificateInfo> FetchCertificateAsync(
            string domain,
            CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TcpTimeoutMs);

            using var tcp = new TcpClient();
            await tcp.ConnectAsync(domain, DefaultHttpsPort, cts.Token);

            using var ssl = new SslStream(tcp.GetStream(), leaveInnerStreamOpen: false);
            await ssl.AuthenticateAsClientAsync(
                new SslClientAuthenticationOptions
                {
                    TargetHost = domain,
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
                },
                cts.Token);

            var x509 = (X509Certificate2)ssl.RemoteCertificate!;

            return new SslCertificateInfo
            {
                Domain = domain,
                Subject = x509.Subject,
                Issuer = x509.Issuer,
                IssuedAt = x509.NotBefore.ToUniversalTime(),
                ExpiresAt = x509.NotAfter.ToUniversalTime()
            };
        }

        private static SslCertificateCheckResult ClassifyCertificate(string domain, SslCertificateInfo cert)
        {
            if (cert.ExpiresAt < DateTime.UtcNow)
                return SslCertificateCheckResult.CreateExpired(domain, cert);

            if (cert.DaysUntilExpiry <= CriticalDays)
                return SslCertificateCheckResult.CreateExpiringSoon(domain, cert, isCritical: true);

            if (cert.DaysUntilExpiry <= WarnDays)
                return SslCertificateCheckResult.CreateExpiringSoon(domain, cert, isCritical: false);

            return SslCertificateCheckResult.CreateValid(domain, cert);
        }
    }
}
