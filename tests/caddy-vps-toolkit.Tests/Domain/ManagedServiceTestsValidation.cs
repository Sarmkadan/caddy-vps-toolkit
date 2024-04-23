#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Domain.Models;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace CaddyVpsToolkit.Tests.Domain;

/// <summary>
/// Validation helpers for ManagedService to support edge-case testing.
/// </summary>
public static class ManagedServiceTestsValidation
{
    /// <summary>
    /// Validates a ManagedService instance and returns any validation problems.
    /// </summary>
    /// <param name="value">The service to validate</param>
    /// <returns>A list of human-readable validation problems, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static IReadOnlyList<string> Validate(this ManagedService value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate required string properties
        if (string.IsNullOrWhiteSpace(value.Name))
        {
            problems.Add("Service name is required");
        }
        else if (value.Name.Length < 3 || value.Name.Length > 255)
        {
            problems.Add("Service name must be between 3 and 255 characters");
        }

        if (string.IsNullOrWhiteSpace(value.ExecutablePath))
        {
            problems.Add("Executable path is required");
        }

        if (string.IsNullOrWhiteSpace(value.WorkingDirectory))
        {
            problems.Add("Working directory is required");
        }

        // Validate port range
        if (value.Port <= 0 || value.Port > 65535)
        {
            problems.Add("Port must be between 1 and 65535");
        }

        // Validate default dates
        if (value.CreatedAt == default)
        {
            problems.Add("CreatedAt must be set to a non-default DateTime");
        }

        if (value.UpdatedAt == default)
        {
            problems.Add("UpdatedAt must be set to a non-default DateTime");
        }

        // Validate Status is not default
        if (value.Status == default)
        {
            problems.Add("Status must be set to a valid ServiceStatus value");
        }

        // Validate non-null collections
        if (value.ExposedPorts is null)
        {
            problems.Add("ExposedPorts collection must not be null");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a ManagedService instance is valid.
    /// </summary>
    /// <param name="value">The service to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(this ManagedService value) => Validate(value).Count == 0;

    /// <summary>
    /// Ensures that a ManagedService instance is valid, throwing an exception with details if not.
    /// </summary>
    /// <param name="value">The service to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    /// <exception cref="ValidationException">Thrown with validation problems if invalid</exception>
    public static void EnsureValid(this ManagedService value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ValidationException(
                $"ManagedService validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}
