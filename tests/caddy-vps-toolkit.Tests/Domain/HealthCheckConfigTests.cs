#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for the HealthCheckConfig class.
/// </summary>
namespace CaddyVpsToolkit.Tests.Domain
{
    public sealed class HealthCheckConfigTests
    {
        /// <summary>
        /// Verifies that a valid HealthCheckConfig instance does not throw an exception when validated.
        /// </summary>
        [Fact]
        public void Validate_WithValidData_ShouldNotThrow()
        {
            // Arrange
            var config = new HealthCheckConfig
            {
                IntervalSeconds = 30,
                TimeoutSeconds = 5,
                UnhealthyThreshold = 3,
                HealthyThreshold = 2,
                Type = HealthCheckType.Http,
                Endpoint = "/health"
            };

            // Act
            var act = () => config.Validate();

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// Verifies that a HealthCheckConfig instance with an interval less than 5 seconds throws a ValidationException.
        /// </summary>
        /// <param name="config">The HealthCheckConfig instance to validate.</param>
        [Fact]
        public void Validate_WithIntervalLessThan5_ShouldThrowValidationException()
        {
            // Arrange
            var config = new HealthCheckConfig { IntervalSeconds = 4, TimeoutSeconds = 2 };

            // Act
            var act = () => config.Validate();

            // Assert
            act.Should().Throw<System.ComponentModel.DataAnnotations.ValidationException>().WithMessage("*interval must be at least 5 seconds*");
        }

        /// <summary>
        /// Verifies that a HealthCheckConfig instance with a timeout greater than the interval throws a ValidationException.
        /// </summary>
        /// <param name="config">The HealthCheckConfig instance to validate.</param>
        [Fact]
        public void Validate_WithTimeoutGreaterThanInterval_ShouldThrowValidationException()
        {
            // Arrange
            var config = new HealthCheckConfig { IntervalSeconds = 10, TimeoutSeconds = 15 };

            // Act
            var act = () => config.Validate();

            // Assert
            act.Should().Throw<System.ComponentModel.DataAnnotations.ValidationException>().WithMessage("*Timeout cannot be greater than interval*");
        }

        /// <summary>
        /// Verifies that a HealthCheckConfig instance with a missing endpoint for HTTP throws a ValidationException.
        /// </summary>
        /// <param name="config">The HealthCheckConfig instance to validate.</param>
        [Fact]
        public void Validate_WithMissingEndpointForHttp_ShouldThrowValidationException()
        {
            // Arrange
            var config = new HealthCheckConfig 
            { 
                IntervalSeconds = 30, 
                TimeoutSeconds = 5, 
                Type = HealthCheckType.Http,
                Endpoint = string.Empty
            };

            // Act
            var act = () => config.Validate();

            // Assert
            act.Should().Throw<System.ComponentModel.DataAnnotations.ValidationException>().WithMessage("*HTTP health check requires an endpoint*");
        }

        /// <summary>
        /// Verifies that the GetHealthCheckUrl method constructs the correct URL for HTTP health checks.
        /// </summary>
        /// <param name="endpoint">The endpoint to use in the URL.</param>
        /// <param name="expected">The expected URL.</param>
        [Theory]
        [InlineData("/health", "http://localhost:8080/health")]
        [InlineData("api/status", "http://localhost:8080api/status")]
        public void GetHealthCheckUrl_WithHttpType_ShouldConstructCorrectUrl(string endpoint, string expected)
        {
            // Arrange
            var config = new HealthCheckConfig { Type = HealthCheckType.Http, Endpoint = endpoint };

            // Act
            var url = config.GetHealthCheckUrl("localhost", 8080);

            // Assert
            url.Should().Be(expected);
        }

        /// <summary>
        /// Verifies that the GetHealthCheckUrl method returns null for TCP health checks.
        /// </summary>
        /// <param name="config">The HealthCheckConfig instance to use.</param>
        [Fact]
        public void GetHealthCheckUrl_WithTcpType_ShouldReturnNull()
        {
            // Arrange
            var config = new HealthCheckConfig { Type = HealthCheckType.Tcp, Endpoint = "/health" };

            // Act
            var url = config.GetHealthCheckUrl("localhost", 8080);

            // Assert
            url.Should().BeNull();
        }
    }
}
