#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Results;
using CaddyVpsToolkit.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CaddyVpsToolkit.Tests.Services
{
    /// <summary>
    /// Tests for the SslCertStatusChecker class.
    /// </summary>
    public sealed class SslCertStatusCheckerTests
    {
        private readonly ISslCertificateMonitoringService _sslMonitor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SslCertStatusCheckerTests"/> class.
        /// </summary>
        public SslCertStatusCheckerTests()
        {
            _sslMonitor = Substitute.For<ISslCertificateMonitoringService>();
        }

        /// <summary>
        /// Verifies that the CheckAllServicesAsync method skips local host bindings.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task CheckAllServicesAsync_SkipsLocalHostBindings()
        {
            // Arrange
            var services = new List<ManagedService>
            {
                new ManagedService
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "local-svc",
                    HostBinding = "localhost",
                    Port = 8080,
                    ExecutablePath = "/usr/bin/app",
                    WorkingDirectory = "/app",
                    Description = "Local service"
                }
            };

            _sslMonitor.CheckAllServicesAsync(services, Arg.Any<CancellationToken>())
                .Returns(new List<SslCertificateCheckResult>().AsReadOnly());

            // Act
            var results = await _sslMonitor.CheckAllServicesAsync(services, CancellationToken.None);

            // Assert
            results.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the CheckCertificateAsync method returns a failure when the domain is empty.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task CheckCertificateAsync_WithEmptyDomain_ReturnsFailure()
        {
            // Arrange
            _sslMonitor.CheckCertificateAsync(string.Empty, Arg.Any<CancellationToken>())
                .Returns(Result<SslCertificateCheckResult>.Failure("Domain must not be empty.", "INVALID_DOMAIN"));

            // Act
            var result = await _sslMonitor.CheckCertificateAsync(string.Empty);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be("INVALID_DOMAIN");
        }

        /// <summary>
        /// Verifies that the SslCertificateCheckResult.CreateValid method sets the correct status.
        /// </summary>
        /// <param name="domain">The domain of the certificate.</param>
        /// <param name="cert">The certificate information.</param>
        /// <returns>A new instance of the SslCertificateCheckResult class.</returns>
        [Fact]
        public void SslCertificateCheckResult_CreateValid_SetsCorrectStatus()
        {
            // Arrange
            var cert = new SslCertificateInfo
            {
                Domain = "example.com",
                Subject = "CN=example.com",
                Issuer = "CN=Let's Encrypt",
                IssuedAt = DateTime.UtcNow.AddDays(-30),
                ExpiresAt = DateTime.UtcNow.AddDays(60)
            };

            // Act
            var result = SslCertificateCheckResult.CreateValid("example.com", cert);

            // Assert
            result.Status.Should().Be(SslCertificateStatus.Valid);
            result.Domain.Should().Be("example.com");
            result.Certificate.Should().NotBeNull();
            result.Certificate!.DaysUntilExpiry.Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Verifies that the SslCertificateCheckResult.CreateExpired method sets the correct status.
        /// </summary>
        /// <param name="domain">The domain of the certificate.</param>
        /// <param name="cert">The certificate information.</param>
        /// <returns>A new instance of the SslCertificateCheckResult class.</returns>
        [Fact]
        public void SslCertificateCheckResult_CreateExpired_SetsCorrectStatus()
        {
            // Arrange
            var cert = new SslCertificateInfo
            {
                Domain = "old.example.com",
                ExpiresAt = DateTime.UtcNow.AddDays(-5),
                IssuedAt = DateTime.UtcNow.AddYears(-1)
            };

            // Act
            var result = SslCertificateCheckResult.CreateExpired("old.example.com", cert);

            // Assert
            result.Status.Should().Be(SslCertificateStatus.Expired);
            result.Message.Should().Contain("expired");
        }

        /// <summary>
        /// Verifies that the SslCertificateCheckResult.CreateExpiringSoon method sets the correct status.
        /// </summary>
        /// <param name="domain">The domain of the certificate.</param>
        /// <param name="cert">The certificate information.</param>
        /// <param name="isCritical">A flag indicating whether the certificate is critical.</param>
        /// <returns>A new instance of the SslCertificateCheckResult class.</returns>
        [Fact]
        public void SslCertificateCheckResult_CreateExpiringSoon_Critical_SetsCorrectStatus()
        {
            // Arrange
            var cert = new SslCertificateInfo
            {
                Domain = "soon.example.com",
                ExpiresAt = DateTime.UtcNow.AddDays(3),
                IssuedAt = DateTime.UtcNow.AddDays(-87)
            };

            // Act
            var result = SslCertificateCheckResult.CreateExpiringSoon("soon.example.com", cert, isCritical: true);

            // Assert
            result.Status.Should().Be(SslCertificateStatus.Critical);
        }

        /// <summary>
        /// Verifies that the SslCertificateCheckResult.CreateError method sets the error status.
        /// </summary>
        /// <param name="domain">The domain of the certificate.</param>
        /// <param name="message">The error message.</param>
        /// <returns>A new instance of the SslCertificateCheckResult class.</returns>
        [Fact]
        public void SslCertificateCheckResult_CreateError_SetsErrorStatus()
        {
            // Act
            var result = SslCertificateCheckResult.CreateError("bad.example.com", "Connection refused");

            // Assert
            result.Status.Should().Be(SslCertificateStatus.Error);
            result.Certificate.Should().BeNull();
            result.Message.Should().Contain("Connection refused");
        }

        /// <summary>
        /// Verifies that the SslCertificateInfo.DaysUntilExpiry method returns zero for expired certificates.
        /// </summary>
        /// <param name="cert">The certificate information.</param>
        /// <returns>The number of days until expiry.</returns>
        [Fact]
        public void SslCertificateInfo_DaysUntilExpiry_ReturnsZeroForExpiredCert()
        {
            // Arrange
            var cert = new SslCertificateInfo
            {
                Domain = "example.com",
                ExpiresAt = DateTime.UtcNow.AddDays(-10),
                IssuedAt = DateTime.UtcNow.AddYears(-1)
            };

            // Act & Assert
            cert.DaysUntilExpiry.Should().Be(0);
            cert.IsValid.Should().BeFalse();
        }
    }
}
