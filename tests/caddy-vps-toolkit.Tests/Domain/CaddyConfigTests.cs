#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Domain.Models;
using FluentAssertions;
using Xunit;

/// <summary>
/// Contains tests for the CaddyConfig class.
/// </summary>
namespace CaddyVpsToolkit.Tests.Domain
{
    public sealed class CaddyConfigTests
    {
        /// <summary>
        /// Verifies that a valid CaddyConfig instance does not throw an exception when validated.
        /// </summary>
        [Fact]
        public void Validate_WithValidData_ShouldNotThrow()
        {
            // Arrange
            var config = new CaddyConfig
            {
                AdminPort = 2019,
                HttpPort = 80,
                HttpsPort = 443,
                IdleTimeout = 120,
                ReadTimeout = 30,
                WriteTimeout = 30
            };

            // Act
            var act = () => config.Validate();

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// Verifies that a CaddyConfig instance with an invalid admin port throws a ValidationException.
        /// </summary>
        /// <param name="port">The admin port to test.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(65536)]
        public void Validate_WithInvalidAdminPort_ShouldThrowValidationException(int port)
        {
            // Arrange
            var config = new CaddyConfig { AdminPort = port, HttpPort = 80, HttpsPort = 443 };

            // Act
            var act = () => config.Validate();

            // Assert
            act.Should().Throw<ValidationException>().WithMessage("*Admin port must be between 1 and 65535*");
        }

        /// <summary>
        /// Verifies that a CaddyConfig instance with a negative timeout throws a ValidationException.
        /// </summary>
        [Fact]
        public void Validate_WithNegativeTimeout_ShouldThrowValidationException()
        {
            // Arrange
            var config = new CaddyConfig { AdminPort = 2019, HttpPort = 80, HttpsPort = 443, ReadTimeout = -1 };

            // Act
            var act = () => config.Validate();

            // Assert
            act.Should().Throw<ValidationException>().WithMessage("*Timeouts cannot be negative*");
        }

        /// <summary>
        /// Verifies that the SetDefaultValues method sets the default email addresses when the input emails are null.
        /// </summary>
        [Fact]
        public void SetDefaultValues_WhenEmailsAreNull_ShouldSetDefaults()
        {
            // Arrange
            var config = new CaddyConfig { AdminEmail = null, CertificateEmail = null };

            // Act
            config.SetDefaultValues();

            // Assert
            config.AdminEmail.Should().Be("admin@localhost");
            config.CertificateEmail.Should().Be("admin@localhost");
        }

        /// <summary>
        /// Verifies that the GenerateCaddyfileGlobals method includes the metrics directive when metrics are enabled.
        /// </summary>
        [Fact]
        public void GenerateCaddyfileGlobals_ShouldContainMetricsWhenEnabled()
        {
            // Arrange
            var config = new CaddyConfig { EnableMetrics = true, AdminPort = 2019, HttpPort = 80, HttpsPort = 443 };

            // Act
            var result = config.GenerateCaddyfileGlobals();

            // Assert
            result.Should().Contain("metrics");
        }

        /// <summary>
        /// Verifies that the GenerateCaddyfileGlobals method includes the auto_https off directive when auto_https is disabled.
        /// </summary>
        [Fact]
        public void GenerateCaddyfileGlobals_WhenAutoHttpsDisabled_ShouldContainDirective()
        {
            // Arrange
            var config = new CaddyConfig { AutoHttpsDisabled = true, AdminPort = 2019, HttpPort = 80, HttpsPort = 443 };

            // Act
            var result = config.GenerateCaddyfileGlobals();

            // Assert
            result.Should().Contain("auto_https off");
        }
    }
}
