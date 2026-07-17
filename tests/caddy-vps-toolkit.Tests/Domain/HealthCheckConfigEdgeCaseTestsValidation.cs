#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace CaddyVpsToolkit.Tests.Domain;

/// <summary>
/// Validation helpers for HealthCheckConfig to support edge-case testing.
/// </summary>
public static class HealthCheckConfigEdgeCaseTestsValidation
{
    /// <summary>
    /// Validates a HealthCheckConfig instance and returns any validation problems.
    /// </summary>
    /// <param name="value">The configuration to validate</param>
    /// <returns>A list of human-readable validation problems, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this HealthCheckConfig value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (value.IntervalSeconds < 5)
        {
            problems.Add("Health check interval must be at least 5 seconds");
        }

        if (value.TimeoutSeconds > value.IntervalSeconds)
        {
            problems.Add("Timeout cannot be greater than interval");
        }

        if (value.TimeoutSeconds < 1)
        {
            problems.Add("Timeout must be at least 1 second");
        }

        if (value.UnhealthyThreshold < 1)
        {
            problems.Add("Unhealthy threshold must be at least 1");
        }

        if (value.HealthyThreshold < 1)
        {
            problems.Add("Healthy threshold must be at least 1");
        }

        if (value.Type == HealthCheckType.Http && string.IsNullOrWhiteSpace(value.Endpoint))
        {
            problems.Add("HTTP health check requires an endpoint");
        }

        if (string.IsNullOrWhiteSpace(value.ServiceId))
        {
            problems.Add("Service ID cannot be null or empty");
        }

        if (value.Type == HealthCheckType.Exec && string.IsNullOrWhiteSpace(value.ExpectedResponse))
        {
            problems.Add("Exec health check requires an expected response pattern");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a HealthCheckConfig instance is valid.
    /// </summary>
    /// <param name="value">The configuration to check</param>
    /// <returns>True if valid, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static bool IsValid(this HealthCheckConfig value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that a HealthCheckConfig instance is valid, throwing an exception with details if not.
    /// </summary>
    /// <param name="value">The configuration to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    /// <exception cref="ValidationException">Thrown with validation problems if invalid</exception>
    public static void EnsureValid(this HealthCheckConfig value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ValidationException(
                $"HealthCheckConfig validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}