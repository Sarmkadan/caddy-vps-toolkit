#nullable enable

using System;
using Microsoft.Extensions.DependencyInjection;
using CaddyVpsToolkit.Middleware;

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
        /// <param name="services">The service collection to configure</param>
        /// <param name="configureOptions">Optional action to configure suppression options</param>
        /// <returns>The configured service collection</returns>
        public static IServiceCollection AddNotificationService(
            this IServiceCollection services,
            Action<NotificationSuppressionOptions>? configureOptions = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Configure suppression options
            var options = new NotificationSuppressionOptions();
            configureOptions?.Invoke(options);

            services.AddSingleton(options);
            services.AddSingleton<NotificationService>();

            return services;
        }

        /// <summary>
        /// Adds NotificationService with default options to the service collection.
        /// </summary>
        /// <param name="services">The service collection to configure</param>
        /// <returns>The configured service collection</returns>
        public static IServiceCollection AddNotificationService(this IServiceCollection services)
        {
            return services.AddNotificationService(configureOptions: null);
        }
    }
}