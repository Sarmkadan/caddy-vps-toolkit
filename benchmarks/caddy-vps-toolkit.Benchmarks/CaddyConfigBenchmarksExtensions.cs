#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Running;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Benchmarks;

/// <summary>
/// Extension methods for <see cref="CaddyConfigBenchmarks"/> that provide additional utility and benchmarking scenarios.
/// </summary>
public static class CaddyConfigBenchmarksExtensions
{
    /// <summary>
    /// Creates a new instance of <see cref="CaddyConfig"/> with common production settings pre-configured.
    /// Useful for benchmarking realistic configurations.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance (unused, for extension method syntax).</param>
    /// <returns>A configured <see cref="CaddyConfig"/> instance.</returns>
    public static CaddyConfig CreateProductionConfig(this CaddyConfigBenchmarks benchmarks)
    {
        return new CaddyConfig
        {
            AdminEmail = "admin@example.com",
            AdminPort = 2019,
            AdminHost = "127.0.0.1",
            EnableMetrics = true,
            EnableLogOutput = true,
            HttpPort = 80,
            HttpsPort = 443,
            IdleTimeout = 300,
            ReadTimeout = 60,
            WriteTimeout = 60,
            AutoHttpsDisabled = false,
            TlsPolicy = "clients"
        };
    }

    /// <summary>
    /// Creates a new route with common API configuration for benchmarking.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance (unused, for extension method syntax).</param>
    /// <param name="domain">The domain name for the route.</param>
    /// <param name="upstreamPort">The upstream port number.</param>
    /// <returns>A configured <see cref="CaddyRoute"/> instance.</returns>
    public static CaddyRoute CreateApiRoute(this CaddyConfigBenchmarks benchmarks, string domain, int upstreamPort)
    {
        return new CaddyRoute
        {
            Domain = domain,
            Path = "/api",
            UpstreamUrl = $"http://127.0.0.1:{upstreamPort}",
            StripPath = true,
            EnableHttps = true,
            AutoRedirectHttp = true,
            PreserveHostHeader = true,
            TimeoutSeconds = 30,
            CustomHeaders = new Dictionary<string, string>
            {
                ["X-Forwarded-Proto"] = "https",
                ["X-Real-IP"] = "{http.request.remote}",
                ["Cache-Control"] = "no-cache"
            }
        };
    }

    /// <summary>
    /// Benchmarks the generation of a complete Caddyfile from a configuration.
    /// This is useful for measuring the end-to-end performance of configuration serialization.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="config">The configuration to serialize.</param>
    /// <returns>The generated Caddyfile content.</returns>
    public static string GenerateCaddyfile(this CaddyConfigBenchmarks benchmarks, CaddyConfig config)
    {
        var globals = config.GenerateCaddyfileGlobals();
        var routes = new List<string>();

        // Simulate multiple routes for realistic benchmarking
        for (int i = 0; i < 5; i++)
        {
            var route = new CaddyRoute
            {
                Domain = $"service-{i}.example.com",
                UpstreamUrl = $"http://127.0.0.1:{8080 + i}",
                EnableHttps = true,
                TimeoutSeconds = 30
            };
            routes.Add(route.GenerateRoutePath());
        }

        return $"""" + globals + "\n\n" + string.Join("\n\n", routes) + "\n" + $"""";
    }

    /// <summary>
    /// Validates multiple routes in sequence to benchmark validation overhead.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="routes">The routes to validate.</param>
    public static void ValidateRoutes(this CaddyConfigBenchmarks benchmarks, IEnumerable<CaddyRoute> routes)
    {
        foreach (var route in routes)
        {
            route.Validate();
        }
    }

    /// <summary>
    /// Generates path matchers for all routes in a configuration.
    /// Useful for benchmarking path-based routing scenarios.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="routes">The routes to generate matchers for.</param>
    /// <returns>A dictionary mapping domains to their path matchers.</returns>
    public static Dictionary<string, string> GetPathMatchers(this CaddyConfigBenchmarks benchmarks, IEnumerable<CaddyRoute> routes)
    {
        var matchers = new Dictionary<string, string>();

        foreach (var route in routes)
        {
            var matcher = route.GetCaddyPathMatcher();
            matchers[route.Domain] = matcher;
        }

        return matchers;
    }
}
