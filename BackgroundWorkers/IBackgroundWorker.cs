#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Threading.Tasks;

namespace CaddyVpsToolkit.BackgroundWorkers
{
    /// <summary>
    /// Interface for background workers that run asynchronously.
    /// Supports start/stop lifecycle management.
    /// </summary>
    public interface IBackgroundWorker
    {
        Task StartAsync();
        Task StopAsync();
        bool IsRunning { get; }
        string WorkerName { get; }
    }
}
