#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;

namespace CaddyVpsToolkit.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering formatter services in the DI container.
    /// </summary>
    public static class FormatterServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all built‑in <see cref="CaddyVpsToolkit.Formatters.IOutputFormatter"/> implementations.
        /// The default formatter (when <see cref="IOutputFormatter"/> is requested directly) is <see cref="CaddyVpsToolkit.Formatters.TableFormatter"/>.
        /// </summary>
        public static IServiceCollection AddFormatters(this IServiceCollection services)
        {
            // Register concrete formatters
            services.AddTransient<CaddyVpsToolkit.Formatters.TableFormatter>();
            services.AddTransient<CaddyVpsToolkit.Formatters.CsvFormatter>();
            services.AddTransient<CaddyVpsToolkit.Formatters.JsonFormatter>();
            services.AddTransient<CaddyVpsToolkit.Formatters.TextFormatter>();

            // Register the default IOutputFormatter (TableFormatter)
            services.AddTransient<CaddyVpsToolkit.Formatters.IOutputFormatter, CaddyVpsToolkit.Formatters.TableFormatter>();

            return services;
        }
    }
}
