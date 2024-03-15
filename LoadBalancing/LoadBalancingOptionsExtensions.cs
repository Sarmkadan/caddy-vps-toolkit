// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.LoadBalancing
{
    /// <summary>
    /// Provides extension methods for <see cref="LoadBalancingOptions"/> to simplify common configuration patterns
    /// and enable fluent-style configuration of load balancing behavior.
    /// </summary>
    public static class LoadBalancingOptionsExtensions
    {
        /// <summary>
        /// Configures the load balancer to use round-robin strategy with the specified health check settings.
        /// </summary>
        /// <param name="options">The load balancing options to configure.</param>
        /// <param name="healthCheckIntervalSeconds">Interval in seconds between health probes.</param>
        /// <param name="healthProbeTimeoutMs">TCP connection timeout for health probes in milliseconds.</param>
        /// <param name="unhealthyThreshold">Number of consecutive failures to mark upstream as unhealthy.</param>
        /// <param name="healthyThreshold">Number of consecutive successes to restore unhealthy upstream.</param>
        /// <returns>The configured <see cref="LoadBalancingOptions"/> for method chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        public static LoadBalancingOptions UseRoundRobinWithHealthChecks(
            this LoadBalancingOptions options,
            int healthCheckIntervalSeconds = 30,
            int healthProbeTimeoutMs = 5000,
            int unhealthyThreshold = 3,
            int healthyThreshold = 2)
        {
            ArgumentNullException.ThrowIfNull(options);

            options.DefaultStrategy = LoadBalancingStrategy.RoundRobin;
            options.HealthCheckIntervalSeconds = healthCheckIntervalSeconds;
            options.HealthProbeTimeoutMs = healthProbeTimeoutMs;
            options.UnhealthyThreshold = unhealthyThreshold;
            options.HealthyThreshold = healthyThreshold;
            options.ActiveHealthEnabled = true;

            return options;
        }

        /// <summary>
        /// Configures the load balancer to use least-connections strategy with active health monitoring.
        /// </summary>
        /// <param name="options">The load balancing options to configure.</param>
        /// <param name="healthCheckIntervalSeconds">Interval in seconds between health probes.</param>
        /// <param name="healthProbeTimeoutMs">TCP connection timeout for health probes in milliseconds.</param>
        /// <param name="unhealthyThreshold">Number of consecutive failures to mark upstream as unhealthy.</param>
        /// <param name="healthyThreshold">Number of consecutive successes to restore unhealthy upstream.</param>
        /// <returns>The configured <see cref="LoadBalancingOptions"/> for method chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        public static LoadBalancingOptions UseLeastConnectionsWithHealthChecks(
            this LoadBalancingOptions options,
            int healthCheckIntervalSeconds = 30,
            int healthProbeTimeoutMs = 5000,
            int unhealthyThreshold = 3,
            int healthyThreshold = 2)
        {
            ArgumentNullException.ThrowIfNull(options);

            options.DefaultStrategy = LoadBalancingStrategy.LeastConnections;
            options.HealthCheckIntervalSeconds = healthCheckIntervalSeconds;
            options.HealthProbeTimeoutMs = healthProbeTimeoutMs;
            options.UnhealthyThreshold = unhealthyThreshold;
            options.HealthyThreshold = healthyThreshold;
            options.ActiveHealthEnabled = true;

            return options;
        }

        /// <summary>
        /// Configures the circuit breaker to open when the specified percentage of upstreams are unhealthy.
        /// </summary>
        /// <param name="options">The load balancing options to configure.</param>
        /// <param name="healthThreshold">Minimum fraction of healthy upstreams (0.0-1.0) below which the circuit opens.</param>
        /// <param name="recoverySeconds">Time in seconds the circuit remains open before attempting recovery.</param>
        /// <returns>The configured <see cref="LoadBalancingOptions"/> for method chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="healthThreshold"/> is not between 0.0 and 1.0.</exception>
        public static LoadBalancingOptions ConfigureCircuitBreaker(
            this LoadBalancingOptions options,
            double healthThreshold,
            int recoverySeconds = 60)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (healthThreshold < 0.0 || healthThreshold > 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(healthThreshold),
                    healthThreshold,
                    "Health threshold must be between 0.0 and 1.0");
            }

            options.CircuitBreakerHealthThreshold = healthThreshold;
            options.CircuitBreakerRecoverySeconds = recoverySeconds;
            options.CircuitBreakerEnabled = true;

            return options;
        }

        /// <summary>
        /// Enables sticky sessions with the specified cookie name and timeout configuration.
        /// </summary>
        /// <param name="options">The load balancing options to configure.</param>
        /// <param name="cookieName">Name of the cookie used for session affinity.</param>
        /// <param name="drainTimeoutSeconds">Time in seconds to allow existing connections to complete during draining.</param>
        /// <returns>The configured <see cref="LoadBalancingOptions"/> for method chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> or <paramref name="cookieName"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="cookieName"/> is empty or whitespace.</exception>
        public static LoadBalancingOptions EnableStickySessions(
            this LoadBalancingOptions options,
            string cookieName,
            int drainTimeoutSeconds = 30)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrEmpty(cookieName);

            options.StickySessionEnabled = true;
            options.DefaultStickyCookieName = cookieName.Trim();
            options.ConnectionDrainTimeoutSeconds = drainTimeoutSeconds;

            return options;
        }

        /// <summary>
        /// Gets the effective health check interval in milliseconds, converting seconds to milliseconds.
        /// </summary>
        /// <param name="options">The load balancing options.</param>
        /// <returns>The health check interval in milliseconds.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        public static int GetHealthCheckIntervalMs(this LoadBalancingOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return options.HealthCheckIntervalSeconds * 1000;
        }

        /// <summary>
        /// Gets the effective health probe timeout in seconds, converting milliseconds to seconds.
        /// </summary>
        /// <param name="options">The load balancing options.</param>
        /// <returns>The health probe timeout in seconds.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        public static double GetHealthProbeTimeoutSeconds(this LoadBalancingOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return options.HealthProbeTimeoutMs / 1000.0;
        }

        /// <summary>
        /// Determines whether the circuit breaker is configured to open when all upstreams are unavailable.
        /// </summary>
        /// <param name="options">The load balancing options.</param>
        /// <returns><c>true</c> if the circuit breaker opens only when all upstreams are down; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        public static bool IsStrictCircuitBreaker(this LoadBalancingOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return options.CircuitBreakerHealthThreshold == 0.0;
        }

        /// <summary>
        /// Gets the collection of all retry-related configuration values as key-value pairs.
        /// </summary>
        /// <param name="options">The load balancing options.</param>
        /// <returns>An enumerable of retry configuration entries.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        public static IEnumerable<KeyValuePair<string, object>> GetRetryConfiguration(
            this LoadBalancingOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            yield return new KeyValuePair<string, object>(
                nameof(options.MaxRetries),
                options.MaxRetries);

            yield return new KeyValuePair<string, object>(
                nameof(options.RetryDurationSeconds),
                options.RetryDurationSeconds);
        }
    }
}