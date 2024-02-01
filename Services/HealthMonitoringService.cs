#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Service for monitoring health of managed services
    /// </summary>
    public sealed class HealthMonitoringService
    {
        private readonly IHealthCheckRepository _repository;
        private readonly ServiceManagementService _serviceManager;
        private readonly HttpClient _httpClient;

        public HealthMonitoringService(IHealthCheckRepository repository, ServiceManagementService serviceManager)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(AppConstants.HealthCheckSocketTimeoutMs / 1000) };
        }

        /// <summary>
        /// Perform an HTTP health check for a service
        /// </summary>
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
                var failureResult = HealthCheckResult.CreateFailure(serviceId, ex.Message);
                await _repository.AddAsync(failureResult);
                throw new HealthCheckException(serviceId, ex.Message);
            }
        }

        /// <summary>
        /// Get latest health status for a service
        /// </summary>
        public async Task<HealthCheckResult> GetLatestHealthStatusAsync(string serviceId)
        {
            return await _repository.GetLatestAsync(serviceId);
        }

        /// <summary>
        /// Get health check history for a service
        /// </summary>
        public async Task<List<HealthCheckResult>> GetHealthHistoryAsync(string serviceId, int hours)
        {
            if (hours < 1)
                throw new ArgumentException("Hours must be at least 1", nameof(hours));

            return await _repository.GetRecentAsync(serviceId, hours);
        }

        /// <summary>
        /// Get health statistics for a service
        /// </summary>
        public async Task<HealthCheckStatistics> GetHealthStatisticsAsync(string serviceId, DateTime from, DateTime to)
        {
            if (from >= to)
                throw new ArgumentException("'from' must be before 'to'");

            return await _repository.GetStatisticsAsync(serviceId, from, to);
        }

        /// <summary>
        /// Clean up old health check records
        /// </summary>
        public async Task<bool> CleanupOldRecordsAsync(int daysToKeep = 30)
        {
            if (daysToKeep < 1)
                throw new ArgumentException("Days to keep must be at least 1", nameof(daysToKeep));

            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            return await _repository.DeleteOlderThanAsync(cutoffDate);
        }

        /// <summary>
        /// Check health of all services
        /// </summary>
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
        /// Get health summary for all services
        /// </summary>
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
            // Verify systemd unit is not in a transient restart/activating state before probing
            // the HTTP endpoint. Firing the HTTP probe during a restart race causes a 502 from
            // the Caddy reverse proxy that would otherwise be reported as a permanent failure.
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
                // Fix: Ensure proper disposal of IDisposable HttpRequestMessage and HttpResponseMessage to prevent socket leaks
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

        /// <summary>
        /// Returns the systemd <c>ActiveState</c> for the given unit name by running
        /// <c>systemctl is-active</c>. Returns an empty string when systemd is unavailable
        /// or the unit is not found, which causes the HTTP probe to proceed normally.
        /// </summary>
        private static async Task<string> GetSystemdActiveStateAsync(string unitName)
        {
            try
            {
                var result = await ProcessUtilities.ExecuteAsync("systemctl", $"is-active {unitName}", 5000);
                return result.Output.Trim().ToLowerInvariant();
            }
            catch
            {
                // systemd not available or command failed; let the HTTP probe proceed
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
