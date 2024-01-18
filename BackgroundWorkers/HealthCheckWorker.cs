// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.Services;
using CaddyVpsToolkit.Middleware;

namespace CaddyVpsToolkit.BackgroundWorkers
{
    /// <summary>
    /// Background worker that periodically performs health checks on all services.
    /// Runs on a configurable interval and logs results for monitoring.
    /// </summary>
    public class HealthCheckWorker : IBackgroundWorker
    {
        private readonly HealthMonitoringService _healthMonitor;
        private readonly ServiceManagementService _serviceManager;
        private readonly ILogger _logger;
        private readonly int _intervalSeconds;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _workerTask;

        public HealthCheckWorker(
            HealthMonitoringService healthMonitor,
            ServiceManagementService serviceManager,
            ILogger logger,
            int intervalSeconds = 300)
        {
            _healthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
            _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _intervalSeconds = intervalSeconds;
        }

        public async Task StartAsync()
        {
            if (_cancellationTokenSource != null)
                return; // Already running

            _cancellationTokenSource = new CancellationTokenSource();
            _workerTask = RunWorkerAsync(_cancellationTokenSource.Token);

            await _logger.LogInfoAsync($"Health check worker started (interval: {_intervalSeconds}s)");
        }

        public async Task StopAsync()
        {
            if (_cancellationTokenSource == null)
                return;

            _cancellationTokenSource.Cancel();
            try
            {
                await _workerTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            _workerTask = null;

            await _logger.LogInfoAsync("Health check worker stopped");
        }

        private async Task RunWorkerAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var services = await _serviceManager.GetAllServicesAsync();

                    foreach (var service in services)
                    {
                        if (!service.IsEnabled)
                            continue;

                        try
                        {
                            var result = await _healthMonitor.CheckServiceHealthAsync(service.Id);
                            if (!result.IsHealthy)
                            {
                                await _logger.LogWarningAsync(
                                    $"Service '{service.Name}' is unhealthy. Response: {result.ResponseTimeMs}ms");
                            }
                        }
                        catch (Exception ex)
                        {
                            await _logger.LogErrorAsync($"Health check failed for service '{service.Name}': {ex.Message}");
                        }
                    }

                    // Wait for next interval or until cancellation
                    await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw; // Re-throw cancellation exceptions
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"Health check worker error: {ex.Message}");
                }
            }
        }

        public bool IsRunning => _cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested;

        public string WorkerName => "HealthCheckWorker";
    }
}
