#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests
{
    /// <summary>
    /// Unit tests for the public API of <see cref="CaddyRoute"/>.
    /// Covers defaults, validation boundaries, error paths and matcher/path helpers.
    /// </summary>
    [Trait("Category", "Unit")]
    public sealed class CaddyRouteUnitTests
    {
        private static CaddyRoute CreateValidRoute() => new()
        {
            Domain = "api.example.com",
            UpstreamUrl = "http://localhost:5000"
        };

        [Fact]
        public void NewRoute_HasExpectedDefaults()
        {
            // Act
            var route = new CaddyRoute();

            // Assert
            route.Path.Should().Be("/");
            route.TimeoutSeconds.Should().Be(30);
            route.EnableHttps.Should().BeTrue();
            route.AutoRedirectHttp.Should().BeTrue();
            route.IsActive.Should().BeTrue();
            route.StripPath.Should().BeFalse();
            route.BasicAuthEnabled.Should().BeFalse();
            route.CustomHeaders.Should().NotBeNull().And.BeEmpty();
            route.Domain.Should().BeNull();
        }

        [Fact]
        public void NewRoute_GeneratesUniqueGuidId()
        {
            // Act
            var first = new CaddyRoute();
            var second = new CaddyRoute();

            // Assert
            Guid.TryParse(first.Id, out _).Should().BeTrue();
            first.Id.Should().NotBe(second.Id);
        }

        [Fact]
        public void Validate_ValidRoute_DoesNotThrow()
        {
            // Arrange
            var route = CreateValidRoute();

            // Act
            var act = () => route.Validate();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_TimeoutOfExactlyOneSecond_IsAccepted()
        {
            // Arrange: lower boundary of the allowed range
            var route = CreateValidRoute();
            route.TimeoutSeconds = 1;

            // Act
            var act = () => route.Validate();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_WhitespaceDomain_ThrowsDomainRequired()
        {
            // Arrange
            var route = CreateValidRoute();
            route.Domain = "   ";

            // Act
            var act = () => route.Validate();

            // Assert
            act.Should().Throw<ValidationException>().WithMessage("Domain is required");
        }

        [Fact]
        public void Validate_MissingDomainAndUpstream_ReportsDomainFirst()
        {
            // Arrange: both required fields are empty; domain is checked first
            var route = new CaddyRoute { Domain = null, UpstreamUrl = null };

            // Act
            var act = () => route.Validate();

            // Assert
            act.Should().Throw<ValidationException>().WithMessage("Domain is required");
        }

        [Fact]
        public void Validate_WhitespaceUpstreamUrl_ThrowsUpstreamRequired()
        {
            // Arrange
            var route = CreateValidRoute();
            route.UpstreamUrl = "\t ";

            // Act
            var act = () => route.Validate();

            // Assert
            act.Should().Throw<ValidationException>().WithMessage("Upstream URL is required");
        }

        [Fact]
        public void Validate_RelativeUpstreamUrl_ThrowsInvalidUpstream()
        {
            // Arrange: free text with spaces and no scheme is not an absolute URI on any platform
            // (note: a bare "/path" is accepted on Linux as a file URI, so it is not used here)
            var route = CreateValidRoute();
            route.UpstreamUrl = "not a valid url";

            // Act
            var act = () => route.Validate();

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("Invalid upstream URL: not a valid url");
        }

        [Fact]
        public void Validate_ZeroTimeout_ThrowsTimeoutError()
        {
            // Arrange: one below the lower boundary
            var route = CreateValidRoute();
            route.TimeoutSeconds = 0;

            // Act
            var act = () => route.Validate();

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("Timeout must be at least 1 second");
        }

        [Fact]
        public void Validate_BasicAuthEnabledWithWhitespaceUsername_ThrowsUsernameRequired()
        {
            // Arrange
            var route = CreateValidRoute();
            route.BasicAuthEnabled = true;
            route.BasicAuthUsername = " ";

            // Act
            var act = () => route.Validate();

            // Assert
            act.Should().Throw<ValidationException>().WithMessage("Basic auth requires username");
        }

        [Fact]
        public void Validate_BasicAuthDisabledWithoutUsername_DoesNotThrow()
        {
            // Arrange: username is only mandatory when basic auth is enabled
            var route = CreateValidRoute();
            route.BasicAuthEnabled = false;
            route.BasicAuthUsername = null;

            // Act
            var act = () => route.Validate();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GetCaddyPathMatcher_WhitespacePath_ReturnsEmpty()
        {
            // Arrange
            var route = CreateValidRoute();
            route.Path = "   ";

            // Act & Assert
            route.GetCaddyPathMatcher().Should().BeEmpty();
        }

        [Fact]
        public void GetCaddyMatcherName_WithServiceId_ReplacesHyphensWithUnderscores()
        {
            // Arrange
            var route = CreateValidRoute();
            route.ServiceId = "my-api-service";

            // Act & Assert
            route.GetCaddyMatcherName().Should().Be("my_api_service");
        }

        [Fact]
        public void GetCaddyMatcherName_WithoutServiceId_FallsBackToRouteId()
        {
            // Arrange: whitespace ServiceId counts as missing
            var route = CreateValidRoute();
            route.Id = "route-42-abc";
            route.ServiceId = "  ";

            // Act & Assert
            route.GetCaddyMatcherName().Should().Be("route_42_abc");
        }
    }
}
