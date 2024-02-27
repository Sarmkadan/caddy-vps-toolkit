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
    /// Extension methods for ServiceCreatedEventHandler providing additional functionality
    /// </summary>
    public static class ServiceCreatedEventHandlerExtensions
    {
        /// <summary>
        /// Creates a new ServiceCreatedEventHandler with the specified logger and webhook handler
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="webhookHandler">The webhook handler instance</param>
        /// <returns>A new ServiceCreatedEventHandler instance</returns>
        public static ServiceCreatedEventHandler WithLogger(this ILogger logger, IWebhookHandler webhookHandler)
        {
            return new ServiceCreatedEventHandler(logger, webhookHandler);
        }

        /// <summary>
        /// Handles the service created event with additional validation logging
        /// </summary>
        /// <param name="handler">The event handler</param>
        /// <param name="@event">The service created event</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task HandleWithValidationAsync(this ServiceCreatedEventHandler handler, ServiceCreatedEvent @event)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            if (string.IsNullOrWhiteSpace(@event.ServiceName))
            {
                throw new ArgumentException("Service name cannot be null or whitespace", nameof(@event.ServiceName));
            }

            if (@event.Port <= 0 || @event.Port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(@event.Port), @event.Port, "Port must be between 1 and 65535");
            }

            if (string.IsNullOrWhiteSpace(@event.ExecutablePath))
            {
                throw new ArgumentException("Executable path cannot be null or whitespace", nameof(@event.ExecutablePath));
            }

            await handler.HandleAsync(@event);
        }

        /// <summary>
        /// Handles the service created event and logs to a specific log level
        /// </summary>
        /// <param name="handler">The event handler</param>
        /// <param name="@event">The service created event</param>
        /// <param name="logLevel">The log level to use</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task HandleWithLogLevelAsync(this ServiceCreatedEventHandler handler, ServiceCreatedEvent @event, LogLevel logLevel)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            var message = $"Service created: {{{@event.ServiceName}}} on port {@event.Port} (type: {@event.ServiceType}) - Executable: {@event.ExecutablePath}";

            switch (logLevel)
            {
                case LogLevel.Debug:
                    await handler.GetLogger().LogDebugAsync(message);
                    break;
                case LogLevel.Info:
                    await handler.GetLogger().LogInfoAsync(message);
                    break;
                case LogLevel.Warning:
                    await handler.GetLogger().LogWarningAsync(message);
                    break;
                case LogLevel.Error:
                    await handler.GetLogger().LogErrorAsync(message);
                    break;
                default:
                    await handler.GetLogger().LogInfoAsync(message);
                    break;
            }

            await handler.HandleAsync(@event);
        }

        /// <summary>
        /// Gets the logger instance associated with this handler
        /// </summary>
        /// <param name="handler">The event handler</param>
        /// <returns>The logger instance</returns>
        public static ILogger GetLogger(this ServiceCreatedEventHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            // Use reflection to get the private logger field
            var loggerField = typeof(ServiceCreatedEventHandler).GetField(
                "_logger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (loggerField != null)
            {
                return (ILogger)loggerField.GetValue(handler);
            }

            throw new InvalidOperationException("Logger field not found in ServiceCreatedEventHandler");
        }

        /// <summary>
        /// Gets the webhook handler instance associated with this handler
        /// </summary>
        /// <param name="handler">The event handler</param>
        /// <returns>The webhook handler instance</returns>
        public static IWebhookHandler GetWebhookHandler(this ServiceCreatedEventHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            // Use reflection to get the private webhook handler field
            var webhookHandlerField = typeof(ServiceCreatedEventHandler).GetField(
                "_webhookHandler",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (webhookHandlerField != null)
            {
                return (IWebhookHandler)webhookHandlerField.GetValue(handler);
            }

            throw new InvalidOperationException("Webhook handler field not found in ServiceCreatedEventHandler");
        }
    }
}
