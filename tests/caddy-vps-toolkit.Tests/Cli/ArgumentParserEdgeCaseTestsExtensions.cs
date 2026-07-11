#nullable enable
using System;
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
    /// <param name="test">The test instance.</param>
    /// <param name="args">The command-line arguments to parse.</param>
    /// <param name="expectedCommand">The expected command name.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="args"/> is null.</exception>
    public static void GetCommand_ShouldReturnExpected(this ArgumentParserEdgeCaseTests test, string[] args, string expectedCommand)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(expectedCommand);

        var parser = new ArgumentParser(args);
        parser.GetCommand().Should().Be(expectedCommand);
    }

    /// <summary>
    /// Tests that GetPositional() returns null for negative indices and handles edge cases correctly.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="args">The command-line arguments to parse.</param>
    /// <param name="index">The positional index to retrieve.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="args"/> is null.</exception>
    public static void GetPositional_ShouldHandleNegativeIndices(this ArgumentParserEdgeCaseTests test, string[] args, int index)
    {
        ArgumentNullException.ThrowIfNull(args);

        var parser = new ArgumentParser(args);
        parser.GetPositional(index).Should().BeNull();
    }

    /// <summary>
    /// Tests combined flag operations - checking both presence and value extraction.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="args">The command-line arguments to parse.</param>
    /// <param name="flagName">The flag name to check.</param>
    /// <param name="expectedValue">The expected flag value.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="args"/> or <paramref name="flagName"/> is null.</exception>
    public static void GetFlagValue_ShouldWorkWithHasFlag(this ArgumentParserEdgeCaseTests test, string[] args, string flagName, string expectedValue)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(flagName);

        var parser = new ArgumentParser(args);
        parser.HasFlag(flagName).Should().BeTrue();
        parser.GetFlagValue(flagName).Should().Be(expectedValue);
    }

    /// <summary>
    /// Tests that multiple boolean flags can be checked simultaneously.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="args">The command-line arguments to parse.</param>
    /// <param name="flagNames">The flag names to check.</param>
    /// <param name="expectedResult">The expected result for all flags.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="args"/> or <paramref name="flagNames"/> is null.</exception>
    public static void HasFlag_ShouldHandleMultipleFlags(this ArgumentParserEdgeCaseTests test, string[] args, string[] flagNames, bool expectedResult)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(flagNames);

        var parser = new ArgumentParser(args);
        foreach (var flagName in flagNames)
        {
            parser.HasFlag(flagName).Should().Be(expectedResult);
        }
    }
}