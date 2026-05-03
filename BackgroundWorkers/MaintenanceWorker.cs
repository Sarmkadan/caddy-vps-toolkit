// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Middleware;

namespace CaddyVpsToolkit.BackgroundWorkers
{
    /// <summary>
    /// Background worker for cleanup and maintenance tasks.
    /// Removes old health check records and performs database optimization.
    /// Runs periodically to maintain system performance.
    /// </summary>
    public class MaintenanceWorker : IBackgroundWorker
    {
        private readonly IHealthCheckRepository _healthCheckRepo;
        private readonly ILogger _logger;
        private readonly int _intervalSeconds;
        private readonly int _retentionDays;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _workerTask;

        public MaintenanceWorker(
            IHealthCheckRepository healthCheckRepo,
            ILogger logger,
            int intervalSeconds = 3600,
            int retentionDays = 30)
        {
            _healthCheckRepo = healthCheckRepo ?? throw new ArgumentNullException(nameof(healthCheckRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _intervalSeconds = intervalSeconds;
            _retentionDays = retentionDays;
        }

        public async Task StartAsync()
        {
            if (_cancellationTokenSource != null)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            _workerTask = RunWorkerAsync(_cancellationTokenSource.Token);

            await _logger.LogInfoAsync($"Maintenance worker started (interval: {_intervalSeconds}s, retention: {_retentionDays}d)");
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
                // Expected
            }

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            _workerTask = null;

            await _logger.LogInfoAsync("Maintenance worker stopped");
        }

        private async Task RunWorkerAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await PerformMaintenanceAsync();

                    // Wait for next interval
                    await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"Maintenance worker error: {ex.Message}");
                }
            }
        }

        private async Task PerformMaintenanceAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

            try
            {
                // Clean old health check records
                var deletedCount = await _healthCheckRepo.DeleteOlderThanAsync(cutoffDate);
                if (deletedCount > 0)
                {
                    await _logger.LogInfoAsync($"Cleaned {deletedCount} old health check records");
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Failed to clean old health records: {ex.Message}");
            }
        }

        public bool IsRunning => _cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested;

        public string WorkerName => "MaintenanceWorker";
    }
}
