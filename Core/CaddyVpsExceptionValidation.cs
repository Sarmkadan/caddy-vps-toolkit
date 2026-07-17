using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Core;

/// <summary>
/// Provides validation helpers for <see cref="CaddyVpsException"/> instances.
/// </summary>
public static class CaddyVpsExceptionValidation
{
    /// <summary>
    /// Validates the specified <see cref="CaddyVpsException"/> instance.
    /// </summary>
    /// <param name="value">The exception to validate.</param>
    /// <returns>A list of validation problems; empty if the exception is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this CaddyVpsException value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(value.ErrorCode))
        {
            problems.Add("ErrorCode must not be null or whitespace.");
        }

        if (value.Details is null)
        {
            problems.Add("Details must not be null.");
        }

        return problems;
    }

    /// <summary>
    /// Determines whether the specified <see cref="CaddyVpsException"/> instance is valid.
    /// </summary>
    /// <param name="value">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this CaddyVpsException value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="CaddyVpsException"/> instance is valid, throwing an <see cref="ArgumentException"/> if it is not.
    /// </summary>
    /// <param name="value">The exception to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the exception is invalid, containing a list of validation problems.</exception>
    public static void EnsureValid(this CaddyVpsException value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"The CaddyVpsException instance is invalid. Problems:{Environment.NewLine}" + string.Join(Environment.NewLine, problems),
                nameof(value));
        }
    }
}