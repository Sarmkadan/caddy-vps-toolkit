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

            var workerNames = value.GetWorkerNames();
            if (workerNames.Count == 0)
            {
                problems.Add("WorkerCoordinator has no registered workers");
            }

            var runningWorkerCount = workerNames.Count(name => value.IsWorkerRunning(name));
            if (runningWorkerCount > 0 && runningWorkerCount == workerNames.Count)
            {
                problems.Add("All workers are running, which may indicate they were not properly stopped");
            }

            var invalidWorkerNames = workerNames
                .Where(name => string.IsNullOrWhiteSpace(name))
                .Select(name => $"Worker with empty name");
            problems.AddRange(invalidWorkerNames);

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the specified <see cref="WorkerCoordinator"/> is valid.
        /// </summary>
        /// <param name="value">The coordinator to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(this WorkerCoordinator value) => value?.Validate().Count == 0;

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
