#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using CaddyVpsToolkit.Configuration;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Service for monitoring health of managed services.
    /// </summary>
    public sealed class HealthMonitoringService
    {
        private readonly IHealthCheckRepository _repository;
        private readonly ServiceManagementService _serviceManager;
        private readonly HttpClient _httpClient;
        private readonly UpstreamManagementOptions _upstreamOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthMonitoringService"/> class.
        /// </summary>
        /// <param name="repository">The health check repository.</param>
        /// <param name="serviceManager">The service management service.</param>
        /// <param name="upstreamOptions">The upstream management options containing maintenance window configuration.</param>
        public HealthMonitoringService(IHealthCheckRepository repository, ServiceManagementService serviceManager, UpstreamManagementOptions upstreamOptions)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
            _upstreamOptions = upstreamOptions ?? throw new ArgumentNullException(nameof(upstreamOptions));
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(AppConstants.HealthCheckSocketTimeoutMs / 1000) };
        }

        /// <summary>
        /// Determines if the system is currently in a maintenance window.
        /// </summary>
        /// <returns>True if in maintenance window, otherwise false.</returns>
        private bool IsInMaintenanceWindow()
        {
            return _upstreamOptions.IsMaintenanceWindowActive();
        }

        /// <summary>
        /// Perform an HTTP health check for a service.
        /// </summary>
        /// <param name="serviceId">The ID of the service to check.</param>
        /// <returns>The health check result.</returns>
        public async Task<HealthCheckResult> CheckServiceHealthAsync(string serviceId)
        {
            try
            {
                var service = await _serviceManager.GetServiceAsync(serviceId);
                if (service?.HealthCheck is null)
                    throw new HealthCheckException(serviceId, "No health check configured for this service");

                service.HealthCheck.Validate();

                var result = service.HealthCheck.Type switch
                {
                    HealthCheckType.Http => await CheckHttpHealthAsync(service),
                    HealthCheckType.Tcp => await CheckTcpHealthAsync(service),
                    _ => throw new System.NotSupportedException($"Health check type '{service.HealthCheck.Type}' is not supported")
                };

                result.ServiceId = serviceId;
                result.CheckType = service.HealthCheck.Type.ToString();
                result.Endpoint = service.HealthCheck.Endpoint;

                await _repository.AddAsync(result);
                return result;
            }
            catch (ServiceNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // During maintenance windows, log failures but do not throw exceptions to prevent alerts/state transitions
                if (IsInMaintenanceWindow())
                {
                    var maintenanceResult = HealthCheckResult.CreateFailure(serviceId, ex.Message);
                    maintenanceResult.Status = HealthCheckStatus.Degraded;
                    maintenanceResult.ErrorMessage = $"Maintenance window active - failure logged but not acted upon: {ex.Message}";
                    await _repository.AddAsync(maintenanceResult);
                    return maintenanceResult;
                }

                var failureResult = HealthCheckResult.CreateFailure(serviceId, ex.Message);
                await _repository.AddAsync(failureResult);
                throw new HealthCheckException(serviceId, ex.Message);
            }
        }


        /// <summary>
        /// Get latest health status for a service.
        /// </summary>
        /// <param name="serviceId">The ID of the service.</param>
        /// <returns>The latest health check result.</returns>
        public async Task<HealthCheckResult> GetLatestHealthStatusAsync(string serviceId)
        {
            return await _repository.GetLatestAsync(serviceId);
        }

        /// <summary>
        /// Get health check history for a service.
        /// </summary>
        /// <param name="serviceId">The ID of the service.</param>
        /// <param name="hours">The number of hours of history to retrieve.</param>
        /// <returns>A list of health check results.</returns>
        public async Task<List<HealthCheckResult>> GetHealthHistoryAsync(string serviceId, int hours)
        {
            if (hours < 1)
                throw new ArgumentException("Hours must be at least 1", nameof(hours));

            return await _repository.GetRecentAsync(serviceId, hours);
        }

        /// <summary>
        /// Get health statistics for a service.
        /// </summary>
        /// <param name="serviceId">The ID of the service.</param>
        /// <param name="from">The start time.</param>
        /// <param name="to">The end time.</param>
        /// <returns>The health check statistics.</returns>
        public async Task<HealthCheckStatistics> GetHealthStatisticsAsync(string serviceId, DateTime from, DateTime to)
        {
            if (from >= to)
                throw new ArgumentException("'from' must be before 'to'");

            return await _repository.GetStatisticsAsync(serviceId, from, to);
        }

        /// <summary>
        /// Clean up old health check records.
        /// </summary>
        /// <param name="daysToKeep">The number of days to keep records.</param>
        /// <returns>True if the cleanup was successful, otherwise false.</returns>
        public async Task<bool> CleanupOldRecordsAsync(int daysToKeep = 30)
        {
            if (daysToKeep < 1)
                throw new ArgumentException("Days to keep must be at least 1", nameof(daysToKeep));

            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            return await _repository.DeleteOlderThanAsync(cutoffDate);
        }

        /// <summary>
        /// Check health of all services.
        /// </summary>
        /// <returns>A list of health check results for all services.</returns>
        public async Task<List<HealthCheckResult>> CheckAllServicesHealthAsync()
        {
            var results = new List<HealthCheckResult>();
            var services = await _serviceManager.GetEnabledServicesAsync();

            foreach (var service in services)
            {
                try
                {
                    var result = await CheckServiceHealthAsync(service.Id);
                    results.Add(result);
                }
                catch
                {
                    // Continue checking other services even if one fails
                }
            }

            return results;
        }

        /// <summary>
        /// Get health summary for all services.
        /// </summary>
        /// <returns>The health summary.</returns>
        public async Task<HealthSummary> GetHealthSummaryAsync()
        {
            var summary = new HealthSummary();
            var services = await _serviceManager.GetAllServicesAsync();

            foreach (var service in services)
            {
                summary.TotalServices++;

                if (!service.IsEnabled)
                {
                    summary.DisabledServices++;
                    continue;
                }

                var latestCheck = await _repository.GetLatestAsync(service.Id);
                if (latestCheck is null)
                {
                    summary.UncheckedServices++;
                }
                else if (latestCheck.IsHealthy)
                {
                    summary.HealthyServices++;
                }
                else
                {
                    summary.UnhealthyServices++;
                }
            }

            return summary;
        }

        private async Task<HealthCheckResult> CheckHttpHealthAsync(ManagedService service)
        {
            var unitName = service.GetSystemdUnitName();
            var systemdState = await GetSystemdActiveStateAsync(unitName);
            if (systemdState == "activating" || systemdState == "reloading")
            {
                return new HealthCheckResult
                {
                    ServiceId = service.Id,
                    IsHealthy = true,
                    Status = HealthCheckStatus.Degraded,
                    ResponseTimeMs = 0,
                    ErrorMessage = $"Service '{service.Name}' is restarting (systemd state: {systemdState}); HTTP probe deferred to avoid false positive"
                };
            }

            var config = service.HealthCheck;
            var url = config.GetHealthCheckUrl(service.HostBinding, service.Port);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var request = new HttpRequestMessage(new HttpMethod(config.HttpMethod), url);
                using var response = await _httpClient.SendAsync(request);
                stopwatch.Stop();

                var isHealthy = (int)response.StatusCode == config.ExpectedHttpStatus;
                var result = new HealthCheckResult
                {
                    IsHealthy = isHealthy,
                    Status = isHealthy ? HealthCheckStatus.Healthy : HealthCheckStatus.Unhealthy,
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    HttpStatusCode = (int)response.StatusCode
                };

                if (!string.IsNullOrWhiteSpace(config.ExpectedResponse))
                {
                    var content = await response.Content.ReadAsStringAsync();
                    result.ResponseBody = content;
                    if (!content.Contains(config.ExpectedResponse))
                        result.IsHealthy = false;
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                return HealthCheckResult.CreateFailure(
                    service.Id,
                    $"HTTP health check failed for {service.Name} ({url}): {ex.Message}",
                    (int)stopwatch.ElapsedMilliseconds);
            }
            catch (TaskCanceledException)
            {
                stopwatch.Stop();
                return HealthCheckResult.CreateFailure(
                    service.Id,
                    $"Health check timed out for {service.Name} ({url}) after {config.TimeoutSeconds}s",
                    config.TimeoutSeconds * 1000);
            }
        }

        private static async Task<string> GetSystemdActiveStateAsync(string unitName)
        {
            try
            {
                var result = await ProcessUtilities.ExecuteAsync("systemctl", $"is-active {unitName}", 5000);
                return result.Output.Trim().ToLowerInvariant();
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task<HealthCheckResult> CheckTcpHealthAsync(ManagedService service)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    var connectTask = client.ConnectAsync(service.HostBinding, service.Port);
                    var completedTask = await Task.WhenAny(
                        connectTask,
                        Task.Delay(AppConstants.HealthCheckSocketTimeoutMs)
                    );

                    stopwatch.Stop();

                    if (completedTask == connectTask)
                    {
                        return HealthCheckResult.CreateSuccess(service.Id, (int)stopwatch.ElapsedMilliseconds);
                    }
                    else
                    {
                        return HealthCheckResult.CreateFailure(
                            service.Id,
                            $"TCP connection to {service.Name} ({service.HostBinding}:{service.Port}) timed out after {AppConstants.HealthCheckSocketTimeoutMs}ms");
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return HealthCheckResult.CreateFailure(service.Id, $"TCP check failed: {ex.Message}", (int)stopwatch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// Represents the health summary for all services.
    /// </summary>
    public sealed class HealthSummary
    {
        public int TotalServices { get; set; }
        public int HealthyServices { get; set; }
        public int UnhealthyServices { get; set; }
        public int UncheckedServices { get; set; }
        public int DisabledServices { get; set; }

        public double HealthPercentage => TotalServices > 0 ? (double)HealthyServices / TotalServices * 100 : 0;
    }
}
