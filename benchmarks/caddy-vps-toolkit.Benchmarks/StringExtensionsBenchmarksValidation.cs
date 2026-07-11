using System;
using System.Collections.Generic;
using System.Linq;

namespace CaddyVpsToolkit.Benchmarks
{
    /// <summary>
    /// Validation helpers for <see cref="StringExtensionsBenchmarks"/>.
    /// </summary>
    public static class StringExtensionsBenchmarksValidation
    {
        /// <summary>
        /// Returns a list of human‑readable validation problems for the supplied <see cref="StringExtensionsBenchmarks"/> instance.
        /// </summary>
        /// <param name="value">The benchmark instance to validate.</param>
        /// <returns>A read‑only list of problem descriptions. Empty if the instance is valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        public static IReadOnlyList<string> Validate(this StringExtensionsBenchmarks value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // String members – must not be null or empty.
            if (string.IsNullOrEmpty(value.ToKebabCase()))
            {
                problems.Add($"{nameof(value.ToKebabCase)}() is null or empty.");
            }

            if (string.IsNullOrEmpty(value.ToCamelCase()))
            {
                problems.Add($"{nameof(value.ToCamelCase)}() is null or empty.");
            }

            if (string.IsNullOrEmpty(value.Truncate()))
            {
                problems.Add($"{nameof(value.Truncate)}() is null or empty.");
            }

            // Boolean members – the benchmarks are expected to return true for the "match" cases.
            if (!value.IsNumeric_Digits())
            {
                problems.Add($"{nameof(value.IsNumeric_Digits)}() is false.");
            }

            if (!value.IsNumeric_NonDigits())
            {
                problems.Add($"{nameof(value.IsNumeric_NonDigits)}() is false.");
            }

            if (!value.StartsWithAny_Match())
            {
                problems.Add($"{nameof(value.StartsWithAny_Match)}() is false.");
            }

            if (!value.StartsWithAny_NoMatch())
            {
                problems.Add($"{nameof(value.StartsWithAny_NoMatch)}() is false.");
            }

            return problems;
        }

        /// <summary>
        /// Determines whether the supplied <see cref="StringExtensionsBenchmarks"/> instance is valid.
        /// </summary>
        /// <param name="value">The benchmark instance to check.</param>
        /// <returns><c>true</c> if no validation problems were found; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        public static bool IsValid(this StringExtensionsBenchmarks value) =>
            !value.Validate().Any();

        /// <summary>
        /// Ensures that the supplied <see cref="StringExtensionsBenchmarks"/> instance is valid.
        /// </summary>
        /// <param name="value">The benchmark instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when one or more validation problems are detected.</exception>
        public static void EnsureValid(this StringExtensionsBenchmarks value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();
            if (problems.Count > 0)
            {
                var message = $"StringExtensionsBenchmarks validation failed: {string.Join("; ", problems)}";
                throw new ArgumentException(message, nameof(value));
            }
        }
    }
}