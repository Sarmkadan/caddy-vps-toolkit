using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CaddyVpsToolkit.Services;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Core;

namespace CaddyVpsToolkit.Examples
{
    /// <summary>
    /// Demonstrates advanced usage: creating a new service, updating its status,
    /// and handling potential exceptions.
    /// </summary>
    public class AdvancedUsage
    {
        public static async Task RunExample(IServiceProvider serviceProvider)
        {
            var serviceManager = serviceProvider.GetRequiredService<ServiceManagementService>();

            try
            {
                // 1. Create a new service model
                var newService = new ManagedService
                {
                    Name = "my-new-web-app",
                    Description = "A demonstration web application",
                    IsEnabled = true,
                    Priority = 50
                };

                // 2. Add the service
                Console.WriteLine($"Creating service: {newService.Name}...");
                string serviceId = await serviceManager.CreateServiceAsync(newService);
                Console.WriteLine($"Service created with ID: {serviceId}");

                // 3. Update the service status
                Console.WriteLine("Updating service status to Running...");
                await serviceManager.UpdateServiceStatusAsync(serviceId, ServiceStatus.Running);

                // 4. Verify
                var updatedService = await serviceManager.GetServiceAsync(serviceId);
                Console.WriteLine($"Service '{updatedService.Name}' is now {updatedService.Status}");
            }
            catch (ServiceConfigurationException ex)
            {
                Console.WriteLine($"Configuration error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}
