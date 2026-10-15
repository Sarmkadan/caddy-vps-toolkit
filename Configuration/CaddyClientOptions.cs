#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace CaddyVpsToolkit.Configuration
{
    /// <summary>
    /// Settings for the Caddy admin API client. Bound from the <c>CaddyClient</c> section of
    /// configuration and validated at host startup by
    /// <see cref="CaddyVpsToolkit.DependencyInjection.CaddyVpsToolkitServiceCollectionExtensions.AddCaddyVpsToolkit"/>.
    /// </summary>
    public sealed class CaddyClientOptions
    {
        /// <summary>Configuration section name this class binds to.</summary>
        public const string SectionName = "CaddyClient";

        /// <summary>
        /// Base URI of the Caddy admin API. Must be an absolute <c>http</c> or <c>https</c> URI.
        /// </summary>
        public string AdminEndpoint { get; set; } = "http://localhost:2019";

        /// <summary>
        /// Timeout for a single admin API request. Must be greater than zero and at most 5 minutes.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
