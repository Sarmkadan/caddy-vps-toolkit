#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Read-only query extension methods for <see cref="HealthCheckResult"/> and collections of it.
    /// </summary>
    public static class HealthCheckResultQueryExtensions
    {
        /// <summary>
        /// Determines whether the health check result indicates a failure, i.e. the check
        /// was not healthy or the reported status is <see cref="HealthCheckStatus.Unhealthy"/>.
        /// </summary>
        /// <param name="result">The health check result to inspect.</param>
        /// <returns><c>true</c> if the result indicates a failure; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
        public static bool IsFailure(this HealthCheckResult result)
        {
            ArgumentNullException.ThrowIfNull(result);
            return !result.IsHealthy || result.Status == HealthCheckStatus.Unhealthy;
        }

        /// <summary>
        /// Filters a sequence of health check results down to those with the given <see cref="HealthCheckStatus"/>.
        /// The sequence is not materialized; the filter is applied lazily.
        /// </summary>
        /// <param name="results">The health check results to filter.</param>
        /// <param name="status">The status to match.</param>
        /// <returns>A lazily evaluated sequence of results whose <see cref="HealthCheckResult.Status"/> equals <paramref name="status"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> is null.</exception>
        public static IEnumerable<HealthCheckResult> WithStatus(
            this IEnumerable<HealthCheckResult> results,
            HealthCheckStatus status)
        {
            ArgumentNullException.ThrowIfNull(results);
            return results.Where(r => r.Status == status);
        }

        /// <summary>
        /// Filters a sequence of health check results down to those that indicate a failure
        /// (see <see cref="IsFailure(HealthCheckResult)"/>). The filter is applied lazily.
        /// </summary>
        /// <param name="results">The health check results to filter.</param>
        /// <returns>A lazily evaluated sequence of failed results.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> is null.</exception>
        public static IEnumerable<HealthCheckResult> GetFailures(this IEnumerable<HealthCheckResult> results)
        {
            ArgumentNullException.ThrowIfNull(results);
            return results.Where(r => r.IsFailure());
        }
    }
}
