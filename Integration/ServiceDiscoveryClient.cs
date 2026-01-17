#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaddyVpsToolkit.Integration
{
    /// <summary>
    /// Service discovery abstraction for locating service endpoints.
    /// Supports registration, deregistration, and lookup of services.
    /// Can be implemented with Consul, Eureka, or other service registries.
    /// </summary>
    public interface IServiceDiscoveryClient
    {
        Task<ServiceInstance> DiscoverAsync(string serviceName);
        Task<List<ServiceInstance>> DiscoverAllAsync(string serviceName);
        Task RegisterAsync(ServiceInstance instance);
        Task DeregisterAsync(string serviceId);
    }

    /// <summary>
    /// In-memory implementation of service discovery for local development
    /// </summary>
    public sealed class InMemoryServiceDiscoveryClient : IServiceDiscoveryClient
    {
        private readonly Dictionary<string, List<ServiceInstance>> _registry = new();
        private readonly object _lockObject = new();

        public async Task<ServiceInstance> DiscoverAsync(string serviceName)
        {
            var instances = await DiscoverAllAsync(serviceName);
            return instances.Count > 0 ? instances[0] : null;
        }

        public async Task<List<ServiceInstance>> DiscoverAllAsync(string serviceName)
        {
            lock (_lockObject)
            {
                if (_registry.TryGetValue(serviceName, out var instances))
                    return new List<ServiceInstance>(instances);
                return new List<ServiceInstance>();
            }
        }

        public async Task RegisterAsync(ServiceInstance instance)
        {
            if (instance is null)
                throw new ArgumentNullException(nameof(instance));

            lock (_lockObject)
            {
                if (!_registry.ContainsKey(instance.ServiceName))
                    _registry[instance.ServiceName] = new List<ServiceInstance>();

                _registry[instance.ServiceName].Add(instance);
            }
        }

        public async Task DeregisterAsync(string serviceId)
        {
            if (string.IsNullOrEmpty(serviceId))
                throw new ArgumentException("Service ID required", nameof(serviceId));

            lock (_lockObject)
            {
                foreach (var services in _registry.Values)
                {
                    services.RemoveAll(s => s.Id == serviceId);
                }
            }
        }
    }

    /// <summary>
    /// Service instance for service discovery
    /// </summary>
    public sealed class ServiceInstance
    {
        public string Id { get; set; }
        public string ServiceName { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();

        public string GetUrl()
        {
            return $"http://{Host}:{Port}";
        }

        public override bool Equals(object obj)
        {
            return obj is ServiceInstance si && si.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }
    }
}
