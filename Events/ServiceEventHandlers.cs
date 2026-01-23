#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Middleware;
using CaddyVpsToolkit.Integration;

namespace CaddyVpsToolkit.Events
{
    /// <summary>
    /// Handler for when a service is created - logs and triggers webhook
    /// </summary>
    public sealed class ServiceCreatedEventHandler : IEventHandler<ServiceCreatedEvent>
    {
        private readonly ILogger _logger;
        private readonly IWebhookHandler _webhookHandler;

        public ServiceCreatedEventHandler(ILogger logger, IWebhookHandler webhookHandler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
        }

        public async Task HandleAsync(ServiceCreatedEvent @event)
        {
            await _logger.LogInfoAsync($"Service created: {{{@event.ServiceName}}} on port {@event.Port}");

            await _webhookHandler.TriggerAsync(
                WebhookEventType.ServiceCreated,
                new
                {
                    ServiceName = @event.ServiceName,
                    ServiceType = @event.ServiceType,
                    Port = @event.Port,
                    CreatedAt = @event.OccurredAt
                }
            );
        }
    }

    /// <summary>
    /// Handler for when service status changes
    /// </summary>
    public sealed class ServiceStatusChangedEventHandler : IEventHandler<ServiceStatusChangedEvent>
    {
        private readonly ILogger _logger;
        private readonly IWebhookHandler _webhookHandler;

        public ServiceStatusChangedEventHandler(ILogger logger, IWebhookHandler webhookHandler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
        }

        public async Task HandleAsync(ServiceStatusChangedEvent @event)
        {
            await _logger.LogInfoAsync(
                $"Service status changed: {{{@event.ServiceName}}} from {@event.OldStatus} to {@event.NewStatus}");

            await _webhookHandler.TriggerAsync(
                WebhookEventType.ServiceStatusChanged,
                new
                {
                    ServiceName = @event.ServiceName,
                    OldStatus = @event.OldStatus,
                    NewStatus = @event.NewStatus,
                    ChangedAt = @event.ChangedAt
                }
            );
        }
    }

    /// <summary>
    /// Handler for health check failures
    /// </summary>
    public sealed class ServiceHealthCheckFailedEventHandler : IEventHandler<ServiceHealthCheckFailedEvent>
    {
        private readonly ILogger _logger;
        private readonly IWebhookHandler _webhookHandler;

        public ServiceHealthCheckFailedEventHandler(ILogger logger, IWebhookHandler webhookHandler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
        }

        public async Task HandleAsync(ServiceHealthCheckFailedEvent @event)
        {
            await _logger.LogWarningAsync(
                $"Health check failed for {{{@event.ServiceName}}}: {@event.ErrorMessage} (consecutive: {{@event.ConsecutiveFailures}})");

            await _webhookHandler.TriggerAsync(
                WebhookEventType.HealthCheckFailed,
                new
                {
                    ServiceName = @event.ServiceName,
                    ErrorMessage = @event.ErrorMessage,
                    ConsecutiveFailures = @event.ConsecutiveFailures
                }
            );
        }
    }

    /// <summary>
    /// Handler for when a service recovers from health issues
    /// </summary>
    public sealed class ServiceHealthRecoveredEventHandler : IEventHandler<ServiceHealthRecoveredEvent>
    {
        private readonly ILogger _logger;
        private readonly IWebhookHandler _webhookHandler;

        public ServiceHealthRecoveredEventHandler(ILogger logger, IWebhookHandler webhookHandler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
        }

        public async Task HandleAsync(ServiceHealthRecoveredEvent @event)
        {
            await _logger.LogInfoAsync($"Service recovered: {{{@event.ServiceName}}} (response: {@event.ResponseTimeMs}ms)");

            await _webhookHandler.TriggerAsync(
                WebhookEventType.HealthCheckRecovered,
                new
                {
                    ServiceName = @event.ServiceName,
                    ResponseTimeMs = @event.ResponseTimeMs,
                    RecoveredAt = @event.OccurredAt
                }
            );
        }
    }
}
