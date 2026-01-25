#nullable enable
using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Domain;

/// <summary>
/// Edge-case tests for CaddyRoute validation, path matching, and configuration boundaries.
/// </summary>
public sealed class CaddyRouteEdgeCaseTests
{
    [Fact]
    public void Validate_NullDomain_ThrowsValidationException()
    {
        var route = new CaddyRoute { Domain = null!, UpstreamUrl = "http://localhost:5000" };

        var act = () => route.Validate();

        act.Should().Throw<ValidationException>().WithMessage("*Domain*");
    }

    [Fact]
    public void Validate_EmptyDomain_ThrowsValidationException()
    {
        var route = new CaddyRoute { Domain = "", UpstreamUrl = "http://localhost:5000" };

        var act = () => route.Validate();

        act.Should().Throw<ValidationException>().WithMessage("*Domain*");
    }

    [Fact]
    public void Validate_NullUpstreamUrl_ThrowsValidationException()
    {
        var route = new CaddyRoute { Domain = "example.com", UpstreamUrl = null! };

        var act = () => route.Validate();

        act.Should().Throw<ValidationException>().WithMessage("*Upstream*");
    }

    [Fact]
    public void Validate_InvalidUpstreamUrl_ThrowsValidationException()
    {
        var route = new CaddyRoute { Domain = "example.com", UpstreamUrl = "not-a-url" };

        var act = () => route.Validate();

        act.Should().Throw<ValidationException>().WithMessage("*Invalid upstream*");
    }

    [Fact]
    public void Validate_ZeroTimeout_ThrowsValidationException()
    {
        var route = new CaddyRoute
        {
            Domain = "example.com",
            UpstreamUrl = "http://localhost:5000",
            TimeoutSeconds = 0
        };

        var act = () => route.Validate();

        act.Should().Throw<ValidationException>().WithMessage("*Timeout*");
    }

    [Fact]
    public void Validate_NegativeTimeout_ThrowsValidationException()
    {
        var route = new CaddyRoute
        {
            Domain = "example.com",
            UpstreamUrl = "http://localhost:5000",
            TimeoutSeconds = -1
        };

        var act = () => route.Validate();

        act.Should().Throw<ValidationException>().WithMessage("*Timeout*");
    }

    [Fact]
    public void Validate_BasicAuthEnabledWithoutUsername_ThrowsValidationException()
    {
        var route = new CaddyRoute
        {
            Domain = "example.com",
            UpstreamUrl = "http://localhost:5000",
            BasicAuthEnabled = true,
            BasicAuthUsername = null!
        };

        var act = () => route.Validate();

        act.Should().Throw<ValidationException>().WithMessage("*Basic auth*username*");
    }

    [Fact]
    public void Validate_ValidRoute_DoesNotThrow()
    {
        var route = new CaddyRoute
        {
            Domain = "example.com",
            UpstreamUrl = "http://localhost:5000",
            TimeoutSeconds = 30
        };

        var act = () => route.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void GetCaddyPathMatcher_NullPath_ReturnsEmpty()
    {
        var route = new CaddyRoute { Path = null! };

        route.GetCaddyPathMatcher().Should().BeEmpty();
    }

    [Fact]
    public void GetCaddyPathMatcher_SlashPath_ReturnsEmpty()
    {
        var route = new CaddyRoute { Path = "/" };

        route.GetCaddyPathMatcher().Should().BeEmpty();
    }

    [Fact]
    public void GetCaddyPathMatcher_CustomPath_ReturnsPath()
    {
        var route = new CaddyRoute { Path = "/api/v1" };

        route.GetCaddyPathMatcher().Should().Be("/api/v1");
    }

    [Fact]
    public void GenerateRoutePath_WithCustomPath_ConcatenatesDomainAndPath()
    {
        var route = new CaddyRoute { Domain = "example.com", Path = "/api" };

        route.GenerateRoutePath().Should().Be("example.com/api");
    }

    [Fact]
    public void GenerateRoutePath_WithSlashPath_ReturnsDomainOnly()
    {
        var route = new CaddyRoute { Domain = "example.com", Path = "/" };

        route.GenerateRoutePath().Should().Be("example.com");
    }
}
