#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CaddyVpsToolkit.Middleware;

namespace CaddyVpsToolkit.BackgroundWorkers
{
    /// <summary>
    /// Coordinator for managing multiple background workers.
    /// Provides start/stop all, status monitoring, and graceful shutdown.
    /// </summary>
    public sealed class WorkerCoordinator
    {
        private readonly Dictionary<string, IBackgroundWorker> _workers = new();
        private readonly ILogger _logger;
        private readonly object _lockObject = new();

        public WorkerCoordinator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Register(string name, IBackgroundWorker worker)
        {
            if (string.IsNullOrEmpty(name) || worker is null)
                throw new ArgumentException("Name and worker required");

            _ = _logger.LogInfoAsync(
                "Register called with {Name}"
                    .Replace("{Name}", name));

            lock (_lockObject)
            {
                _workers[name] = worker;
            }
        }

        public async Task StartAllAsync()
        {
            List<IBackgroundWorker> workers;
            lock (_lockObject)
            {
                workers = _workers.Values.ToList();
            }

            await _logger.LogInfoAsync(
                "Starting {WorkerCount} background workers"
                    .Replace("{WorkerCount}", workers.Count.ToString()));

            var failureCount = 0;

            foreach (var worker in workers)
            {
                try
                {
                    await worker.StartAsync();
                    await _logger.LogDebugAsync(
                        "Worker started successfully: {WorkerName}"
                            .Replace("{WorkerName}", worker.WorkerName));
                }
                catch (Exception ex)
                {
                    failureCount++;
                    await _logger.LogErrorAsync(
                        "Failed to start worker {WorkerName}: {ExceptionType}: {ExceptionMessage}"
                            .Replace("{WorkerName}", worker.WorkerName)
                            .Replace("{ExceptionType}", ex.GetType().FullName ?? ex.GetType().Name)
                            .Replace("{ExceptionMessage}", ex.Message));
                }
            }

            if (failureCount > 0)
            {
                await _logger.LogWarningAsync(
                    "StartAllAsync completed in degraded state: {FailureCount} of {WorkerCount} workers failed to start"
                        .Replace("{FailureCount}", failureCount.ToString())
                        .Replace("{WorkerCount}", workers.Count.ToString()));
            }
            else
            {
                await _logger.LogInfoAsync(
                    "StartAllAsync completed: all {WorkerCount} workers started"
                        .Replace("{WorkerCount}", workers.Count.ToString()));
            }
        }

        public async Task StopAllAsync()
        {
            List<IBackgroundWorker> workers;
            lock (_lockObject)
            {
                workers = _workers.Values.ToList();
            }

            await _logger.LogInfoAsync(
                "Stopping {WorkerCount} background workers"
                    .Replace("{WorkerCount}", workers.Count.ToString()));

            var failureCount = 0;

            foreach (var worker in workers)
            {
                try
                {
                    await worker.StopAsync();
                    await _logger.LogDebugAsync(
                        "Worker stopped successfully: {WorkerName}"
                            .Replace("{WorkerName}", worker.WorkerName));
                }
                catch (Exception ex)
                {
                    failureCount++;
                    await _logger.LogErrorAsync(
                        "Error stopping worker {WorkerName}: {ExceptionType}: {ExceptionMessage}"
                            .Replace("{WorkerName}", worker.WorkerName)
                            .Replace("{ExceptionType}", ex.GetType().FullName ?? ex.GetType().Name)
                            .Replace("{ExceptionMessage}", ex.Message));
                }
            }

            if (failureCount > 0)
            {
                await _logger.LogWarningAsync(
                    "StopAllAsync completed in degraded state: {FailureCount} of {WorkerCount} workers failed to stop"
                        .Replace("{FailureCount}", failureCount.ToString())
                        .Replace("{WorkerCount}", workers.Count.ToString()));
            }
            else
            {
                await _logger.LogInfoAsync(
                    "StopAllAsync completed: all {WorkerCount} workers stopped"
                        .Replace("{WorkerCount}", workers.Count.ToString()));
            }
        }

        public string GetStatus()
        {
            _ = _logger.LogInfoAsync("GetStatus called");

            var lines = new List<string> { "Background Workers Status:" };

            lock (_lockObject)
            {
                foreach (var kvp in _workers)
                {
                    var status = kvp.Value.IsRunning ? "Running" : "Stopped";
                    lines.Add($"  {kvp.Key}: {status}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        public List<string> GetWorkerNames()
        {
            _ = _logger.LogInfoAsync("GetWorkerNames called");

            lock (_lockObject)
            {
                return _workers.Keys.ToList();
            }
        }

        public bool IsWorkerRunning(string name)
        {
            _ = _logger.LogInfoAsync(
                "IsWorkerRunning called with {Name}"
                    .Replace("{Name}", name ?? "null"));

            lock (_lockObject)
            {
                return _workers.TryGetValue(name, out var worker) && worker.IsRunning;
            }
        }
    }
}
