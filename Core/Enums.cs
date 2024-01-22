// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace CaddyVpsToolkit.Core
{
    /// <summary>
    /// Service status enumeration
    /// </summary>
    public enum ServiceStatus
    {
        Stopped = 0,
        Running = 1,
        Restarting = 2,
        Failed = 3,
        Unknown = 4
    }

    /// <summary>
    /// Service type enumeration
    /// </summary>
    public enum ServiceType
    {
        WebApplication = 0,
        ApiService = 1,
        Worker = 2,
        Database = 3,
        Cache = 4,
        Custom = 5
    }

    /// <summary>
    /// Health check type enumeration
    /// </summary>
    public enum HealthCheckType
    {
        Http = 0,
        Tcp = 1,
        Exec = 2,
        Grpc = 3
    }
}
