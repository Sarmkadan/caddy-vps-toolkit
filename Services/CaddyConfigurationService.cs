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
using System.Text.Json;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Service for generating and managing Caddy reverse proxy configurations.
    /// </summary>
    public sealed class CaddyConfigurationService
    {
        private readonly ServiceManagementService _serviceManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CaddyConfigurationService"/> class.
        /// </summary>
        /// <param name="serviceManager">The service manager.</param>
        public CaddyConfigurationService(ServiceManagementService serviceManager)
        {
            _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
        }

        /// <summary>
        /// Generate Caddyfile content for all services with routes.
        /// </summary>
        /// <param name="globalConfig">The global Caddy configuration.</param>
        /// <param name="routes">The list of Caddy routes.</param>
        /// <returns>The generated Caddyfile content.</returns>
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
        /// <param name="content">The Caddyfile content.</param>
        /// <param name="filePath">The file path to write to.</param>
        /// <param name="dryRun">Whether to perform a dry run.</param>
        /// <returns>True if the write (or dry run) was successful.</returns>
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
        /// <param name="oldContent">The old content.</param>
        /// <param name="newContent">The new content.</param>
        /// <param name="filePath">The file path.</param>
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
        /// Read Caddyfile from disk.
        /// </summary>
        /// <param name="filePath">The file path to read from.</param>
        /// <returns>The Caddyfile content.</returns>
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
        /// <param name="route">The Caddy route.</param>
        /// <returns>The generated route block.</returns>
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
        /// Validate Caddyfile syntax in-process by checking brace balance line by line.
        /// This is a structural check only; it does not invoke the caddy binary and cannot
        /// detect directive-level errors.
        /// </summary>
        /// <param name="content">The Caddyfile content.</param>
        /// <returns>True if the syntax is valid.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is null or whitespace.</exception>
        /// <exception cref="CaddyOperationException">Thrown when the content contains unmatched braces.</exception>
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
    /// Validates the Caddy configuration for common problems.
    /// </summary>
    /// <param name="globalConfig">The global Caddy configuration.</param>
    /// <param name="routes">The list of Caddy routes to validate.</param>
    /// <returns>A list of validation findings (severity + message).</returns>
    public List<ConfigurationFinding> ValidateConfiguration(CaddyConfig globalConfig, List<CaddyRoute> routes)
    {
        var findings = new List<ConfigurationFinding>();

        if (globalConfig is null)
            throw new ArgumentNullException(nameof(globalConfig));

        if (routes is null)
            routes = new List<CaddyRoute>();

        // Check 1: Duplicate site addresses
        var siteAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var route in routes.Where(r => r.IsActive))
        {
            var siteAddress = route.GenerateRoutePath();
            if (siteAddresses.Contains(siteAddress))
            {
                findings.Add(new ConfigurationFinding
                {
                    Severity = "error",
                    Message = $"Duplicate site address detected: {siteAddress}"
                });
            }
            else
            {
                siteAddresses.Add(siteAddress);
            }
        }

        // Check 2: Empty upstreams
        foreach (var route in routes.Where(r => r.IsActive))
        {
            if (string.IsNullOrWhiteSpace(route.UpstreamUrl))
            {
                findings.Add(new ConfigurationFinding
                {
                    Severity = "error",
                    Message = $"Route '{route.GenerateRoutePath()}' has an empty upstream URL"
                });
            }
            else if (route.UpstreamUrl.Trim() == "")
            {
                findings.Add(new ConfigurationFinding
                {
                    Severity = "error",
                    Message = $"Route '{route.GenerateRoutePath()}' has a whitespace-only upstream URL"
                });
            }
        }

        // Check 3: Missing TLS settings for non-localhost sites
        foreach (var route in routes.Where(r => r.IsActive && r.EnableHttps))
        {
            if (route.EnableHttps && !IsLocalhost(route.Domain) && string.IsNullOrWhiteSpace(route.TlsDnsProvider))
            {
                findings.Add(new ConfigurationFinding
                {
                    Severity = "warning",
                    Message = $"Non-localhost domain '{route.Domain}' may not have proper TLS configuration. Consider setting TlsDnsProvider or verifying auto-HTTPS is enabled."
                });
            }
        }

        return findings;
    }

    /// <summary>
    /// Helper method to check if a domain is localhost or localhost equivalent.
    /// </summary>
    /// <param name="domain">The domain to check.</param>
    /// <returns>True if the domain is localhost.</returns>
    private static bool IsLocalhost(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return true;

        var normalized = domain.Trim().ToLowerInvariant();
        return normalized == "localhost" || normalized == "127.0.0.1" || normalized == "::1" || normalized.StartsWith("localhost.");
    }


        /// <summary>
        /// Generate configuration for a service as a Caddy route.
        /// </summary>
        /// <param name="service">The managed service.</param>
        /// <param name="domain">The domain name.</param>
        /// <param name="tlsDnsProvider">The optional TLS DNS provider.</param>
        /// <returns>The generated Caddy route.</returns>
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
        /// Generate Caddy JSON configuration (alternative format).
        /// Emits the admin endpoint, the server listeners derived from the global HTTP/HTTPS
        /// ports, and one reverse-proxy route per active entry in <paramref name="routes"/>.
        /// All values are JSON-escaped by the writer.
        /// </summary>
        /// <param name="globalConfig">The global Caddy configuration.</param>
        /// <param name="routes">The list of Caddy routes.</param>
        /// <returns>The generated JSON configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="globalConfig"/> is null.</exception>
        public string GenerateCaddyJsonAsync(CaddyConfig globalConfig, List<CaddyRoute> routes)
        {
            ArgumentNullException.ThrowIfNull(globalConfig);
            routes ??= new List<CaddyRoute>();

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();

                writer.WriteStartObject("admin");
                writer.WriteString("listen", FormattableString.Invariant($"{globalConfig.AdminHost}:{globalConfig.AdminPort}"));
                writer.WriteEndObject();

                writer.WriteStartObject("apps");
                writer.WriteStartObject("http");
                writer.WriteStartObject("servers");
                writer.WriteStartObject("default");

                writer.WriteStartArray("listen");
                writer.WriteStringValue(FormattableString.Invariant($":{globalConfig.HttpPort}"));
                writer.WriteStringValue(FormattableString.Invariant($":{globalConfig.HttpsPort}"));
                writer.WriteEndArray();

                writer.WriteStartArray("routes");
                foreach (var route in routes.Where(r => r.IsActive))
                {
                    writer.WriteStartObject();

                    writer.WriteStartArray("match");
                    writer.WriteStartObject();
                    writer.WriteStartArray("host");
                    writer.WriteStringValue(route.Domain);
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    writer.WriteEndArray();

                    writer.WriteStartArray("handle");
                    writer.WriteStartObject();
                    writer.WriteString("handler", "reverse_proxy");
                    writer.WriteStartArray("upstreams");
                    writer.WriteStartObject();
                    writer.WriteString("dial", GetDialAddress(route.UpstreamUrl));
                    writer.WriteEndObject();
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    writer.WriteEndArray();

                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WriteEndObject(); // default
                writer.WriteEndObject(); // servers
                writer.WriteEndObject(); // http
                writer.WriteEndObject(); // apps

                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Convert an upstream URL to the host:port dial address expected by Caddy's JSON config.
        /// Falls back to the raw value when it is not an absolute URL.
        /// </summary>
        /// <param name="upstreamUrl">The upstream URL.</param>
        /// <returns>The dial address.</returns>
        private static string GetDialAddress(string upstreamUrl)
        {
            if (Uri.TryCreate(upstreamUrl, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host))
                return FormattableString.Invariant($"{uri.Host}:{uri.Port}");

            return upstreamUrl;
        }
    }
}
