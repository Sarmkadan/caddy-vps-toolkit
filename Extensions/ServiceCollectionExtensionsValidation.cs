#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Extensions
{
    /// <summary>
    /// Provides validation extension methods for <see cref="InfrastructureOptions"/> to ensure configuration values are valid.
    /// </summary>
    public static class ServiceCollectionExtensionsValidation
    {
        /// <summary>
        /// Validates the <see cref="InfrastructureOptions"/> instance and returns a list of human-readable problems.
        /// </summary>
        /// <param name="value">The <see cref="InfrastructureOptions"/> instance to validate.</param>
        /// <returns>An immutable list of validation problems (empty if valid).</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this InfrastructureOptions value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // Validate HttpTimeoutMs (should be positive)
            if (value.HttpTimeoutMs <= 0)
            {
                problems.Add($"HttpTimeoutMs must be positive, but was {value.HttpTimeoutMs}.");
            }

            // Validate MaxRetries (should be non-negative)
            if (value.MaxRetries < 0)
            {
                problems.Add($"MaxRetries must be non-negative, but was {value.MaxRetries}.");
            }

            // Validate LogPath (should not be null or whitespace)
            if (string.IsNullOrWhiteSpace(value.LogPath))
            {
                problems.Add("LogPath must not be null or whitespace.");
            }
            else if (value.LogPath.Length > 1024)
            {
                problems.Add("LogPath must not exceed 1024 characters.");
            }

            // MinLogLevel is an enum - no validation needed as it's always valid
            // Validate RateLimitCapacity (should be positive)
            if (value.RateLimitCapacity <= 0)
            {
                problems.Add($"RateLimitCapacity must be positive, but was {value.RateLimitCapacity}.");
            }

            // Validate RateLimitRefillRate (should be positive)
            if (value.RateLimitRefillRate <= 0)
            {
                problems.Add($"RateLimitRefillRate must be positive, but was {value.RateLimitRefillRate}.");
            }

            return problems;
        }

        /// <summary>
        /// Determines whether the <see cref="InfrastructureOptions"/> instance is valid.
        /// </summary>
        /// <param name="value">The <see cref="InfrastructureOptions"/> instance to check.</param>
        /// <returns>True if valid; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static bool IsValid(this InfrastructureOptions value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return value.Validate().Count == 0;
        }

        /// <summary>
        /// Validates the <see cref="InfrastructureOptions"/> instance and throws an <see cref="ArgumentException"/> if invalid.
        /// </summary>
        /// <param name="value">The <see cref="InfrastructureOptions"/> instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if validation fails, containing a list of problems.</exception>
        public static void EnsureValid(this InfrastructureOptions value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();
            if (problems.Count > 0)
            {
                throw new ArgumentException(
                    $"InfrastructureOptions validation failed:{Environment.NewLine}- {
                    string.Join($"{Environment.NewLine}- ", problems)}");
            }
        }
    }
}