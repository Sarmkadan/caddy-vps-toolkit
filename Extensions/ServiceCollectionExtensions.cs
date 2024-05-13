#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using Microsoft.Extensions.DependencyInjection;
using CaddyVpsToolkit.Caching;
using CaddyVpsToolkit.Integration;
using CaddyVpsToolkit.Middleware;
using CaddyVpsToolkit.Events;
using CaddyVpsToolkit.BackgroundWorkers;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> to simplify dependency injection registration.
    /// Provides a fluent API for adding infrastructure services to the service collection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds caching services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddCachingServices(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<ICacheService, MemoryCache>();
            return services;
        }

        /// <summary>
        /// Adds HTTP client services with retry policy to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="timeoutMs">HTTP request timeout in milliseconds. Defaults to 30 seconds.</param>
        /// <param name="maxRetries">Maximum number of retry attempts. Defaults to 3.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddHttpClientServices(
            this IServiceCollection services,
            int timeoutMs = 30000,
            int maxRetries = 3)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<IRetryPolicy>(_ => new ExponentialBackoffRetryPolicy(maxRetries));
            services.AddSingleton<IHttpClient>(sp =>
                new HttpClientWrapper(timeoutMs, sp.GetRequiredService<IRetryPolicy>()));

            return services;
        }

        /// <summary>
        /// Adds webhook services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddWebhookServices(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<IWebhookHandler>(sp =>
                new WebhookHandler(sp.GetRequiredService<IHttpClient>()));

            return services;
        }

        /// <summary>
        /// Adds logging services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="logPath">Path to the log file. Defaults to "logs/app.log".</param>
        /// <param name="minLevel">Minimum log level to record. Defaults to <see cref="LogLevel.Info"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddLoggingServices(
            this IServiceCollection services,
            string logPath = "logs/app.log",
            LogLevel minLevel = LogLevel.Info)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrEmpty(logPath);

            services.AddSingleton<ILogger>(_ => new FileLogger(logPath, minLevel, true));
            return services;
        }

        /// <summary>
        /// Adds event bus services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddEventBus(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<IEventBus, EventBus>();
            return services;
        }

        /// <summary>
        /// Adds rate limiting services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="capacity">Maximum number of requests allowed in the bucket. Defaults to 100.</param>
        /// <param name="refillRate">Number of tokens added per second. Defaults to 10.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddRateLimiting(
            this IServiceCollection services,
            int capacity = 100,
            int refillRate = 10)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<IRateLimiter>(_ => new TokenBucketRateLimiter(capacity, refillRate));
            return services;
        }

        /// <summary>
        /// Adds service discovery services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddServiceDiscovery(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<IServiceDiscoveryClient, InMemoryServiceDiscoveryClient>();
            return services;
        }

        /// <summary>
        /// Adds all infrastructure services at once to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configure">Optional configuration action for infrastructure options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            Action<InfrastructureOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            var options = new InfrastructureOptions();
            configure?.Invoke(options);

            services.AddCachingServices();
            services.AddHttpClientServices(options.HttpTimeoutMs, options.MaxRetries);
            services.AddWebhookServices();
            services.AddLoggingServices(options.LogPath, options.MinLogLevel);
            services.AddEventBus();
            services.AddRateLimiting(options.RateLimitCapacity, options.RateLimitRefillRate);
            services.AddServiceDiscovery();

            return services;
        }
    }

    /// <summary>
    /// Configuration options for infrastructure services.
    /// </summary>
    public sealed class InfrastructureOptions
    {
        public int HttpTimeoutMs { get; set; } = 30000;
        public int MaxRetries { get; set; } = 3;
        public string LogPath { get; set; } = "logs/app.log";
        public LogLevel MinLogLevel { get; set; } = LogLevel.Info;
        public int RateLimitCapacity { get; set; } = 100;
        public int RateLimitRefillRate { get; set; } = 10;
    }
}