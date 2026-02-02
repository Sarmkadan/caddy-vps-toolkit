#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Represents a Caddy reverse proxy route configuration. Each route maps a domain (and optional path)
    /// to an upstream service URL. Supports HTTPS auto-provisioning, HTTP-to-HTTPS redirect,
    /// custom headers, rate limiting, and basic authentication.
    /// </summary>
    public sealed class CaddyRoute
    {
        /// <summary>Unique route identifier, auto-generated as a GUID.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>ID of the <see cref="ManagedService"/> this route points to.</summary>
        public string ServiceId { get; set; }

        /// <summary>Domain name for the Caddyfile site block (e.g., "api.example.com").</summary>
        [Required]
        public string Domain { get; set; }

        /// <summary>URL path prefix to match. Defaults to "/" (all paths).</summary>
        public string Path { get; set; } = "/";

        /// <summary>Upstream service URL to proxy to (e.g., "http://localhost:5000").</summary>
        public string UpstreamUrl { get; set; }

        /// <summary>Whether to strip the matched path prefix before forwarding to upstream.</summary>
        public bool StripPath { get; set; }

        /// <summary>Whether to pass the original Host header to the upstream server.</summary>
        public bool PreserveHostHeader { get; set; }

        /// <summary>Upstream request timeout in seconds. Must be at least 1.</summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>Enable automatic HTTPS via Caddy's built-in ACME provider.</summary>
        public bool EnableHttps { get; set; } = true;

        /// <summary>Automatically redirect HTTP requests to HTTPS.</summary>
        public bool AutoRedirectHttp { get; set; } = true;

        /// <summary>Custom HTTP headers to inject into proxied requests (key-value pairs).</summary>
        public Dictionary<string, string> CustomHeaders { get; set; } = new();

        /// <summary>Rate limiting rule expression for Caddy (e.g., "10r/s").</summary>
        public string RateLimitRule { get; set; }

        /// <summary>Whether HTTP Basic Authentication is enabled for this route.</summary>
        public bool BasicAuthEnabled { get; set; }

        /// <summary>Username for Basic Authentication.</summary>
        public string BasicAuthUsername { get; set; }

        /// <summary>Bcrypt-hashed password for Basic Authentication.</summary>
        public string BasicAuthPasswordHash { get; set; }

        /// <summary>
        /// DNS-01 challenge provider name for Let's Encrypt wildcard certificates
        /// (e.g., "cloudflare", "route53"). When set, the generated Caddyfile site block
        /// will include a <c>tls { dns &lt;provider&gt; }</c> directive instead of the
        /// default HTTP-01 challenge. Leave null/empty to use the default HTTP-01 challenge.
        /// </summary>
        public string TlsDnsProvider { get; set; }

        /// <summary>UTC timestamp when the route was created.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the last configuration change.</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Whether the route is active. Inactive routes are excluded from Caddyfile generation.</summary>
        public bool IsActive { get; set; } = true;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Domain))
                throw new ValidationException("Domain is required");

            if (string.IsNullOrWhiteSpace(UpstreamUrl))
                throw new ValidationException("Upstream URL is required");

            if (!Uri.TryCreate(UpstreamUrl, UriKind.Absolute, out _))
                throw new ValidationException($"Invalid upstream URL: {UpstreamUrl}");

            if (TimeoutSeconds < 1)
                throw new ValidationException("Timeout must be at least 1 second");

            if (BasicAuthEnabled && string.IsNullOrWhiteSpace(BasicAuthUsername))
                throw new ValidationException("Basic auth requires username");
        }

        public string GetCaddyPathMatcher()
        {
            return string.IsNullOrWhiteSpace(Path) || Path == "/" ? "" : Path;
        }

        /// <summary>
        /// Returns an identifier safe for use as a Caddy named matcher (e.g., <c>@name</c>).
        /// Hyphens are not allowed in Caddy matcher/snippet identifiers; they are replaced with underscores.
        /// Uses <see cref="ServiceId"/> when available, otherwise falls back to <see cref="Id"/>.
        /// </summary>
        public string GetCaddyMatcherName()
        {
            var id = string.IsNullOrWhiteSpace(ServiceId) ? Id : ServiceId;
            return id.Replace("-", "_");
        }

        public string GenerateRoutePath()
        {
            return $"{Domain}{GetCaddyPathMatcher()}";
        }
    }
}
