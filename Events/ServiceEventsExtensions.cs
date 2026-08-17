#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// Extension methods for service event types
// =============================================================================

using System;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Events;

namespace CaddyVpsToolkit.Events
{
    /// <summary>
    /// Provides human‑readable descriptions, criticality checks and log‑friendly
    /// string representations for the service event hierarchy.
    /// </summary>
    public static class ServiceEventExtensions
    {
        /// <summary>
        /// Returns a concise, human‑readable description of the event.
        /// </summary>
        public static string Describe(this DomainEvent @event) =>
            @event switch
            {
                ServiceCreatedEvent e =>
                    $"Service '{e.ServiceName}' of type {e.ServiceType} created on port {e.Port}.",

                ServiceDeletedEvent e =>
                    $"Service '{e.ServiceName}' of type {e.ServiceType} deleted.",

                ServiceStatusChangedEvent e =>
                    $"Service '{e.ServiceName}' status changed from {e.OldStatus} to {e.NewStatus} at {e.ChangedAt:u}.",

                ServiceConfigurationUpdatedEvent e =>
                    $"Service '{e.ServiceName}' configuration key '{e.ConfigurationKey}' changed from '{e.OldValue}' to '{e.NewValue}'.",

                ServiceHealthCheckFailedEvent e =>
                    $"Health check failed for service '{e.ServiceName}': {e.ErrorMessage} (consecutive failures: {e.ConsecutiveFailures}).",

                ServiceHealthRecoveredEvent e =>
                    $"Service '{e.ServiceName}' recovered with response time {e.ResponseTimeMs}ms.",

                _ => @event.ToString() ?? string.Empty
            };

        /// <summary>
        /// Determines whether the event is considered critical.
        /// Currently, health‑check failures and deletions are treated as critical.
        /// </summary>
        public static bool IsCritical(this DomainEvent @event) =>
            @event switch
            {
                ServiceHealthCheckFailedEvent => true,
                ServiceDeletedEvent => true,
                _ => false
            };

        /// <summary>
        /// Returns a log‑friendly string that includes a UTC timestamp and the description.
        /// </summary>
        public static string ToLogString(this DomainEvent @event) =>
            $"[{DateTime.UtcNow:u}] {@event.Describe()}";
    }
}
