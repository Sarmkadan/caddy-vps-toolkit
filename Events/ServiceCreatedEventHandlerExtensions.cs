#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Middleware;
using CaddyVpsToolkit.Integration;

namespace CaddyVpsToolkit.Events
{
    /// <summary>
    /// Extension methods for <see cref="ServiceCreatedEventHandler"/> providing additional functionality
    /// </summary>
    public static class ServiceCreatedEventHandlerExtensions
    {
        /// <summary>
        /// Creates a new <see cref="ServiceCreatedEventHandler"/> with the specified logger and webhook handler.
        /// </summary>
        /// <param name="logger">The logger instance. Cannot be null.</param>
        /// <param name="webhookHandler">The webhook handler instance. Cannot be null.</param>
        /// <returns>A new <see cref="ServiceCreatedEventHandler"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> or <paramref name="webhookHandler"/> is null.</exception>
        public static ServiceCreatedEventHandler WithLogger(this ILogger logger, IWebhookHandler webhookHandler)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(webhookHandler);

            return new ServiceCreatedEventHandler(logger, webhookHandler);
        }

        /// <summary>
        /// Handles the service created event with additional validation logging
        /// </summary>
        /// <param name="handler">The event handler. Cannot be null.</param>
        /// <param name="@event">The service created event. Cannot be null and must have valid properties.</param>
        /// <returns>Task representing the async operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> or <paramref name="@event"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="@event"/>.ServiceName is null or whitespace, or <paramref name="@event"/>.ExecutablePath is null or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="@event"/>.Port is not in valid range (1-65535).</exception>
        public static async Task HandleWithValidationAsync(this ServiceCreatedEventHandler handler, ServiceCreatedEvent @event)
        {
            ArgumentNullException.ThrowIfNull(handler);
            ArgumentNullException.ThrowIfNull(@event);

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
        /// <param name="handler">The event handler. Cannot be null.</param>
        /// <param name="@event">The service created event. Cannot be null.</param>
        /// <param name="logLevel">The log level to use.</param>
        /// <returns>Task representing the async operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> or <paramref name="@event"/> is null.</exception>
        public static async Task HandleWithLogLevelAsync(this ServiceCreatedEventHandler handler, ServiceCreatedEvent @event, LogLevel logLevel)
        {
            ArgumentNullException.ThrowIfNull(handler);
            ArgumentNullException.ThrowIfNull(@event);

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
        /// <param name="handler">The event handler. Cannot be null.</param>
        /// <returns>The logger instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the logger field cannot be found in <see cref="ServiceCreatedEventHandler"/> type.</exception>
        public static ILogger GetLogger(this ServiceCreatedEventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

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
        /// <param name="handler">The event handler. Cannot be null.</param>
        /// <returns>The webhook handler instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the webhook handler field cannot be found in <see cref="ServiceCreatedEventHandler"/> type.</exception>
        public static IWebhookHandler GetWebhookHandler(this ServiceCreatedEventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

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
