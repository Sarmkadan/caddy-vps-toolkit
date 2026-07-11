#nullable enable
using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Tests.Cli
{
    /// <summary>
    /// Validation helpers for ArgumentValidatorTests.
    /// Validates ArgumentValidatorTests instances for null references and proper construction.
    /// </summary>
    public static class ArgumentValidatorTestsValidation
    {
        /// <summary>
        /// Validates that an ArgumentValidatorTests instance is in a valid state.
        /// </summary>
        /// <param name="value">The ArgumentValidatorTests instance to validate</param>
        /// <returns>An enumerable of human-readable validation problems, or empty if valid</returns>
        /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
        public static IReadOnlyList<string> Validate(this ArgumentValidatorTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return Array.Empty<string>();
        }

        /// <summary>
        /// Determines whether an ArgumentValidatorTests instance is in a valid state.
        /// </summary>
        /// <param name="value">The ArgumentValidatorTests instance to check</param>
        /// <returns>True if valid; otherwise, false</returns>
        public static bool IsValid(this ArgumentValidatorTests value)
        {
            return value.Validate().Count == 0;
        }

        /// <summary>
        /// Ensures that an ArgumentValidatorTests instance is in a valid state.
        /// </summary>
        /// <param name="value">The ArgumentValidatorTests instance to validate</param>
        /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
        /// <exception cref="ArgumentException">Thrown if validation fails, containing a list of problems</exception>
        public static void EnsureValid(this ArgumentValidatorTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();
            if (problems.Count > 0)
            {
                throw new ArgumentException(
                    $"ArgumentValidatorTests validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
            }
        }
    }
}