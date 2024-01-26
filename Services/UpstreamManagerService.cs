// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.LoadBalancing;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Central engine for v2 dynamic upstream management.
    /// Coordinates upstream pool registration, health-aware request routing, active health probing,
    /// graceful connection draining, and Caddy reverse-proxy config generation.
    /// Integrates with the existing <see cref="HealthMonitoringService"/> and
    /// <see cref="CaddyConfigurationService"/> to slot into the v1.x infrastructure without
    /// requiring changes to those services.
    /// </summary>
    public sealed class UpstreamManagerService
    {
        private readonly ServiceManagementService _serviceManager;
        private readonly HealthMonitoringService _healthMonitor;
        private readonly CaddyConfigurationService _caddyConfig;
        private readonly LoadBalancingOptions _options;

        // In-memory pool registry (poolId → pool). Designed for fast reads on the hot request path.
        private readonly ConcurrentDictionary<string, UpstreamPool> _pools = new();

        // Per-pool round-robin cursor. Uses modular arithmetic to avoid overflow.
        private readonly ConcurrentDictionary<string, int> _rrCursors = new();

        // Per-upstream drain semaphores prevent concurrent drain operations on the same server.
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _drainLocks = new();

        // ─── Construction ─────────────────────────────────────────────────────

        /// <summary>
        /// Initialises a new <see cref="UpstreamManagerService"/> with the required service dependencies.
        /// </summary>
        /// <param name="serviceManager">Service registry for validating pool ↔ service associations.</param>
        /// <param name="healthMonitor">Health monitoring integration for cross-service correlation.</param>
        /// <param name="caddyConfig">Caddy config service used when writing generated configurations to disk.</param>
        /// <param name="options">Runtime load-balancing and health-check configuration.</param>
        public UpstreamManagerService(
            ServiceManagementService serviceManager,
            HealthMonitoringService healthMonitor,
            CaddyConfigurationService caddyConfig,
            LoadBalancingOptions options)
        {
            _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
            _healthMonitor  = healthMonitor  ?? throw new ArgumentNullException(nameof(healthMonitor));
            _caddyConfig    = caddyConfig    ?? throw new ArgumentNullException(nameof(caddyConfig));
            _options        = options        ?? throw new ArgumentNullException(nameof(options));
        }

        // ─── Pool Lifecycle ────────────────────────────────────────────────────

        /// <summary>
        /// Registers a new upstream pool, validates it against the referenced managed service,
        /// applies global option defaults to fields the pool left unset, and caches it for routing.
        /// </summary>
        /// <param name="pool">The pool to register. Must reference a valid <see cref="ManagedService.Id"/>.</param>
        /// <returns>The pool identifier (unchanged from <see cref="UpstreamPool.Id"/>).</returns>
        /// <exception cref="ServiceNotFoundException">The referenced service does not exist.</exception>
        /// <exception cref="ServiceConfigurationException">Pool validation failed.</exception>
        public async Task<string> RegisterPoolAsync(UpstreamPool pool)
        {
            ArgumentNullException.ThrowIfNull(pool);

            try
            {
                pool.Validate();
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {
                throw new ServiceConfigurationException($"Upstream pool validation failed: {ex.Message}");
            }

            // Verify the referenced service exists — throws ServiceNotFoundException if absent.
            await _serviceManager.GetServiceAsync(pool.ServiceId);

            ApplyGlobalDefaults(pool);

            pool.UpdatedAt = DateTime.UtcNow;
            _pools[pool.Id] = pool;
            return pool.Id;
        }

        /// <summary>
        /// Retrieves a registered pool by its identifier, or <c>null</c> when not found.
        /// </summary>
        /// <param name="poolId">The unique pool identifier.</param>
        public Task<UpstreamPool?> GetPoolAsync(string poolId)
        {
            _pools.TryGetValue(poolId, out var pool);
            return Task.FromResult(pool);
        }

        /// <summary>
        /// Returns all pools currently registered for the specified managed service.
        /// </summary>
        /// <param name="serviceId">The service identifier to filter by.</param>
        public Task<List<UpstreamPool>> GetPoolsForServiceAsync(string serviceId) =>
            Task.FromResult(_pools.Values.Where(p => p.ServiceId == serviceId).ToList());

        /// <summary>
        /// Returns all currently registered upstream pools.
        /// </summary>
        public Task<List<UpstreamPool>> GetAllPoolsAsync() =>
            Task.FromResult(_pools.Values.ToList());

        /// <summary>
        /// Removes a pool from the registry. Returns <c>true</c> when the pool existed and was removed.
        /// </summary>
        /// <param name="poolId">The pool to remove.</param>
        public Task<bool> RemovePoolAsync(string poolId) =>
            Task.FromResult(_pools.TryRemove(poolId, out _));

        // ─── Health-Aware Request Routing ─────────────────────────────────────

        /// <summary>
        /// Selects the best available upstream server from the specified pool using its configured
        /// load-balancing strategy, factoring in real-time health state and active-connection counts.
        /// </summary>
        /// <param name="context">
        /// Per-request context including the target pool ID, optional client IP (for IP-hash strategy),
        /// and optional session token (for sticky-session routing).
        /// </param>
        /// <returns>
        /// The selected <see cref="UpstreamServer"/>, or <c>null</c> when the circuit breaker is open
        /// (i.e. no upstream is currently available).
        /// </returns>
        /// <exception cref="ServiceConfigurationException">The specified pool is not registered.</exception>
        public Task<UpstreamServer?> SelectUpstreamAsync(UpstreamSelectionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!_pools.TryGetValue(context.PoolId, out var pool))
                throw new ServiceConfigurationException($"Upstream pool '{context.PoolId}' is not registered");

            var candidates = pool.GetAvailableServers();

            if (candidates.Count == 0)
            {
                if (_options.CircuitBreakerEnabled)
                    return Task.FromResult<UpstreamServer?>(null);

                // Circuit breaker is off — fall back to all non-disabled servers and try anyway.
                candidates = pool.Servers
                    .Where(s => s.Status != UpstreamServerStatus.Disabled)
                    .ToList();

                if (candidates.Count == 0)
                    return Task.FromResult<UpstreamServer?>(null);
            }

            var selected = pool.Strategy switch
            {
                LoadBalancingStrategy.RoundRobin       => SelectRoundRobin(candidates, context.PoolId),
                LoadBalancingStrategy.LeastConnections  => SelectLeastConnections(candidates),
                LoadBalancingStrategy.Random            => SelectRandom(candidates),
                LoadBalancingStrategy.WeightedRandom    => SelectWeightedRandom(candidates),
                LoadBalancingStrategy.IpHash            => SelectByIpHash(candidates, context.ClientIp),
                _                                       => SelectRoundRobin(candidates, context.PoolId)
            };

            return Task.FromResult<UpstreamServer?>(selected);
        }

        /// <summary>
        /// Records the outcome of a proxied request or health probe for a specific upstream and
        /// applies threshold logic to automatically promote or demote its health status.
        /// </summary>
        /// <param name="poolId">The pool that owns the upstream.</param>
        /// <param name="upstreamId">The upstream server identifier.</param>
        /// <param name="succeeded">Whether the operation succeeded.</param>
        /// <param name="responseTimeMs">Round-trip time in milliseconds; <c>0</c> on failure.</param>
        public Task RecordUpstreamResultAsync(
            string poolId,
            string upstreamId,
            bool succeeded,
            int responseTimeMs = 0)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
                return Task.CompletedTask;

            var server = pool.Servers.FirstOrDefault(s => s.Id == upstreamId);
            if (server is null)
                return Task.CompletedTask;

            server.RecordHealthProbeResult(succeeded, responseTimeMs);

            if (!succeeded && server.ConsecutiveFailures >= _options.UnhealthyThreshold)
                server.Status = UpstreamServerStatus.Unhealthy;
            else if (succeeded
                     && server.ConsecutiveSuccesses >= _options.HealthyThreshold
                     && server.Status == UpstreamServerStatus.Unhealthy)
                server.Status = UpstreamServerStatus.Active;

            return Task.CompletedTask;
        }

        // ─── Active Health Probing ─────────────────────────────────────────────

        /// <summary>
        /// Performs active TCP health probes against every upstream server in the specified pool
        /// concurrently, updates each server's health state according to configured thresholds, and
        /// returns a snapshot list reflecting the post-probe state.
        /// </summary>
        /// <param name="poolId">The pool whose upstreams should be probed.</param>
        /// <param name="cancellationToken">Token to cancel in-flight probe tasks.</param>
        /// <returns>Per-upstream health snapshots captured after probing completes.</returns>
        /// <exception cref="ServiceConfigurationException">The pool is not registered.</exception>
        public async Task<List<UpstreamHealthSnapshot>> ProbeAllUpstreamsAsync(
            string poolId,
            CancellationToken cancellationToken = default)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
                throw new ServiceConfigurationException($"Upstream pool '{poolId}' is not registered");

            var probeTasks = pool.Servers
                .Select(s => ProbeUpstreamAsync(pool, s, cancellationToken));

            var snapshots = await Task.WhenAll(probeTasks);
            return snapshots.ToList();
        }

        /// <summary>
        /// Probes every upstream in every registered pool and returns consolidated health reports.
        /// Useful for background workers that need a full-system health refresh on a schedule.
        /// </summary>
        /// <param name="cancellationToken">Token to abort probing.</param>
        public async Task<List<UpstreamPoolHealthReport>> ProbeAllPoolsAsync(
            CancellationToken cancellationToken = default)
        {
            var tasks = _pools.Keys
                .Select(id => BuildHealthReportAfterProbeAsync(id, cancellationToken));
            return (await Task.WhenAll(tasks)).ToList();
        }

        // ─── Connection Draining ──────────────────────────────────────────────

        /// <summary>
        /// Initiates a graceful drain on the specified upstream server. The server is immediately
        /// marked <see cref="UpstreamServerStatus.Draining"/> so no new requests are routed to it.
        /// The method then waits up to <see cref="LoadBalancingOptions.ConnectionDrainTimeoutSeconds"/>
        /// for active connections to finish before forcing the server to
        /// <see cref="UpstreamServerStatus.Disabled"/>.
        /// </summary>
        /// <param name="poolId">The owning pool's identifier.</param>
        /// <param name="upstreamId">The upstream server to drain.</param>
        /// <param name="cancellationToken">Token to abort the drain wait.</param>
        /// <exception cref="ServiceConfigurationException">Pool or upstream not found.</exception>
        public async Task DrainUpstreamAsync(
            string poolId,
            string upstreamId,
            CancellationToken cancellationToken = default)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
                throw new ServiceConfigurationException($"Upstream pool '{poolId}' is not registered");

            var server = pool.Servers.FirstOrDefault(s => s.Id == upstreamId)
                ?? throw new ServiceConfigurationException($"Upstream '{upstreamId}' not found in pool '{poolId}'");

            server.Status    = UpstreamServerStatus.Draining;
            server.UpdatedAt = DateTime.UtcNow;

            var semaphore = _drainLocks.GetOrAdd(upstreamId, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(_options.ConnectionDrainTimeoutSeconds);
                while (server.ActiveConnections > 0 && DateTime.UtcNow < deadline)
                    await Task.Delay(250, cancellationToken);

                server.Status    = UpstreamServerStatus.Disabled;
                server.UpdatedAt = DateTime.UtcNow;
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Re-activates a previously drained or disabled upstream server,
        /// resetting its consecutive counters so it enters the pool as a fresh candidate.
        /// </summary>
        /// <param name="poolId">The owning pool's identifier.</param>
        /// <param name="upstreamId">The upstream server to re-enable.</param>
        public Task ReactivateUpstreamAsync(string poolId, string upstreamId)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
                throw new ServiceConfigurationException($"Upstream pool '{poolId}' is not registered");

            var server = pool.Servers.FirstOrDefault(s => s.Id == upstreamId)
                ?? throw new ServiceConfigurationException($"Upstream '{upstreamId}' not found in pool '{poolId}'");

            server.Status               = UpstreamServerStatus.Active;
            server.IsHealthy            = true;
            server.ConsecutiveFailures  = 0;
            server.ConsecutiveSuccesses = 0;
            server.UpdatedAt            = DateTime.UtcNow;

            return Task.CompletedTask;
        }

        // ─── Caddy Config Generation ──────────────────────────────────────────

        /// <summary>
        /// Generates a Caddy <c>reverse_proxy</c> block for the specified pool, incorporating
        /// all health-aware upstream state and pool-level settings (LB policy, retries, health probes).
        /// </summary>
        /// <param name="poolId">The pool to generate configuration for.</param>
        /// <param name="matchPath">The Caddyfile path matcher. Defaults to <c>"/*"</c>.</param>
        /// <returns>A multi-line Caddyfile block ready to embed in a site block.</returns>
        /// <exception cref="ServiceConfigurationException">The pool is not registered.</exception>
        public Task<string> GenerateCaddyConfigForPoolAsync(string poolId, string matchPath = "/*")
        {
            if (!_pools.TryGetValue(poolId, out var pool))
                throw new ServiceConfigurationException($"Upstream pool '{poolId}' is not registered");

            return Task.FromResult(pool.GenerateCaddyUpstreamBlock(matchPath));
        }

        /// <summary>
        /// Generates a complete Caddyfile site block for all enabled pools belonging to a service,
        /// placing each pool's <c>reverse_proxy</c> block inside the site's domain header.
        /// Optionally writes the result to disk via <see cref="CaddyConfigurationService"/>.
        /// </summary>
        /// <param name="serviceId">The service whose pools should be rendered.</param>
        /// <param name="domain">The domain name Caddy should listen on (e.g. <c>"api.example.com"</c>).</param>
        /// <param name="writeToFile">
        /// When <c>true</c>, the generated block is appended to the Caddyfile at the default path.
        /// </param>
        /// <returns>The generated Caddyfile site block as a string.</returns>
        public async Task<string> GenerateCaddyConfigForServiceAsync(
            string serviceId,
            string domain,
            bool writeToFile = false)
        {
            await _serviceManager.GetServiceAsync(serviceId);

            var pools  = await GetPoolsForServiceAsync(serviceId);
            var active = pools.Where(p => p.IsEnabled).ToList();

            if (active.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine($"{domain} {{");

            foreach (var pool in active)
                sb.Append(pool.GenerateCaddyUpstreamBlock("/*"));

            sb.AppendLine("}");

            var config = sb.ToString();

            if (writeToFile)
            {
                var existing = await _caddyConfig.ReadCaddyfileAsync(Core.AppConstants.CaddyfilePath);
                await _caddyConfig.WriteCaddyfileAsync(
                    existing + Environment.NewLine + config,
                    Core.AppConstants.CaddyfilePath);
            }

            return config;
        }

        // ─── Health Reporting ─────────────────────────────────────────────────

        /// <summary>
        /// Produces an <see cref="UpstreamPoolHealthReport"/> capturing the current health state of
        /// every upstream in the specified pool without performing any new probes.
        /// </summary>
        /// <param name="poolId">The pool to report on.</param>
        /// <param name="cancellationToken">Not used; retained for interface symmetry.</param>
        public Task<UpstreamPoolHealthReport> GetHealthReportAsync(
            string poolId,
            CancellationToken cancellationToken = default)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
                throw new ServiceConfigurationException($"Upstream pool '{poolId}' is not registered");

            return Task.FromResult(BuildReport(pool));
        }

        /// <summary>
        /// Returns health reports for all registered pools.
        /// </summary>
        public Task<List<UpstreamPoolHealthReport>> GetAllHealthReportsAsync()
        {
            var reports = _pools.Values.Select(BuildReport).ToList();
            return Task.FromResult(reports);
        }

        // ─── Private — Strategy Selectors ─────────────────────────────────────

        private UpstreamServer SelectRoundRobin(List<UpstreamServer> servers, string poolId)
        {
            // Atomically advance the cursor and wrap at the current server count.
            var idx = _rrCursors.AddOrUpdate(
                poolId,
                addValue: 0,
                updateValueFactory: (_, current) => (current + 1) % servers.Count);

            return servers[idx % servers.Count];
        }

        private static UpstreamServer SelectLeastConnections(List<UpstreamServer> servers) =>
            servers.MinBy(s => s.ActiveConnections) ?? servers[0];

        private static UpstreamServer SelectRandom(List<UpstreamServer> servers) =>
            servers[Random.Shared.Next(servers.Count)];

        private static UpstreamServer SelectWeightedRandom(List<UpstreamServer> servers)
        {
            var totalWeight = servers.Sum(s => s.Weight);
            var roll        = Random.Shared.Next(totalWeight);
            var cumulative  = 0;

            foreach (var server in servers)
            {
                cumulative += server.Weight;
                if (roll < cumulative)
                    return server;
            }

            return servers[^1];
        }

        private static UpstreamServer SelectByIpHash(List<UpstreamServer> servers, string? clientIp)
        {
            if (string.IsNullOrWhiteSpace(clientIp))
                return SelectRandom(servers);

            var hash = Math.Abs(clientIp.GetHashCode(StringComparison.Ordinal));
            return servers[hash % servers.Count];
        }

        // ─── Private — Probing ────────────────────────────────────────────────

        private async Task<UpstreamHealthSnapshot> ProbeUpstreamAsync(
            UpstreamPool pool,
            UpstreamServer server,
            CancellationToken cancellationToken)
        {
            var sw      = Stopwatch.StartNew();
            var success = false;

            try
            {
                using var tcp         = new TcpClient();
                var connectTask       = tcp.ConnectAsync(server.Address, server.Port);
                var timeoutTask       = Task.Delay(_options.HealthProbeTimeoutMs, cancellationToken);
                var completed         = await Task.WhenAny(connectTask, timeoutTask);

                success = completed == connectTask && !connectTask.IsFaulted;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                success = false;
            }
            finally
            {
                sw.Stop();
            }

            await RecordUpstreamResultAsync(pool.Id, server.Id, success, (int)sw.ElapsedMilliseconds);

            return new UpstreamHealthSnapshot(
                server.Id,
                server.GetUpstreamAddress(),
                server.IsHealthy,
                server.Status,
                server.ConsecutiveFailures,
                server.AverageResponseTimeMs,
                server.ActiveConnections,
                server.LastCheckedAt
            );
        }

        private async Task<UpstreamPoolHealthReport> BuildHealthReportAfterProbeAsync(
            string poolId,
            CancellationToken cancellationToken)
        {
            await ProbeAllUpstreamsAsync(poolId, cancellationToken);
            return await GetHealthReportAsync(poolId, cancellationToken);
        }

        // ─── Private — Helpers ────────────────────────────────────────────────

        private static UpstreamPoolHealthReport BuildReport(UpstreamPool pool)
        {
            var snapshots = pool.Servers.Select(s => new UpstreamHealthSnapshot(
                s.Id,
                s.GetUpstreamAddress(),
                s.IsHealthy,
                s.Status,
                s.ConsecutiveFailures,
                s.AverageResponseTimeMs,
                s.ActiveConnections,
                s.LastCheckedAt
            )).ToList();

            return new UpstreamPoolHealthReport(
                pool.Id,
                pool.Name,
                pool.Strategy,
                TotalUpstreams:        pool.Servers.Count,
                HealthyUpstreams:      pool.Servers.Count(s => s.IsHealthy),
                AvailableUpstreams:    pool.GetAvailableServers().Count,
                TotalActiveConnections: pool.GetTotalActiveConnections(),
                Upstreams:             snapshots,
                GeneratedAt:           DateTime.UtcNow
            );
        }

        private void ApplyGlobalDefaults(UpstreamPool pool)
        {
            if (pool.HealthCheckIntervalSeconds == 30)
                pool.HealthCheckIntervalSeconds = _options.HealthCheckIntervalSeconds;

            if (pool.UnhealthyThreshold == 3)
                pool.UnhealthyThreshold = _options.UnhealthyThreshold;

            if (pool.HealthyThreshold == 2)
                pool.HealthyThreshold = _options.HealthyThreshold;

            if (pool.MaxRetries == 2)
                pool.MaxRetries = _options.MaxRetries;

            if (pool.RetryDurationSeconds == 30)
                pool.RetryDurationSeconds = _options.RetryDurationSeconds;

            if (_options.StickySessionEnabled && string.IsNullOrWhiteSpace(pool.StickyCookieName))
                pool.StickyCookieName = _options.DefaultStickyCookieName;
        }
    }
}
