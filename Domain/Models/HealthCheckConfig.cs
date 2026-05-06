// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Core;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Configuration for service health checks
    /// </summary>
    public class HealthCheckConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ServiceId { get; set; }

        [Required]
        public HealthCheckType Type { get; set; }

        public string Endpoint { get; set; }

        public int IntervalSeconds { get; set; } = 30;

        public int TimeoutSeconds { get; set; } = 5;

        public int UnhealthyThreshold { get; set; } = 3;

        public int HealthyThreshold { get; set; } = 2;

        public bool Enabled { get; set; } = true;

        public string HttpMethod { get; set; } = "GET";

        public int ExpectedHttpStatus { get; set; } = 200;

        public string ExpectedResponse { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public void Validate()
        {
            if (IntervalSeconds < 5)
                throw new ValidationException("Health check interval must be at least 5 seconds");

            if (TimeoutSeconds > IntervalSeconds)
                throw new ValidationException("Timeout cannot be greater than interval");

            if (TimeoutSeconds < 1)
                throw new ValidationException("Timeout must be at least 1 second");

            if (UnhealthyThreshold < 1)
                throw new ValidationException("Unhealthy threshold must be at least 1");

            if (HealthyThreshold < 1)
                throw new ValidationException("Healthy threshold must be at least 1");

            if (Type == HealthCheckType.Http && string.IsNullOrWhiteSpace(Endpoint))
                throw new ValidationException("HTTP health check requires an endpoint");
        }

        public string GetHealthCheckUrl(string serviceHost, int servicePort)
        {
            if (Type == HealthCheckType.Http)
            {
                var protocol = Endpoint?.StartsWith("/") == true ? "http" : "http://";
                return $"http://{serviceHost}:{servicePort}{Endpoint}";
            }
            return null;
        }
    }
}
