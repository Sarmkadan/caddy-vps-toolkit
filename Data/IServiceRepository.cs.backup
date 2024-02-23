#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Data
{
    /// <summary>
    /// Repository interface for managing services
    /// </summary>
    public interface IServiceRepository
    {
        Task<ManagedService> GetByIdAsync(string id);
        Task<ManagedService> GetByNameAsync(string name);
        Task<List<ManagedService>> GetAllAsync();
        Task<List<ManagedService>> GetByTypeAsync(ServiceType type);
        Task<List<ManagedService>> GetEnabledServicesAsync();
        Task<string> AddAsync(ManagedService service);
        Task<bool> UpdateAsync(ManagedService service);
        Task<bool> DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
        Task<int> GetCountAsync();
        Task<List<ManagedService>> SearchAsync(string query);
    }

    /// <summary>
    /// Repository interface for health check results
    /// </summary>
    public interface IHealthCheckRepository
    {
        Task<HealthCheckResult> GetLatestAsync(string serviceId);
        Task<List<HealthCheckResult>> GetRecentAsync(string serviceId, int hours);
        Task<List<HealthCheckResult>> GetByServiceIdAsync(string serviceId);
        Task<string> AddAsync(HealthCheckResult result);
        Task<bool> DeleteOlderThanAsync(DateTime cutoffDate);
        Task<HealthCheckStatistics> GetStatisticsAsync(string serviceId, DateTime from, DateTime to);
    }

    /// <summary>
    /// Repository interface for application configuration
    /// </summary>
    public interface IConfigurationRepository
    {
        Task<string> GetValueAsync(string key);
        Task SetValueAsync(string key, string value);
        Task<bool> DeleteAsync(string key);
        Task<Dictionary<string, string>> GetAllAsync();
    }

    public sealed class HealthCheckStatistics
    {
        public int TotalChecks { get; set; }
        public int SuccessfulChecks { get; set; }
        public int FailedChecks { get; set; }
        public double SuccessRate { get; set; }
        public int AverageResponseTimeMs { get; set; }
        public int MaxResponseTimeMs { get; set; }
        public int MinResponseTimeMs { get; set; }
    }
}
