#nullable enable
using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Core;
using System;
using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Domain.Models;
using FluentAssertions;
using Xunit;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace CaddyVpsToolkit.Tests.Domain;
/// <summary>
/// Edge-case tests for HealthCheckConfig validation and URL generation boundaries.
/// </summary>
public sealed class HealthCheckConfigEdgeCaseTests
{
    [Fact]
    public void Validate_IntervalBelowMinimum_ThrowsValidationException()
    {
        var config = new HealthCheckConfig { IntervalSeconds = 4, TimeoutSeconds = 2 };

        var act = () => config.Validate();

        act.Should().Throw<ValidationException>().WithMessage("*interval*at least 5*");
    }

    [Fact]
    public void Validate_IntervalExactlyMinimum_DoesNotThrow()
    {
        var config = new HealthCheckConfig { IntervalSeconds = 5, TimeoutSeconds = 1 };

        var act = () => config.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_TimeoutGreaterThanInterval_ThrowsValidationException()
    {
        var config = new HealthCheckConfig { IntervalSeconds = 10, TimeoutSeconds = 15 };

        var act = () => config.Validate();

        act.Should().Throw<ValidationException>().WithMessage("*Timeout*greater than interval*");
    }

    [Fact]
    public void Validate_TimeoutEqualsInterval_DoesNotThrow()
    {
        var config = new HealthCheckConfig { IntervalSeconds = 10, TimeoutSeconds = 10 };

        var act = () => config.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroTimeout_ThrowsValidationException()
    {
        var config = new HealthCheckConfig { IntervalSeconds = 10, TimeoutSeconds = 0 };

        var act = () => config.Validate();

        act.Should().Throw<ValidationException>().WithMessage("*Timeout*at least 1*");
    }

    [Fact]
    public void Validate_ZeroUnhealthyThreshold_ThrowsValidationException()
    {
        var config = new HealthCheckConfig
        {
            IntervalSeconds = 10,
            TimeoutSeconds = 5,
            UnhealthyThreshold = 0
        };

        var act = () => config.Validate();

        act.Should().Throw<ValidationException>().WithMessage("*Unhealthy threshold*");
    }

    [Fact]
    public void Validate_ZeroHealthyThreshold_ThrowsValidationException()
    {
        var config = new HealthCheckConfig
        {
            IntervalSeconds = 10,
            TimeoutSeconds = 5,
            HealthyThreshold = 0
        };

        var act = () => config.Validate();

        act.Should().Throw<ValidationException>().WithMessage("*Healthy threshold*");
    }

    [Fact]
    public void Validate_HttpTypeWithoutEndpoint_ThrowsValidationException()
    {
        var config = new HealthCheckConfig
        {
            Type = HealthCheckType.Http,
            IntervalSeconds = 10,
            TimeoutSeconds = 5,
            Endpoint = null
        };

        var act = () => config.Validate();

        act.Should().Throw<ValidationException>().WithMessage("*HTTP*endpoint*");
    }

    [Fact]
    public void GetHealthCheckUrl_HttpType_ReturnsFormattedUrl()
    {
        var config = new HealthCheckConfig
        {
            Type = HealthCheckType.Http,
            Endpoint = "/health"
        };

        var url = config.GetHealthCheckUrl("localhost", 5000);

        url.Should().Be("http://localhost:5000/health");
    }

    [Fact]
    public void GetHealthCheckUrl_NonHttpType_ReturnsNull()
    {
        var config = new HealthCheckConfig { Type = HealthCheckType.Tcp };

        var url = config.GetHealthCheckUrl("localhost", 5000);

        url.Should().BeNull();
    }
}
