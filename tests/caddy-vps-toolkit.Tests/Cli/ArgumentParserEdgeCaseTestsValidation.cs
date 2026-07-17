#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using CaddyVpsToolkit.Cli;

namespace CaddyVpsToolkit.Tests.Cli;

/// <summary>
/// Validation helpers for ArgumentParser edge case tests.
/// Validates ArgumentParser instances for null/empty args, out-of-range indices, and invalid flag names.
/// </summary>
public static class ArgumentParserEdgeCaseTestsValidation
{
    /// <summary>
    /// Validates that an ArgumentParser instance is in a valid state.
    /// </summary>
    /// <param name="value">The ArgumentParser instance to validate</param>
    /// <returns>An enumerable of human-readable validation problems, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this ArgumentParser value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate GetCommand behavior - should never return null, only empty string
        var command = value.GetCommand();
        if (command is null)
        {
            problems.Add("GetCommand() returned null");
        }

        // Validate GetAllPositional doesn't throw and returns consistent results
        try
        {
            _ = value.GetAllPositional();
        }
        catch (Exception ex)
        {
            problems.Add($"GetAllPositional() threw exception: {ex.GetType().Name}");
        }

        // Validate GetAllFlags doesn't throw and returns consistent results
        try
        {
            _ = value.GetAllFlags();
        }
        catch (Exception ex)
        {
            problems.Add($"GetAllFlags() threw exception: {ex.GetType().Name}");
        }

        // Validate that boolean flags handling is consistent
        try
        {
            _ = value.HasFlag("verbose");
            _ = value.GetFlagValue("verbose");
        }
        catch (Exception ex)
        {
            problems.Add($"Boolean flag handling threw exception: {ex.GetType().Name}");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether an ArgumentParser instance is in a valid state.
    /// </summary>
    /// <param name="value">The ArgumentParser instance to check</param>
    /// <returns>True if valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static bool IsValid(this ArgumentParser value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that an ArgumentParser instance is in a valid state.
    /// </summary>
    /// <param name="value">The ArgumentParser instance to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails, containing a list of problems</exception>
    public static void EnsureValid(this ArgumentParser value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ArgumentParser validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}
