#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Domain.Models;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace CaddyVpsToolkit.Tests.Domain;

/// <summary>
/// Validation helpers for <see cref="ManagedService"/> to support edge-case testing.
/// Provides comprehensive validation beyond the basic validation in the domain model.
/// </summary>
public sealed class ManagedServiceTestsValidation
{
    /// <summary>
    /// Validates a ManagedService instance and returns any validation problems.
    /// Performs comprehensive validation including optional properties and collection items.
    /// </summary>
    /// <param name="value">The service to validate</param>
    /// <returns>A list of human-readable validation problems, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public IReadOnlyList<string> Validate(ManagedService value)
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

        if (string.IsNullOrWhiteSpace(value.Description))
        {
            problems.Add("Service description is required");
        }

        if (string.IsNullOrWhiteSpace(value.ExecutablePath))
        {
            problems.Add("Executable path is required");
        }

        if (string.IsNullOrWhiteSpace(value.WorkingDirectory))
        {
            problems.Add("Working directory is required");
        }

        if (string.IsNullOrWhiteSpace(value.HostBinding))
        {
            problems.Add("Host binding is required");
        }

        // Validate port range
        if (value.Port <= 0 || value.Port > 65535)
        {
            problems.Add("Port must be between 1 and 65535");
        }

        // Validate optional string properties
        if (!string.IsNullOrWhiteSpace(value.SystemdUnitName) && value.SystemdUnitName.Length > 255)
        {
            problems.Add("Systemd unit name must be 255 characters or less");
        }

        if (!string.IsNullOrWhiteSpace(value.Arguments) && value.Arguments.Length > 4096)
        {
            problems.Add("Arguments must be 4096 characters or less");
        }

        if (!string.IsNullOrWhiteSpace(value.EnvironmentVariables) && value.EnvironmentVariables.Length > 4096)
        {
            problems.Add("Environment variables must be 4096 characters or less");
        }

        // Validate collections
        if (value.ExposedPorts is null)
        {
            problems.Add("ExposedPorts collection must not be null");
        }
        else
        {
            for (var i = 0; i < value.ExposedPorts.Count; i++)
            {
                try
                {
                    value.ExposedPorts[i].Validate();
                }
                catch (ValidationException ex)
                {
                    problems.Add($"ExposedPorts[{i}]: {ex.Message}");
                }
            }
        }

        // Validate HealthCheck if present
        if (value.HealthCheck is not null)
        {
            try
            {
                value.HealthCheck.Validate();
            }
            catch (ValidationException ex)
            {
                problems.Add($"HealthCheck: {ex.Message}");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a ManagedService instance is valid.
    /// </summary>
    /// <param name="value">The service to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid(ManagedService value) => Validate(value).Count == 0;

    /// <summary>
    /// Ensures that a ManagedService instance is valid, throwing an exception with details if not.
    /// </summary>
    /// <param name="value">The service to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    /// <exception cref="ValidationException">Thrown with validation problems if invalid</exception>
    public void EnsureValid(ManagedService value)
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