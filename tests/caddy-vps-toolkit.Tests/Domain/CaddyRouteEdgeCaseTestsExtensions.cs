namespace CaddyVpsToolkit.Tests.Domain;

using System;
using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Domain.Models;
using FluentAssertions;

/// <summary>
/// Extension methods for <see cref="CaddyRouteEdgeCaseTests"/> that provide reusable test assertions
/// for edge cases in CaddyRoute validation and configuration.
/// </summary>
public static class CaddyRouteEdgeCaseTestsExtensions
{
    /// <summary>
    /// Asserts that a route with basic auth enabled but no username throws a validation exception.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
    public static void Validate_BasicAuthEnabledWithoutUsername_ThrowsValidationException(this CaddyRouteEdgeCaseTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);

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

    /// <summary>
    /// Asserts that a route with basic auth enabled but no password hash throws a validation exception.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
    public static void Validate_BasicAuthEnabledWithoutPasswordHash_ThrowsValidationException(this CaddyRouteEdgeCaseTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);

        var route = new CaddyRoute
        {
            Domain = "example.com",
            UpstreamUrl = "http://localhost:5000",
            BasicAuthEnabled = true,
            BasicAuthUsername = "testuser",
            BasicAuthPasswordHash = null!
        };

        var act = () => route.Validate();
        act.Should().Throw<ValidationException>().WithMessage("*password*");
    }
}