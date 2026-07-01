#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CaddyVpsToolkit.Services;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Benchmarks;

/// <summary>
/// Benchmarks for ServiceManagementService operations.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public sealed class ServiceManagementServiceBenchmarks
{
    private static readonly ManagedService _sampleService = new()
    {
        Name = "benchmark-service",
        Port = 8080,
        Domain = "benchmark.example.com"
    };

    [Benchmark(Baseline = true)]
    public ManagedService CreateService() => new ManagedService
    {
        Name = "new-service",
        Port = 9090,
        Domain = "new.example.com"
    };
}
