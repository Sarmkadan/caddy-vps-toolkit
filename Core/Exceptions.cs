// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace CaddyVpsToolkit.Core
{
    /// <summary>
    /// Base exception for the application
    /// </summary>
    public class CaddyVpsException : Exception
    {
        public string ErrorCode { get; set; }
        public object Details { get; set; }

        public CaddyVpsException(string message, string errorCode = null, Exception innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode ?? "GENERAL_ERROR";
        }

        public CaddyVpsException(string message, string errorCode, object details, Exception innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            Details = details;
        }
    }

    /// <summary>
    /// Thrown when a service cannot be found
    /// </summary>
    public class ServiceNotFoundException : CaddyVpsException
    {
        public ServiceNotFoundException(string serviceId)
            : base($"Service '{serviceId}' not found", "SERVICE_NOT_FOUND", serviceId)
        {
        }
    }

    /// <summary>
    /// Thrown when service configuration is invalid
    /// </summary>
    public class ServiceConfigurationException : CaddyVpsException
    {
        public ServiceConfigurationException(string message, object details = null)
            : base(message, "SERVICE_CONFIG_ERROR", details)
        {
        }
    }

    /// <summary>
    /// Thrown when systemd operation fails
    /// </summary>
    public class SystemdOperationException : CaddyVpsException
    {
        public SystemdOperationException(string message, Exception innerException = null)
            : base(message, "SYSTEMD_ERROR", null, innerException)
        {
        }
    }

    /// <summary>
    /// Thrown when Caddy operation fails
    /// </summary>
    public class CaddyOperationException : CaddyVpsException
    {
        public CaddyOperationException(string message, Exception innerException = null)
            : base(message, "CADDY_ERROR", null, innerException)
        {
        }
    }

    /// <summary>
    /// Thrown when health check fails
    /// </summary>
    public class HealthCheckException : CaddyVpsException
    {
        public HealthCheckException(string serviceId, string message)
            : base(message, "HEALTH_CHECK_ERROR", serviceId)
        {
        }
    }

    /// <summary>
    /// Thrown when database operation fails
    /// </summary>
    public class DatabaseException : CaddyVpsException
    {
        public DatabaseException(string message, Exception innerException = null)
            : base(message, "DATABASE_ERROR", null, innerException)
        {
        }
    }

    /// <summary>
    /// Thrown when validation fails
    /// </summary>
    public class ValidationException : CaddyVpsException
    {
        public ValidationException(string message, object details = null)
            : base(message, "VALIDATION_ERROR", details)
        {
        }
    }

    /// <summary>
    /// Thrown when operation is not supported
    /// </summary>
    public class NotSupportedException : CaddyVpsException
    {
        public NotSupportedException(string message)
            : base(message, "NOT_SUPPORTED")
        {
        }
    }
}
