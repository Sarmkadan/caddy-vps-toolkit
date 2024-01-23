// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Benchmarks;

/// <summary>
/// Benchmarks for Caddy configuration model operations — the core value-add of this tool.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CaddyConfigBenchmarks
{
    private static readonly CaddyConfig _config = new()
    {
        AdminEmail = "ops@example.com",
        AdminPort = 2019,
        AdminHost = "localhost",
        EnableMetrics = true,
        EnableLogOutput = true,
        HttpPort = 80,
        HttpsPort = 443,
        IdleTimeout = 120,
        ReadTimeout = 30,
        WriteTimeout = 30,
    };

    private static readonly CaddyRoute _simpleRoute = new()
    {
        Domain = "api.example.com",
        UpstreamUrl = "http://127.0.0.1:8080",
        EnableHttps = true,
        AutoRedirectHttp = true,
        PreserveHostHeader = true,
        TimeoutSeconds = 30,
    };

    private static readonly CaddyRoute _routeWithPath = new()
    {
        Domain = "app.example.com",
        Path = "/api/v1",
        UpstreamUrl = "http://127.0.0.1:3000",
        StripPath = true,
        EnableHttps = true,
        TimeoutSeconds = 60,
        CustomHeaders = new Dictionary<string, string>
        {
            ["X-Forwarded-Proto"] = "https",
            ["X-Real-IP"] = "{http.request.remote}",
        },
    };

    [Benchmark(Baseline = true)]
    public string GenerateRoutePath_Simple() => _simpleRoute.GenerateRoutePath();

    [Benchmark]
    public string GenerateRoutePath_WithPath() => _routeWithPath.GenerateRoutePath();

    [Benchmark]
    public string GenerateCaddyfileGlobals() => _config.GenerateCaddyfileGlobals();

    [Benchmark]
    public void ValidateConfig() => _config.Validate();

    [Benchmark]
    public void ValidateRoute() => _simpleRoute.Validate();

    [Benchmark]
    public string GetCaddyPathMatcher_Root() => _simpleRoute.GetCaddyPathMatcher();

    [Benchmark]
    public string GetCaddyPathMatcher_Prefixed() => _routeWithPath.GetCaddyPathMatcher();
}
