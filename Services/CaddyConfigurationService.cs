#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Service for generating and managing Caddy reverse proxy configurations
    /// </summary>
    public sealed class CaddyConfigurationService
    {
        private readonly ServiceManagementService _serviceManager;

        public CaddyConfigurationService(ServiceManagementService serviceManager)
        {
            _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
        }

        /// <summary>
        /// Generate Caddyfile content for all services with routes
        /// </summary>
        public async Task<string> GenerateCaddyfileAsync(CaddyConfig globalConfig, List<CaddyRoute> routes)
        {
            if (globalConfig is null)
                throw new ArgumentNullException(nameof(globalConfig));

            if (routes is null)
                routes = new List<CaddyRoute>();

            globalConfig.Validate();
            globalConfig.SetDefaultValues();

            var sb = new StringBuilder();

            // Global configuration
            sb.AppendLine(globalConfig.GenerateCaddyfileGlobals());
            sb.AppendLine();

            // Generate route blocks
            var activeRoutes = routes.Where(r => r.IsActive).ToList();
            foreach (var route in activeRoutes)
            {
                route.Validate();
                sb.AppendLine(GenerateRouteBlock(route));
            }

            if (activeRoutes.Count == 0)
            {
                sb.AppendLine("# No active routes configured");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Write Caddyfile to disk.
        /// When <paramref name="dryRun"/> is <c>true</c>, the content that would be written is
        /// printed to stdout and no file is modified — safe to use on production servers before
        /// committing a configuration change.
        /// </summary>
        public async Task<bool> WriteCaddyfileAsync(string content, string filePath = null, bool dryRun = false)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content cannot be empty", nameof(content));

            filePath = filePath ?? Path.Combine(AppConstants.CaddyConfigDirectory, AppConstants.CaddyfileName);

            if (dryRun)
            {
                Console.WriteLine($"[dry-run] Would write Caddyfile to: {filePath}");
                Console.WriteLine("[dry-run] --- begin generated content ---");

                var existingContent = File.Exists(filePath)
                    ? await File.ReadAllTextAsync(filePath)
                    : null;

                if (existingContent is not null)
                    PrintDiff(existingContent, content, filePath);
                else
                    Console.WriteLine(content);

                Console.WriteLine("[dry-run] --- end generated content ---");
                return true;
            }

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(filePath, content);
                return true;
            }
            catch (Exception ex)
            {
                throw new CaddyOperationException($"Failed to write Caddyfile: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Print a simple line-level diff between <paramref name="oldContent"/> and <paramref name="newContent"/>.
        /// </summary>
        private static void PrintDiff(string oldContent, string newContent, string filePath)
        {
            var oldLines = oldContent.Split('\n');
            var newLines = newContent.Split('\n');
            var maxLen = Math.Max(oldLines.Length, newLines.Length);

            Console.WriteLine($"[dry-run] diff {filePath}");
            for (int i = 0; i < maxLen; i++)
            {
                var oldLine = i < oldLines.Length ? oldLines[i] : null;
                var newLine = i < newLines.Length ? newLines[i] : null;

                if (oldLine == newLine) continue;

                if (oldLine is not null)
                    Console.WriteLine($"-{oldLine}");
                if (newLine is not null)
                    Console.WriteLine($"+{newLine}");
            }
        }

        /// <summary>
        /// Read Caddyfile from disk
        /// </summary>
        public async Task<string> ReadCaddyfileAsync(string filePath = null)
        {
            filePath = filePath ?? Path.Combine(AppConstants.CaddyConfigDirectory, AppConstants.CaddyfileName);

            try
            {
                if (!File.Exists(filePath))
                    throw new CaddyOperationException($"Caddyfile not found at {filePath}");

                return await File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                throw new CaddyOperationException($"Failed to read Caddyfile: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate route block for a single route.
        /// When a non-root path is configured, uses a named matcher so the site block header stays
        /// as the bare domain. Named matcher identifiers must not contain hyphens; service names are
        /// sanitized via <see cref="CaddyRoute.GetCaddyMatcherName"/> (hyphens replaced with underscores).
        /// </summary>
        public string GenerateRouteBlock(CaddyRoute route)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            route.Validate();

            var sb = new StringBuilder();
            var hasPath = !string.IsNullOrWhiteSpace(route.Path) && route.Path != "/";

            if (hasPath)
            {
                // Named matcher identifiers in Caddyfile syntax must not contain hyphens;
                // GetCaddyMatcherName() returns an underscore-safe identifier.
                var matcherName = route.GetCaddyMatcherName();
                sb.AppendLine($"{route.Domain} {{");
                sb.AppendLine($"    @{matcherName} path {route.Path}*");
                sb.AppendLine($"    handle @{matcherName} {{");
                sb.AppendLine($"        reverse_proxy {route.UpstreamUrl}");

                if (route.StripPath)
                    sb.AppendLine($"        uri strip_prefix {route.Path}");

                if (!route.PreserveHostHeader)
                    sb.AppendLine("        reverse_proxy_header -Host");

                if (!string.IsNullOrWhiteSpace(route.RateLimitRule))
                    sb.AppendLine($"        rate_limit {route.RateLimitRule}");

                foreach (var header in route.CustomHeaders)
                    sb.AppendLine($"        header +{header.Key} {header.Value}");

                if (route.BasicAuthEnabled && !string.IsNullOrWhiteSpace(route.BasicAuthUsername))
                {
                    sb.AppendLine($"        basicauth {{");
                    sb.AppendLine($"            {route.BasicAuthUsername} {route.BasicAuthPasswordHash}");
                    sb.AppendLine("        }");
                }

                sb.AppendLine($"        timeouts {{");
                sb.AppendLine($"            read {route.TimeoutSeconds}s");
                sb.AppendLine($"            write {route.TimeoutSeconds}s");
                sb.AppendLine($"        }}");

                if (!string.IsNullOrWhiteSpace(route.TlsDnsProvider))
                {
                    sb.AppendLine($"    tls {{");
                    sb.AppendLine($"        dns {route.TlsDnsProvider}");
                    sb.AppendLine($"    }}");
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");
            }
            else
            {
                sb.AppendLine($"{route.Domain} {{");
                sb.AppendLine($"    reverse_proxy {route.UpstreamUrl}");

                if (!route.PreserveHostHeader)
                    sb.AppendLine("    reverse_proxy_header -Host");

                if (!string.IsNullOrWhiteSpace(route.RateLimitRule))
                    sb.AppendLine($"    rate_limit {route.RateLimitRule}");

                foreach (var header in route.CustomHeaders)
                    sb.AppendLine($"    header +{header.Key} {header.Value}");

                if (route.BasicAuthEnabled && !string.IsNullOrWhiteSpace(route.BasicAuthUsername))
                {
                    sb.AppendLine($"    basicauth {{");
                    sb.AppendLine($"        {route.BasicAuthUsername} {route.BasicAuthPasswordHash}");
                    sb.AppendLine("    }");
                }

                sb.AppendLine($"    timeouts {{");
                sb.AppendLine($"        read {route.TimeoutSeconds}s");
                sb.AppendLine($"        write {route.TimeoutSeconds}s");
                sb.AppendLine($"    }}");

                if (!string.IsNullOrWhiteSpace(route.TlsDnsProvider))
                {
                    sb.AppendLine($"    tls {{");
                    sb.AppendLine($"        dns {route.TlsDnsProvider}");
                    sb.AppendLine($"    }}");
                }

                sb.AppendLine("}");
            }

            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Validate Caddyfile syntax (mock - requires actual Caddy)
        /// </summary>
        public async Task<bool> ValidateCaddyfileAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content cannot be empty", nameof(content));

            try
            {
                // Basic syntax validation
                var lines = content.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                int braceCount = 0;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("#"))
                        continue;

                    braceCount += trimmed.Count(c => c == '{');
                    braceCount -= trimmed.Count(c => c == '}');

                    if (braceCount < 0)
                        throw new CaddyOperationException("Invalid Caddyfile: unmatched closing brace");
                }

                if (braceCount != 0)
                    throw new CaddyOperationException("Invalid Caddyfile: unmatched opening braces");

                return true;
            }
            catch (CaddyOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CaddyOperationException($"Caddyfile validation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate configuration for a service as a Caddy route
        /// </summary>
        public CaddyRoute GenerateRouteForService(ManagedService service, string domain, string tlsDnsProvider = null)
        {
            if (service is null)
                throw new ArgumentNullException(nameof(service));

            if (string.IsNullOrWhiteSpace(domain))
                throw new ArgumentException("Domain is required", nameof(domain));

            return new CaddyRoute
            {
                ServiceId = service.Id,
                Domain = domain,
                UpstreamUrl = $"http://{service.HostBinding}:{service.Port}",
                EnableHttps = true,
                AutoRedirectHttp = true,
                PreserveHostHeader = true,
                TimeoutSeconds = 30,
                TlsDnsProvider = tlsDnsProvider
            };
        }

        /// <summary>
        /// Generate Caddy JSON configuration (alternative format)
        /// </summary>
        public string GenerateCaddyJsonAsync(CaddyConfig globalConfig, List<CaddyRoute> routes)
        {
            // Simplified JSON generation
            var json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine("  \"apps\": {");
            json.AppendLine("    \"http\": {");
            json.AppendLine("      \"servers\": {");
            json.AppendLine("        \"default\": {");
            json.AppendLine("          \"routes\": [");

            var activeRoutes = routes.Where(r => r.IsActive).ToList();
            for (int i = 0; i < activeRoutes.Count; i++)
            {
                var route = activeRoutes[i];
                json.Append($"            {{ \"match\": [{{ \"host\": [\"{route.Domain}\"] }}], \"handle\": [{{ \"handler\": \"reverse_proxy\", \"upstreams\": [{{ \"dial\": \"{route.UpstreamUrl}\" }}] }}] }}");
                if (i < activeRoutes.Count - 1)
                    json.Append(",");
                json.AppendLine();
            }

            json.AppendLine("          ]");
            json.AppendLine("        }");
            json.AppendLine("      }");
            json.AppendLine("    }");
            json.AppendLine("  }");
            json.AppendLine("}");

            return json.ToString();
        }
    }
}
