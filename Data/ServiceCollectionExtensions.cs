using Microsoft.Extensions.DependencyInjection;
using CaddyVpsToolkit.Data;

namespace CaddyVpsToolkit.Data
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the configuration repository with the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to add to.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddConfigurationRepository(this IServiceCollection services)
        {
            services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();
            return services;
        }
    }
}
