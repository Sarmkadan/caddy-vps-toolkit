// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// Extension methods for UpstreamServer
// =============================================================================

#nullable enable

using System;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Provides fluent extension methods for <see cref="UpstreamServer"/>.
    /// </summary>
    public static class UpstreamServerFluentExtensions
    {
        /// <summary>
        /// Returns the endpoint string in the format <c>address:port</c>.
        /// This is equivalent to <see cref="UpstreamServer.GetUpstreamAddress"/>.
        /// </summary>
        /// <param name="server">The upstream server instance.</param>
        /// <returns>The formatted endpoint string.</returns>
        public static string ToEndpointString(this UpstreamServer server) =>
            server.GetUpstreamAddress();

        /// <summary>
        /// Determines whether the upstream server is both healthy and enabled for traffic.
        /// A server is considered enabled when its <see cref="UpstreamServer.Status"/> is
        /// <see cref="UpstreamServerStatus.Active"/>. The method also checks the
        /// <see cref="UpstreamServer.IsHealthy"/> flag.
        /// </summary>
        /// <param name="server">The upstream server instance.</param>
        /// <returns>
        /// <c>true</c> if the server is healthy and its status is <c>Active</c>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool IsHealthyAndEnabled(this UpstreamServer server) =>
            server.IsHealthy && server.Status == UpstreamServerStatus.Active;

        /// <summary>
        /// Sets the weight of the upstream server and returns the same instance,
        /// enabling fluent configuration.
        /// </summary>
        /// <param name="server">The upstream server instance.</param>
        /// <param name="weight">The new weight value (must be between 1 and 100).</param>
        /// <returns>The same <see cref="UpstreamServer"/> instance with the updated weight.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="weight"/> is outside the allowed range (1‑100).
        /// </exception>
        public static UpstreamServer WithWeight(this UpstreamServer server, int weight)
        {
            if (weight < 1 || weight > 100)
                throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be between 1 and 100.");

            server.Weight = weight;
            server.UpdatedAt = DateTime.UtcNow;
            return server;
        }
    }
}
