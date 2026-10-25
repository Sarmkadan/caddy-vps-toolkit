// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CaddyVpsToolkit.Core;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Read-only query extension methods for <see cref="ManagedService"/>.
    /// </summary>
    public static class ManagedServiceQueryExtensions
    {
        /// <summary>
        /// Filters a sequence of services down to those whose <see cref="ManagedService.Status"/>
        /// equals the given status.
        /// </summary>
        /// <param name="services">The services to filter.</param>
        /// <param name="status">The runtime status to match.</param>
        /// <returns>A lazily evaluated sequence of services with the requested status.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
        public static IEnumerable<ManagedService> WhereStatus(this IEnumerable<ManagedService> services, ServiceStatus status)
        {
            ArgumentNullException.ThrowIfNull(services);
            return services.Where(s => s.Status == status);
        }

        /// <summary>
        /// Returns every network port the service uses: the primary <see cref="ManagedService.Port"/>
        /// together with the <see cref="ServicePort.InternalPort"/> of each entry in
        /// <see cref="ManagedService.ExposedPorts"/>. Duplicates are removed and the result is sorted ascending.
        /// </summary>
        /// <param name="service">The service instance.</param>
        /// <returns>A sorted, distinct list of port numbers.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> is null.</exception>
        public static IReadOnlyList<int> GetAllPorts(this ManagedService service)
        {
            ArgumentNullException.ThrowIfNull(service);

            return service.ExposedPorts
                .Select(p => p.InternalPort)
                .Append(service.Port)
                .Distinct()
                .Order()
                .ToList();
        }

        /// <summary>
        /// Determines whether the service uses the given port, either as its primary
        /// <see cref="ManagedService.Port"/> or as an <see cref="ServicePort.InternalPort"/> in
        /// <see cref="ManagedService.ExposedPorts"/>.
        /// </summary>
        /// <param name="service">The service instance.</param>
        /// <param name="port">The port number to test.</param>
        /// <returns><c>true</c> if the service uses <paramref name="port"/>; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> is null.</exception>
        public static bool UsesPort(this ManagedService service, int port)
        {
            ArgumentNullException.ThrowIfNull(service);
            return service.Port == port || service.ExposedPorts.Any(p => p.InternalPort == port);
        }
    }
}
