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
    public class WorkerCoordinator
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
            if (string.IsNullOrEmpty(name) || worker == null)
                throw new ArgumentException("Name and worker required");

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

            await _logger.LogInfoAsync($"Starting {workers.Count} background workers");

            foreach (var worker in workers)
            {
                try
                {
                    await worker.StartAsync();
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"Failed to start worker {worker.WorkerName}: {ex.Message}");
                }
            }
        }

        public async Task StopAllAsync()
        {
            List<IBackgroundWorker> workers;
            lock (_lockObject)
            {
                workers = _workers.Values.ToList();
            }

            await _logger.LogInfoAsync($"Stopping {workers.Count} background workers");

            foreach (var worker in workers)
            {
                try
                {
                    await worker.StopAsync();
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"Error stopping worker {worker.WorkerName}: {ex.Message}");
                }
            }
        }

        public string GetStatus()
        {
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
            lock (_lockObject)
            {
                return _workers.Keys.ToList();
            }
        }

        public bool IsWorkerRunning(string name)
        {
            lock (_lockObject)
            {
                return _workers.TryGetValue(name, out var worker) && worker.IsRunning;
            }
        }
    }
}
