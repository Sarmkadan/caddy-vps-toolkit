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
    }
}