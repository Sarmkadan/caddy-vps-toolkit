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
    /// Caddy reverse proxy route configuration
    /// </summary>
    public class CaddyRoute
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ServiceId { get; set; }

        [Required]
        public string Domain { get; set; }

        public string Path { get; set; } = "/";

        public string UpstreamUrl { get; set; }

        public bool StripPath { get; set; }

        public bool PreserveHostHeader { get; set; }

        public int TimeoutSeconds { get; set; } = 30;

        public bool EnableHttps { get; set; } = true;

        public bool AutoRedirectHttp { get; set; } = true;

        public Dictionary<string, string> CustomHeaders { get; set; } = new();

        public string RateLimitRule { get; set; }

        public bool BasicAuthEnabled { get; set; }

        public string BasicAuthUsername { get; set; }

        public string BasicAuthPasswordHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

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

        public string GenerateRoutePath()
        {
            return $"{Domain}{GetCaddyPathMatcher()}";
        }
    }
}
