// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Read-only query extension methods for <see cref="UpstreamPool"/>.
    /// </summary>
    public static class UpstreamPoolQueryExtensions
    {
        /// <summary>
        /// Counts the number of healthy servers in the pool (servers whose
        /// <see cref="UpstreamServer.IsHealthy"/> flag is <c>true</c>).
        /// </summary>
        /// <param name="pool">The upstream pool instance.</param>
        /// <returns>The count of healthy servers.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pool"/> is null.</exception>
        public static int CountHealthyServers(this UpstreamPool pool)
        {
            ArgumentNullException.ThrowIfNull(pool);
            return pool.Servers.Count(s => s.IsHealthy);
        }

        /// <summary>
        /// Counts the number of unhealthy servers in the pool (servers whose
        /// <see cref="UpstreamServer.IsHealthy"/> flag is <c>false</c>).
        /// </summary>
        /// <param name="pool">The upstream pool instance.</param>
        /// <returns>The count of unhealthy servers.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pool"/> is null.</exception>
        public static int CountUnhealthyServers(this UpstreamPool pool)
        {
            ArgumentNullException.ThrowIfNull(pool);
            return pool.Servers.Count(s => !s.IsHealthy);
        }

        /// <summary>
        /// Computes the health ratio of the pool as a value between 0.0 and 1.0.
        /// Returns 0.0 when the pool has no servers.
        /// </summary>
        /// <param name="pool">The upstream pool instance.</param>
        /// <returns>
        /// A <see cref="double"/> in the range [0.0, 1.0] representing the fraction
        /// of servers that are healthy.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pool"/> is null.</exception>
        public static double GetHealthRatio(this UpstreamPool pool)
        {
            ArgumentNullException.ThrowIfNull(pool);

            if (pool.Servers.Count == 0)
                return 0.0;

            return (double)pool.CountHealthyServers() / pool.Servers.Count;
        }
    }
}
