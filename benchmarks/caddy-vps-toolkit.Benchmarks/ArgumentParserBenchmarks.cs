// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CaddyVpsToolkit.Cli;

namespace CaddyVpsToolkit.Benchmarks;

/// <summary>
/// Benchmarks for CLI argument parsing — the hot path on every invocation.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ArgumentParserBenchmarks
{
    private static readonly string[] SmallArgs =
        ["add-service", "--name", "my-api", "--port", "8080", "--verbose"];

    private static readonly string[] LargeArgs =
        ["deploy", "--name=web-app", "--domain=example.com", "--port=443",
         "--type=web", "--ssl", "--upstream=backend:8080",
         "--health-path=/health", "--timeout=30", "--force"];

    [Benchmark(Baseline = true)]
    public string GetCommand_Small() => new ArgumentParser(SmallArgs).GetCommand();

    [Benchmark]
    public string? GetFlagValue_EqualsSyntax() => new ArgumentParser(LargeArgs).GetFlagValue("name");

    [Benchmark]
    public string? GetFlagValue_SpaceSyntax() => new ArgumentParser(SmallArgs).GetFlagValue("port");

    [Benchmark]
    public bool HasFlag_Present() => new ArgumentParser(LargeArgs).HasFlag("ssl");

    [Benchmark]
    public bool HasFlag_Absent() => new ArgumentParser(LargeArgs).HasFlag("nonexistent");

    [Benchmark]
    public List<string> GetAllFlags_Large() => new ArgumentParser(LargeArgs).GetAllFlags();
}
