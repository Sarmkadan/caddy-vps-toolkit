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

namespace CaddyVpsToolkit.Extensions
{
    /// <summary>
    /// Extension methods for IServiceCollection to simplify DI registration.
    /// Provides fluent API for adding infrastructure services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add caching services
        /// </summary>
        public static IServiceCollection AddCachingServices(this IServiceCollection services)
        {
            services.AddSingleton<ICacheService, MemoryCache>();
            return services;
        }

        /// <summary>
        /// Add HTTP client with retry policy
        /// </summary>
        public static IServiceCollection AddHttpClientServices(
            this IServiceCollection services,
            int timeoutMs = 30000,
            int maxRetries = 3)
        {
            services.AddSingleton<IRetryPolicy>(new ExponentialBackoffRetryPolicy(maxRetries));
            services.AddSingleton<IHttpClient>(sp =>
            {
                var retryPolicy = sp.GetRequiredService<IRetryPolicy>();
                return new HttpClientWrapper(timeoutMs, retryPolicy);
            });
            return services;
        }

        /// <summary>
        /// Add webhook services
        /// </summary>
        public static IServiceCollection AddWebhookServices(this IServiceCollection services)
        {
            services.AddSingleton<IWebhookHandler>(sp =>
                new WebhookHandler(sp.GetRequiredService<IHttpClient>()));
            return services;
        }

        /// <summary>
        /// Add logging services
        /// </summary>
        public static IServiceCollection AddLoggingServices(
            this IServiceCollection services,
            string logPath = "logs/app.log",
            LogLevel minLevel = LogLevel.Info)
        {
            services.AddSingleton<ILogger>(new FileLogger(logPath, minLevel, true));
            return services;
        }

        /// <summary>
        /// Add event bus
        /// </summary>
        public static IServiceCollection AddEventBus(this IServiceCollection services)
        {
            services.AddSingleton<IEventBus, EventBus>();
            return services;
        }

        /// <summary>
        /// Add rate limiting
        /// </summary>
        public static IServiceCollection AddRateLimiting(
            this IServiceCollection services,
            int capacity = 100,
            int refillRate = 10)
        {
            services.AddSingleton<IRateLimiter>(new TokenBucketRateLimiter(capacity, refillRate));
            return services;
        }

        /// <summary>
        /// Add service discovery
        /// </summary>
        public static IServiceCollection AddServiceDiscovery(this IServiceCollection services)
        {
            services.AddSingleton<IServiceDiscoveryClient, InMemoryServiceDiscoveryClient>();
            return services;
        }

        /// <summary>
        /// Add all infrastructure services at once
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            Action<InfrastructureOptions> configure = null)
        {
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
    /// Configuration options for infrastructure services
    /// </summary>
    public class InfrastructureOptions
    {
        public int HttpTimeoutMs { get; set; } = 30000;
        public int MaxRetries { get; set; } = 3;
        public string LogPath { get; set; } = "logs/app.log";
        public LogLevel MinLogLevel { get; set; } = LogLevel.Info;
        public int RateLimitCapacity { get; set; } = 100;
        public int RateLimitRefillRate { get; set; } = 10;
    }
}
