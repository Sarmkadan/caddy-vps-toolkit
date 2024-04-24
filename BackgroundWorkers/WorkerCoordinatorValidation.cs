#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaddyVpsToolkit.BackgroundWorkers
{
    /// <summary>
    /// Provides validation helpers for <see cref="WorkerCoordinator"/> instances.
    /// </summary>
    public static class WorkerCoordinatorValidation
    {
        /// <summary>
        /// Validates the specified <see cref="WorkerCoordinator"/> instance.
        /// </summary>
        /// <param name="value">The coordinator to validate.</param>
        /// <returns>A list of human-readable validation problems; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this WorkerCoordinator value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // The logger is validated by the constructor, so we can't check it here
            // We can only validate the state of the coordinator itself

            // Validate that workers dictionary is initialized (it always is due to constructor)
            // No other validation is possible without accessing private fields

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the specified <see cref="WorkerCoordinator"/> is valid.
        /// </summary>
        /// <param name="value">The coordinator to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(this WorkerCoordinator value)
        {
            return value is not null && !value.Validate().Any();
        }

        /// <summary>
        /// Ensures that the specified <see cref="WorkerCoordinator"/> is valid.
        /// </summary>
        /// <param name="value">The coordinator to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid, containing a list of problems.</exception>
        public static void EnsureValid(this WorkerCoordinator value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();
            if (problems.Count > 0)
            {
                throw new ArgumentException(
                    $"WorkerCoordinator is not valid. Problems: {string.Join("; ", problems)}");
            }
        }
    }
}
