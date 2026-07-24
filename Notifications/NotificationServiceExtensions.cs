#nullable enable

using System;
using Microsoft.Extensions.DependencyInjection;
using CaddyVpsToolkit.Middleware;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Notifications
{
    /// <summary>
    /// Extension methods for configuring NotificationService with dependency injection.
    /// </summary>
    public static class NotificationServiceExtensions
    {
        /// <summary>
        /// Adds NotificationService and related services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
        /// <param name="configureOptions">Optional action to configure suppression options.</param>
        /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddNotificationService(
            this IServiceCollection services,
            Action<NotificationSuppressionOptions>? configureOptions = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            // Configure suppression options
            var options = new NotificationSuppressionOptions();
            configureOptions?.Invoke(options);

            services.AddSingleton(options);
            services.AddSingleton<NotificationService>();
            services.AddSingleton<ICircuitBreakerFactory>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger>();
                return new CircuitBreakerFactory(
                    logger,
                    options.CircuitBreakerFailureThreshold,
                    options.CircuitBreakerRecoveryTimeoutSeconds
                );
            });

            return services;
        }

        /// <summary>
        /// Adds NotificationService with default options to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
        /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddNotificationService(this IServiceCollection services)
            => services.AddNotificationService(configureOptions: null);

        /// <summary>
        /// Validates an email address format.
        /// </summary>
        /// <param name="email">The email address to validate</param>
        /// <returns>The validated email address</returns>
        /// <exception cref="ArgumentException">Thrown when email format is invalid</exception>
        public static string ValidateEmail(this string email)
        {
            return DestinationValidator.ValidateEmail(email);
        }

        /// <summary>
        /// Validates a webhook URL format and security constraints.
        /// </summary>
        /// <param name="webhookUrl">The webhook URL to validate</param>
        /// <returns>The validated webhook URL</returns>
        /// <exception cref="ArgumentException">Thrown when URL format is invalid or contains blocked patterns</exception>
        public static string ValidateWebhookUrl(this string webhookUrl)
        {
            return DestinationValidator.ValidateWebhookUrl(webhookUrl);
        }

        /// <summary>
        /// Validates a phone number format.
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate</param>
        /// <returns>The validated phone number</returns>
        /// <exception cref="ArgumentException">Thrown when phone number format is invalid</exception>
        public static string ValidatePhoneNumber(this string phoneNumber)
        {
            return DestinationValidator.ValidatePhoneNumber(phoneNumber);
        }

        /// <summary>
        /// Validates notification message content to prevent template injection.
        /// </summary>
        /// <param name="message">The message content to validate</param>
        /// <returns>The validated message content</returns>
        /// <exception cref="ArgumentException">Thrown when message contains template injection patterns</exception>
        public static string ValidateMessageContent(this string message)
        {
            return DestinationValidator.ValidateMessageContent(message);
        }

        /// <summary>
        /// Validates a destination string based on its type.
        /// </summary>
        /// <param name="destination">The destination string to validate</param>
        /// <param name="destinationType">The type of destination</param>
        /// <returns>The validated destination string</returns>
        /// <exception cref="ArgumentException">Thrown when destination format is invalid</exception>
        public static string ValidateDestination(this string destination, DestinationType destinationType)
        {
            return DestinationValidator.ValidateDestination(destination, destinationType);
        }
    }
}