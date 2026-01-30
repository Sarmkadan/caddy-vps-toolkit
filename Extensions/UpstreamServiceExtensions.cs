// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using System;
using Microsoft.Extensions.DependencyInjection;
using CaddyVpsToolkit.Configuration;
using CaddyVpsToolkit.LoadBalancing;
using CaddyVpsToolkit.Services;

namespace CaddyVpsToolkit.Extensions
{
    /// <summary>
    /// Extension methods on <see cref="IServiceCollection"/> for registering the v2 adaptive
    /// upstream management and health-aware load-balancing subsystem.
    /// </summary>
    /// <remarks>
    /// Call <see cref="AddUpstreamManagement"/> after all core infrastructure services have been
    /// registered (i.e. after <see cref="ServiceCollectionExtensions.AddInfrastructureServices"/>)
    /// so that <see cref="UpstreamManagerService"/> and <see cref="LoadBalancingOptions"/> are
    /// available in the container before the adaptive components resolve their dependencies.
    /// </remarks>
    public static class UpstreamServiceExtensions
    {
        /// <summary>
        /// Registers all services required for the v2 dynamic upstream management and health-aware
        /// load-balancing feature:
        /// <list type="bullet">
        ///   <item><see cref="UpstreamManagementOptions"/> — tuning parameters singleton.</item>
        ///   <item><see cref="IMetricsAggregator"/> (<see cref="SlidingWindowMetricsAggregator"/>) — thread-safe sliding-window metrics.</item>
        ///   <item><see cref="IAdaptiveLoadBalancer"/> (<see cref="AdaptiveLoadBalancer"/>) — adaptive scoring engine.</item>
        ///   <item><see cref="HealthAwareRoutingPolicy"/> — unified routing entry point for callers.</item>
        /// </list>
        /// All registrations use singleton lifetime; the adaptive state (weights, penalty timestamps)
        /// must be shared across requests to remain meaningful.
        /// </summary>
        /// <param name="services">The service collection to add registrations to.</param>
        /// <param name="configure">
        /// Optional delegate for customising <see cref="UpstreamManagementOptions"/>. When omitted,
        /// all default values defined on the options class are used.
        /// </param>
        /// <returns>The original <see cref="IServiceCollection"/> for fluent chaining.</returns>
        /// <example>
        /// Configuring the subsystem from <c>Program.cs</c>:
        /// <code>
        /// services
        ///     .AddInfrastructureServices()
        ///     .AddUpstreamManagement(opts =>
        ///     {
        ///         opts.TargetLatencyMs            = 150.0;
        ///         opts.PenaltyMultiplier          = 0.2;
        ///         opts.PenaltyDecaySeconds        = 45.0;
        ///         opts.WeightAdaptationAlpha      = 0.2;
        ///         opts.AutoRecalibrationEnabled   = true;
        ///         opts.RecalibrationIntervalSeconds = 180;
        ///     });
        /// </code>
        /// </example>
        public static IServiceCollection AddUpstreamManagement(
            this IServiceCollection        services,
            Action<UpstreamManagementOptions>? configure = null)
        {
            var options = new UpstreamManagementOptions();
            configure?.Invoke(options);

            // Options — consumed by AdaptiveLoadBalancer and exposed for operator tooling.
            services.AddSingleton(options);

            // Sliding-window metrics aggregator with the configured per-upstream capacity.
            services.AddSingleton<IMetricsAggregator>(
                new SlidingWindowMetricsAggregator(options.MetricsWindowSize));
            
            services.AddSingleton<IUpstreamPoolRepository, CaddyVpsToolkit.Data.UpstreamPoolRepository>();
            services.AddSingleton<IUpstreamHealthTracker, UpstreamHealthTracker>();
            services.AddSingleton<IUpstreamSelector, UpstreamSelector>();

            // Adaptive load balancer — resolves UpstreamManagerService from the container.
            services.AddSingleton<IAdaptiveLoadBalancer>(sp => new AdaptiveLoadBalancer(
                sp.GetRequiredService<UpstreamManagerService>(),
                sp.GetRequiredService<IMetricsAggregator>(),
                options));

            // Unified routing policy — the primary entry point for request-routing callers.
            services.AddSingleton<HealthAwareRoutingPolicy>(sp => new HealthAwareRoutingPolicy(
                sp.GetRequiredService<UpstreamManagerService>(),
                sp.GetRequiredService<IAdaptiveLoadBalancer>(),
                sp.GetRequiredService<LoadBalancingOptions>()));

            return services;
        }
    }
}
