#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.ComponentModel.DataAnnotations;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Result of a health check for a service
    /// </summary>
    public sealed class HealthCheckResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ServiceId { get; set; }

        public bool IsHealthy { get; set; }

        public HealthCheckStatus Status { get; set; }

        public int ResponseTimeMs { get; set; }

        public int HttpStatusCode { get; set; }

        public string ErrorMessage { get; set; }

        public string ResponseBody { get; set; }

        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        public int ConsecutiveFailures { get; set; }

        public int ConsecutiveSuccesses { get; set; }

        public string CheckType { get; set; }

        public string Endpoint { get; set; }

        public static HealthCheckResult CreateSuccess(string serviceId, int responseTimeMs, int httpStatus = 200)
        {
            return new HealthCheckResult
            {
                ServiceId = serviceId,
                IsHealthy = true,
                Status = HealthCheckStatus.Healthy,
                ResponseTimeMs = responseTimeMs,
                HttpStatusCode = httpStatus,
                CheckedAt = DateTime.UtcNow
            };
        }

        public static HealthCheckResult CreateFailure(string serviceId, string errorMessage, int responseTimeMs = 0)
        {
            return new HealthCheckResult
            {
                ServiceId = serviceId,
                IsHealthy = false,
                Status = HealthCheckStatus.Unhealthy,
                ErrorMessage = errorMessage,
                ResponseTimeMs = responseTimeMs,
                CheckedAt = DateTime.UtcNow
            };
        }

        public bool IsSlowResponse(int thresholdMs = 5000)
        {
            return ResponseTimeMs > thresholdMs;
        }
    }

    public enum HealthCheckStatus
    {
        Unknown,
        Healthy,
        Unhealthy,
        Degraded
    }
}
