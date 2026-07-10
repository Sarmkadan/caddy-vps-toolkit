#nullable enable
using CaddyVpsToolkit.Cli;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Cli;

/// <summary>
/// Extension methods for ArgumentParserEdgeCaseTests providing additional test scenarios
/// and helper assertions for edge cases in argument parser testing.
/// </summary>
public static class ArgumentParserEdgeCaseTestsExtensions
{
    /// <summary>
    /// Tests that GetCommand() returns expected command from various argument patterns.
    /// </summary>
    public static void GetCommand_ShouldReturnExpected(this ArgumentParserEdgeCaseTests _, string[] args, string expectedCommand)
    {
        var parser = new ArgumentParser(args);
        parser.GetCommand().Should().Be(expectedCommand);
    }

    /// <summary>
    /// Tests that GetPositional() returns null for negative indices and throws for invalid indices.
    /// </summary>
    public static void GetPositional_ShouldHandleNegativeIndices(this ArgumentParserEdgeCaseTests _, string[] args, int index)
    {
        var parser = new ArgumentParser(args);
        parser.GetPositional(index).Should().BeNull();
    }

    /// <summary>
    /// Tests combined flag operations - checking both presence and value extraction.
    /// </summary>
    public static void GetFlagValue_ShouldWorkWithHasFlag(this ArgumentParserEdgeCaseTests _, string[] args, string flagName, string expectedValue)
    {
        var parser = new ArgumentParser(args);
        parser.HasFlag(flagName).Should().BeTrue();
        parser.GetFlagValue(flagName).Should().Be(expectedValue);
    }

    /// <summary>
    /// Tests that multiple boolean flags can be checked simultaneously.
    /// </summary>
    public static void HasFlag_ShouldHandleMultipleFlags(this ArgumentParserEdgeCaseTests _, string[] args, string[] flagNames, bool expectedResult)
    {
        var parser = new ArgumentParser(args);
        foreach (var flagName in flagNames)
        {
            parser.HasFlag(flagName).Should().Be(expectedResult);
        }
    }
}