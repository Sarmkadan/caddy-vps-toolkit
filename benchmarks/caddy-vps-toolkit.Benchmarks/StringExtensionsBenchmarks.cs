#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Benchmarks;

/// <summary>
/// Benchmarks for string utilities used throughout config generation and CLI output.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public sealed class StringExtensionsBenchmarks
{
    private const string CamelCaseInput = "reverseProxyUpstreamService";
    private const string KebabCaseInput = "reverse-proxy-upstream-service";
    private const string LongString = "This is a long service description that will be truncated for display purposes in CLI output";
    private const string NumericString = "1234567890";
    private static readonly string[] UrlPrefixes = ["http://", "https://", "ftp://", "ws://"];

    [Benchmark(Baseline = true)]
    public string ToKebabCase() => CamelCaseInput.ToKebabCase();

    [Benchmark]
    public string ToCamelCase() => KebabCaseInput.ToCamelCase();

    [Benchmark]
    public string Truncate() => LongString.Truncate(40);

    [Benchmark]
    public bool IsNumeric_Digits() => NumericString.IsNumeric();

    [Benchmark]
    public bool IsNumeric_NonDigits() => CamelCaseInput.IsNumeric();

    [Benchmark]
    public bool StartsWithAny_Match() => "https://example.com".StartsWithAny(UrlPrefixes);

    [Benchmark]
    public bool StartsWithAny_NoMatch() => "example.com".StartsWithAny(UrlPrefixes);
}
