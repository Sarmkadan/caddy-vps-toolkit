#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Utilities;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests
{
    public sealed class ValidationHelperTests
    {
        [Fact]
        public void ValidatePort_PortZero_ReturnsInvalidResult()
        {
            // Act
            var result = ValidationHelper.ValidatePort(0);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle()
                .Which.Should().Contain("Port must be between 1 and 65535");
        }

        [Fact]
        public void ValidateDomain_WellFormedDomain_ReturnsValidResult()
        {
            // Act
            var result = ValidationHelper.ValidateDomain("api.example.com");

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateServiceName_LessThanThreeChars_ReturnsError()
        {
            // Act
            var result = ValidationHelper.ValidateServiceName("ab");

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle()
                .Which.Should().Contain("at least 3 characters");
        }

        [Fact]
        public void Combine_TwoFailureResults_MergesAllErrorMessages()
        {
            // Arrange
            var portFailure = ValidationHelper.ValidatePort(0);
            var nameFailure = ValidationHelper.ValidateServiceName("x");

            // Act
            var combined = ValidationHelper.Combine(portFailure, nameFailure);

            // Assert
            combined.IsValid.Should().BeFalse();
            combined.Errors.Should().HaveCount(2);
        }

        [Fact]
        public void HealthCheckResult_CreateSuccess_SetsHealthyProperties()
        {
            // Act
            var result = HealthCheckResult.CreateSuccess("svc-42", responseTimeMs: 120, httpStatus: 200);

            // Assert
            result.IsHealthy.Should().BeTrue();
            result.Status.Should().Be(HealthCheckStatus.Healthy);
            result.ResponseTimeMs.Should().Be(120);
            result.HttpStatusCode.Should().Be(200);
            result.ServiceId.Should().Be("svc-42");
        }

        [Fact]
        public void HealthCheckResult_IsSlowResponse_ReturnsTrueOnlyAboveThreshold()
        {
            // Arrange — response time of 6 seconds
            var result = HealthCheckResult.CreateSuccess("svc-1", responseTimeMs: 6000);

            // Act & Assert
            result.IsSlowResponse(thresholdMs: 5000).Should().BeTrue();
            result.IsSlowResponse(thresholdMs: 7000).Should().BeFalse();
        }

        [Fact]
        public void ManagedService_GetSystemdUnitName_WithSpacesInName_FormatsCorrectly()
        {
            // Arrange
            var service = new ManagedService { Name = "My Web App" };

            // Act
            var unitName = service.GetSystemdUnitName();

            // Assert
            unitName.Should().Be("vps-my-web-app.service");
        }
    }
}
