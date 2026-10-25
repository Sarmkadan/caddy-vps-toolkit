// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using System;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Read-only query extension methods for <see cref="CaddyRoute"/>.
    /// </summary>
    public static class CaddyRouteQueryExtensions
    {
        /// <summary>
        /// Determines whether the route's <see cref="CaddyRoute.Domain"/> equals the given host name.
        /// The comparison is case-insensitive, as host names are case-insensitive per RFC 4343.
        /// </summary>
        /// <param name="route">The route instance.</param>
        /// <param name="host">The host name to test (e.g., "api.example.com").</param>
        /// <returns><c>true</c> if the route's domain matches <paramref name="host"/>; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="route"/> or <paramref name="host"/> is null.</exception>
        public static bool MatchesHost(this CaddyRoute route, string host)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(host);
            return string.Equals(route.Domain, host, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether automatic HTTPS is enabled for the route (<see cref="CaddyRoute.EnableHttps"/>)
        /// and the route is active, meaning Caddy will provision a TLS certificate for it.
        /// </summary>
        /// <param name="route">The route instance.</param>
        /// <returns><c>true</c> if the route is active and has HTTPS enabled; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="route"/> is null.</exception>
        public static bool IsTlsEnabled(this CaddyRoute route)
        {
            ArgumentNullException.ThrowIfNull(route);
            return route.IsActive && route.EnableHttps;
        }

        /// <summary>
        /// Determines whether the route uses a DNS-01 challenge for certificate issuance
        /// (<see cref="CaddyRoute.TlsDnsProvider"/> is set to a non-blank provider name),
        /// rather than the default HTTP-01 challenge.
        /// </summary>
        /// <param name="route">The route instance.</param>
        /// <returns><c>true</c> if a DNS challenge provider is configured; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="route"/> is null.</exception>
        public static bool UsesDnsChallenge(this CaddyRoute route)
        {
            ArgumentNullException.ThrowIfNull(route);
            return !string.IsNullOrWhiteSpace(route.TlsDnsProvider);
        }
    }
}
