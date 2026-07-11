#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
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
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>A configured <see cref="CaddyConfig"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
    public static CaddyConfig CreateProductionConfig(this CaddyConfigBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

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
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="domain">The domain name for the route. Must not be null or whitespace.</param>
    /// <param name="upstreamPort">The upstream port number. Must be between 1 and 65535.</param>
    /// <returns>A configured <see cref="CaddyRoute"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> or <paramref name="domain"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="domain"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="upstreamPort"/> is outside valid port range.</exception>
    public static CaddyRoute CreateApiRoute(this CaddyConfigBenchmarks benchmarks, string domain, int upstreamPort)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentException.ThrowIfNullOrWhiteSpace(domain, nameof(domain));

        if (upstreamPort is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(upstreamPort), upstreamPort, "Port must be between 1 and 65535");
        }

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
    /// Generates a complete Caddyfile from a configuration and routes.
    /// This is useful for measuring the end-to-end performance of configuration serialization.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="config">The configuration to serialize. Must not be null.</param>
    /// <param name="routes">The routes to include in the Caddyfile. Must not be null.</param>
    /// <returns>The generated Caddyfile content.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/>, <paramref name="config"/>, or <paramref name="routes"/> is <see langword="null"/>.</exception>
    public static string GenerateCaddyfile(this CaddyConfigBenchmarks benchmarks, CaddyConfig config, IEnumerable<CaddyRoute> routes)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(routes);

        var globals = config.GenerateCaddyfileGlobals();
        var routesContent = new List<string>();

        foreach (var route in routes)
        {
            routesContent.Add(route.GenerateRoutePath());
        }

        return $"""" + globals + "\n\n" + string.Join("\n\n", routesContent) + "\n" + $"""";
    }

    /// <summary>
    /// Validates multiple routes in sequence to benchmark validation overhead.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="routes">The routes to validate. Must not be null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> or <paramref name="routes"/> is <see langword="null"/>.</exception>
    public static void ValidateRoutes(this CaddyConfigBenchmarks benchmarks, IEnumerable<CaddyRoute> routes)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(routes);

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
    /// <param name="routes">The routes to generate matchers for. Must not be null.</param>
    /// <returns>A dictionary mapping domains to their path matchers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> or <paramref name="routes"/> is <see langword="null"/>.</exception>
    public static Dictionary<string, string> GetPathMatchers(this CaddyConfigBenchmarks benchmarks, IEnumerable<CaddyRoute> routes)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(routes);

        var matchers = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var route in routes)
        {
            var matcher = route.GetCaddyPathMatcher();
            matchers[route.Domain] = matcher;
        }

        return matchers;
    }
}
