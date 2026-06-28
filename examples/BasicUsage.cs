using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CaddyVpsToolkit.Services;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Examples
{
    /// <summary>
    /// Demonstrates the most basic usage of the Caddy VPS Toolkit:
    /// Initializing the service management service and listing all services.
    /// </summary>
    public class BasicUsage
    {
        public static async Task RunExample(IServiceProvider serviceProvider)
        {
            // 1. Resolve the ServiceManagementService from the DI container
            var serviceManager = serviceProvider.GetRequiredService<ServiceManagementService>();

            // 2. Fetch all services
            Console.WriteLine("Fetching all managed services...");
            var services = await serviceManager.GetAllServicesAsync();

            foreach (var service in services)
            {
                Console.WriteLine($"Service: {service.Name} | Status: {service.Status}");
            }
        }
    }
}
