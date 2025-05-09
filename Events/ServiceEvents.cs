// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using CaddyVpsToolkit.Core;

namespace CaddyVpsToolkit.Events
{
    /// <summary>
    /// Raised when a new service is created
    /// </summary>
    public class ServiceCreatedEvent : DomainEvent
    {
        public string ServiceName { get; set; }
        public ServiceType ServiceType { get; set; }
        public int Port { get; set; }
        public string ExecutablePath { get; set; }
    }

    /// <summary>
    /// Raised when a service is deleted
    /// </summary>
    public class ServiceDeletedEvent : DomainEvent
    {
        public string ServiceName { get; set; }
        public ServiceType ServiceType { get; set; }
    }

    /// <summary>
    /// Raised when a service status changes
    /// </summary>
    public class ServiceStatusChangedEvent : DomainEvent
    {
        public string ServiceName { get; set; }
        public ServiceStatus OldStatus { get; set; }
        public ServiceStatus NewStatus { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    /// <summary>
    /// Raised when service configuration is updated
    /// </summary>
    public class ServiceConfigurationUpdatedEvent : DomainEvent
    {
        public string ServiceName { get; set; }
        public string ConfigurationKey { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }

    /// <summary>
    /// Raised when a service health check fails
    /// </summary>
    public class ServiceHealthCheckFailedEvent : DomainEvent
    {
        public string ServiceName { get; set; }
        public string ErrorMessage { get; set; }
        public int ConsecutiveFailures { get; set; }
    }

    /// <summary>
    /// Raised when a previously unhealthy service recovers
    /// </summary>
    public class ServiceHealthRecoveredEvent : DomainEvent
    {
        public string ServiceName { get; set; }
        public int ResponseTimeMs { get; set; }
    }
}
