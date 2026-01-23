#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Service for managing VPS services (CRUD + status operations)
    /// </summary>
    public sealed class ServiceManagementService
    {
        private readonly IServiceRepository _repository;

        public ServiceManagementService(IServiceRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Create a new managed service
        /// </summary>
        public async Task<string> CreateServiceAsync(ManagedService service)
        {
            if (service is null)
                throw new ArgumentNullException(nameof(service));

            service.Validate();

            var existing = await _repository.GetByNameAsync(service.Name);
            if (existing is not null)
                throw new ServiceConfigurationException($"Service '{service.Name}' already exists");

            return await _repository.AddAsync(service);
        }

        /// <summary>
        /// Update an existing service
        /// </summary>
        public async Task<bool> UpdateServiceAsync(string serviceId, ManagedService updates)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
                throw new ArgumentException("Service ID is required", nameof(serviceId));

            var existing = await _repository.GetByIdAsync(serviceId);
            if (existing is null)
                throw new ServiceNotFoundException(serviceId);

            // Preserve immutable fields
            updates.Id = existing.Id;
            updates.CreatedAt = existing.CreatedAt;
            updates.Validate();

            return await _repository.UpdateAsync(updates);
        }

        /// <summary>
        /// Delete a service
        /// </summary>
        public async Task<bool> DeleteServiceAsync(string serviceId)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
                throw new ArgumentException("Service ID is required", nameof(serviceId));

            var service = await _repository.GetByIdAsync(serviceId);
            if (service is null)
                throw new ServiceNotFoundException(serviceId);

            // Prevent deletion of running services
            if (service.Status == ServiceStatus.Running)
                throw new ServiceConfigurationException("Cannot delete a running service. Stop it first.");

            return await _repository.DeleteAsync(serviceId);
        }

        /// <summary>
        /// Get service by ID
        /// </summary>
        public async Task<ManagedService> GetServiceAsync(string serviceId)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
                throw new ArgumentException("Service ID is required", nameof(serviceId));

            var service = await _repository.GetByIdAsync(serviceId);
            if (service is null)
                throw new ServiceNotFoundException(serviceId);

            return service;
        }

        /// <summary>
        /// Get all services
        /// </summary>
        public async Task<List<ManagedService>> GetAllServicesAsync()
        {
            return await _repository.GetAllAsync();
        }

        /// <summary>
        /// Get services by type
        /// </summary>
        public async Task<List<ManagedService>> GetServicesByTypeAsync(ServiceType type)
        {
            return await _repository.GetByTypeAsync(type);
        }

        /// <summary>
        /// Get enabled services only
        /// </summary>
        public async Task<List<ManagedService>> GetEnabledServicesAsync()
        {
            return await _repository.GetEnabledServicesAsync();
        }

        /// <summary>
        /// Update service status
        /// </summary>
        public async Task<bool> UpdateServiceStatusAsync(string serviceId, ServiceStatus status)
        {
            var service = await GetServiceAsync(serviceId);
            service.UpdateStatus(status);
            return await _repository.UpdateAsync(service);
        }

        /// <summary>
        /// Enable or disable a service
        /// </summary>
        public async Task<bool> SetServiceEnabledAsync(string serviceId, bool enabled)
        {
            var service = await GetServiceAsync(serviceId);
            service.IsEnabled = enabled;
            service.UpdatedAt = DateTime.UtcNow;
            return await _repository.UpdateAsync(service);
        }

        /// <summary>
        /// Set auto-start flag for a service
        /// </summary>
        public async Task<bool> SetAutoStartAsync(string serviceId, bool autoStart)
        {
            var service = await GetServiceAsync(serviceId);
            service.AutoStart = autoStart;
            service.UpdatedAt = DateTime.UtcNow;
            return await _repository.UpdateAsync(service);
        }

        /// <summary>
        /// Search services by name or description
        /// </summary>
        public async Task<List<ManagedService>> SearchServicesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await _repository.GetAllAsync();

            return await _repository.SearchAsync(query);
        }

        /// <summary>
        /// Get service count
        /// </summary>
        public async Task<int> GetServiceCountAsync()
        {
            return await _repository.GetCountAsync();
        }

        /// <summary>
        /// Check if service exists
        /// </summary>
        public async Task<bool> ServiceExistsAsync(string serviceId)
        {
            return await _repository.ExistsAsync(serviceId);
        }

        /// <summary>
        /// Get running services count
        /// </summary>
        public async Task<int> GetRunningServicesCountAsync()
        {
            var services = await _repository.GetAllAsync();
            int count = 0;
            foreach (var service in services)
            {
                if (service.Status == ServiceStatus.Running)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Update service priority
        /// </summary>
        public async Task<bool> UpdateServicePriorityAsync(string serviceId, int priority)
        {
            if (priority < 0 || priority > 100)
                throw new ServiceConfigurationException("Priority must be between 0 and 100");

            var service = await GetServiceAsync(serviceId);
            service.Priority = priority;
            service.UpdatedAt = DateTime.UtcNow;
            return await _repository.UpdateAsync(service);
        }
    }
}
