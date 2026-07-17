// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Extension methods for UpstreamManagerService providing additional utility
// functionality for pool management, health monitoring, and batch operations.
//
// All methods are thread-safe and leverage the existing service capabilities.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.LoadBalancing;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Provides extension methods for <see cref="UpstreamManagerService"/> that add
    /// convenience and batch operations for upstream pool management.
    /// </summary>
    public static class UpstreamManagerServiceExtensions
    {
        /// <summary>
        /// Attempts to get a pool by its identifier, returning a boolean indicating success
        /// without throwing an exception when the pool is not found.
        /// </summary>
        /// <param name="service">The upstream manager service instance.</param>
        /// <param name="poolId">The unique pool identifier.</param>
        /// <returns>A tuple containing a boolean indicating success and the located pool, or null if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="poolId"/> is null or empty.</exception>
        public static async Task<(bool Success, UpstreamPool? Pool)> TryGetPoolAsync(
            this UpstreamManagerService service,
            string poolId)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentException.ThrowIfNullOrEmpty(poolId);

            var pool = await service.GetPoolAsync(poolId);
            return (pool is not null, pool);
        }

        /// <summary>
        /// Safely removes a pool from the registry, returning true if the pool existed
        /// and was removed, or false if the pool was not found.
        /// </summary>
        /// <param name="service">The upstream manager service instance.</param>
        /// <param name="poolId">The pool identifier to remove.</param>
        /// <returns>True if the pool was found and removed; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="poolId"/> is null or empty.</exception>
        public static async Task<bool> TryRemovePoolAsync(
            this UpstreamManagerService service,
            string poolId)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentException.ThrowIfNullOrEmpty(poolId);

            return await service.RemovePoolAsync(poolId);
        }

        /// <summary>
        /// Gets all pools that match the specified predicate, returning an empty list
        /// if no pools match rather than throwing an exception.
        /// </summary>
        /// <param name="service">The upstream manager service instance.</param>
        /// <param name="predicate">The filter predicate to apply.</param>
        /// <returns>A list of pools matching the predicate.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> or <paramref name="predicate"/> is null.</exception>
        public static async Task<IReadOnlyList<UpstreamPool>> GetPoolsAsync(
            this UpstreamManagerService service,
            Func<UpstreamPool, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(predicate);

            var allPools = await service.GetAllPoolsAsync();
            return allPools.Where(predicate).ToList().AsReadOnly();
        }

        /// <summary>
        /// Generates Caddy configuration for all enabled pools belonging to the specified service,
        /// returning the concatenated configuration blocks.
        /// </summary>
        /// <param name="service">The upstream manager service instance.</param>
        /// <param name="serviceId">The service identifier to generate configuration for.</param>
        /// <param name="matchPath">The Caddyfile path matcher. Defaults to "/*".</param>
        /// <returns>A string containing all generated Caddy configuration blocks.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> is null, or when <paramref name="serviceId"/> or <paramref name="matchPath"/> is null or empty.</exception>
        /// <exception cref="ServiceConfigurationException">Thrown when the service does not exist.</exception>
        public static async Task<string> GenerateCaddyConfigForAllEnabledPoolsAsync(
            this UpstreamManagerService service,
            string serviceId,
            string matchPath = "/*")
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentException.ThrowIfNullOrEmpty(serviceId);
            ArgumentException.ThrowIfNullOrEmpty(matchPath);

            var pools = await service.GetPoolsForServiceAsync(serviceId);
            var enabledPools = pools.Where(p => p.IsEnabled).ToList();

            if (enabledPools.Count == 0)
            {
                return string.Empty;
            }

            var configs = new List<string>(enabledPools.Count);
            foreach (var pool in enabledPools)
            {
                var config = await service.GenerateCaddyConfigForPoolAsync(pool.Id, matchPath);
                configs.Add(config);
            }

            return string.Join(Environment.NewLine, configs);
        }

        /// <summary>
        /// Gets the total number of active connections across all pools managed by this service.
        /// </summary>
        /// <param name="service">The upstream manager service instance.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The total count of active connections.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> is null.</exception>
        public static async Task<int> GetTotalActiveConnectionsAsync(
            this UpstreamManagerService service,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(service);

            var pools = await service.GetAllPoolsAsync();
            var total = 0;

            foreach (var pool in pools)
            {
                cancellationToken.ThrowIfCancellationRequested();
                total += pool.GetTotalActiveConnections();
            }

            return total;
        }

        /// <summary>
        /// Gets the total number of healthy upstreams across all pools.
        /// </summary>
        /// <param name="service">The upstream manager service instance.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The count of healthy upstreams.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> is null.</exception>
        public static async Task<int> GetTotalHealthyUpstreamsAsync(
            this UpstreamManagerService service,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(service);

            var pools = await service.GetAllPoolsAsync();
            var total = 0;

            foreach (var pool in pools)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var report = await service.GetHealthReportAsync(pool.Id, cancellationToken);
                total += report.HealthyUpstreams;
            }

            return total;
        }

        /// <summary>
        /// Gets a summary of all pools with their health status and upstream counts.
        /// </summary>
        /// <param name="service">The upstream manager service instance.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of pool summaries.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> is null.</exception>
        public static async Task<IReadOnlyList<PoolSummary>> GetPoolSummariesAsync(
            this UpstreamManagerService service,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(service);

            var pools = await service.GetAllPoolsAsync();
            var summaries = new List<PoolSummary>(pools.Count);

            foreach (var pool in pools)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var report = await service.GetHealthReportAsync(pool.Id, cancellationToken);
                summaries.Add(new PoolSummary(
                    pool.Id,
                    pool.Name,
                    pool.ServiceId,
                    report.TotalUpstreams,
                    report.HealthyUpstreams,
                    report.AvailableUpstreams,
                    report.TotalActiveConnections,
                    pool.IsEnabled
                ));
            }

            return summaries.AsReadOnly();
        }

        /// <summary>
        /// Gets the first available upstream server from the specified pool that meets the criteria.
        /// </summary>
        /// <param name="service">The upstream manager service instance.</param>
        /// <param name="poolId">The pool identifier.</param>
        /// <param name="filter">Optional filter to select specific upstreams.</param>
        /// <param name="clientIp">Optional client IP address for IP-hash selection.</param>
        /// <returns>The selected upstream server, or null if none available.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> is null, or when <paramref name="poolId"/> is null or empty.</exception>
        /// <exception cref="ServiceConfigurationException">Thrown when the pool is not registered.</exception>
        public static async Task<UpstreamServer?> SelectUpstreamAsync(
            this UpstreamManagerService service,
            string poolId,
            Func<UpstreamServer, bool>? filter = null,
            string? clientIp = null)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentException.ThrowIfNullOrEmpty(poolId);

            var context = new UpstreamSelectionContext(poolId, clientIp);
            var server = await service.SelectUpstreamAsync(context);

            if (server is null || filter is null)
            {
                return server;
            }

            // If a filter is provided and the selected server doesn't match, try to find one that does
            var pool = await service.GetPoolAsync(poolId);
            return pool is null
                ? null
                : pool.GetAvailableServers().FirstOrDefault(filter);
        }

        /// <summary>
        /// Records upstream results for multiple servers in a single batch operation.
        /// </summary>
        /// <param name="service">The upstream manager service instance.</param>
        /// <param name="results">Collection of results to record.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> or <paramref name="results"/> is null.</exception>
        public static async Task RecordUpstreamResultsAsync(
            this UpstreamManagerService service,
            IEnumerable<(string poolId, string UpstreamId, bool Succeeded, int ResponseTimeMs)> results)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(results);

            foreach (var (poolId, upstreamId, succeeded, responseTimeMs) in results)
            {
                await service.RecordUpstreamResultAsync(poolId, upstreamId, succeeded, responseTimeMs);
            }
        }

        /// <summary>
        /// Gets all unhealthy upstreams across all pools.
        /// </summary>
        /// <param name="service">The upstream manager service instance.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of unhealthy upstream identifiers.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> is null.</exception>
        public static async Task<IReadOnlyList<string>> GetUnhealthyUpstreamIdsAsync(
            this UpstreamManagerService service,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(service);

            var pools = await service.GetAllPoolsAsync();
            var unhealthyIds = new List<string>();

            foreach (var pool in pools)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var report = await service.GetHealthReportAsync(pool.Id, cancellationToken);

                unhealthyIds.AddRange(report.Upstreams
                    .Where(upstream => !upstream.IsHealthy)
                    .Select(upstream => upstream.UpstreamId));
            }

            return unhealthyIds.AsReadOnly();
        }
    }

    /// <summary>
    /// Represents a summary view of an upstream pool for monitoring and reporting purposes.
    /// </summary>
    /// <param name="poolId">The unique identifier of the pool.</param>
    /// <param name="name">The human-readable name of the pool.</param>
    /// <param name="serviceId">The service this pool belongs to.</param>
    /// <param name="totalUpstreams">Total number of upstreams in the pool.</param>
    /// <param name="healthyUpstreams">Number of healthy upstreams.</param>
    /// <param name="availableUpstreams">Number of available upstreams for routing.</param>
    /// <param name="totalActiveConnections">Total active connections across all upstreams.</param>
    /// <param name="isEnabled">Whether the pool is enabled.</param>
    public sealed record PoolSummary(
        string poolId,
        string name,
        string serviceId,
        int totalUpstreams,
        int healthyUpstreams,
        int availableUpstreams,
        int totalActiveConnections,
        bool isEnabled);
}