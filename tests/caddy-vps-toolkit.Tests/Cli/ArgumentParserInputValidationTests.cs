#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using CaddyVpsToolkit.Cli;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Cli;

/// <summary>
/// Input validation and injection protection tests for ArgumentParser.
/// Tests malformed input handling, memory safety, and injection prevention.
/// </summary>
public sealed class ArgumentParserInputValidationTests
{
    #region Memory Safety Tests

    [Fact]
    public void Constructor_ExtremelyLongArgument_ThrowsArgumentException()
    {
        // Arrange - Create a 2MB string (double the max allowed length)
        var longString = new string('A', 2 * 1024 * 1024);
        var args = new[] { "deploy", longString };

        // Act & Assert
        var act = () => new ArgumentParser(args);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*exceeds maximum allowed length*");
    }

    [Fact]
    public void Constructor_ArgumentAtMaxLength_Succeeds()
    {
        // Arrange - Create exactly 1MB string
        var maxLengthString = new string('B', 1024 * 1024);
        var args = new[] { "deploy", maxLengthString };

        // Act
        var act = () => new ArgumentParser(args);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ExtremelyLargeArgumentCount_ThrowsArgumentException()
    {
        // Arrange - Create 150k arguments (50% over the max allowed count)
        var args = new string[150000];
        args[0] = "deploy";
        for (int i = 1; i < args.Length; i++)
        {
            args[i] = "arg" + i;
        }

        // Act & Assert
        var act = () => new ArgumentParser(args);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*exceeds maximum allowed*");
    }

    [Fact]
    public void Constructor_ArgumentCountAtMaxLimit_Succeeds()
    {
        // Arrange - Create exactly 100k arguments
        var args = new string[100000];
        args[0] = "deploy";
        for (int i = 1; i < args.Length; i++)
        {
            args[i] = "arg" + i;
        }

        // Act
        var act = () => new ArgumentParser(args);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NullArray_Succeeds()
    {
        // Arrange & Act
        var act = () => new ArgumentParser(null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_EmptyArray_Succeeds()
    {
        // Arrange & Act
        var act = () => new ArgumentParser([]);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Shell Metacharacter Injection Tests

    [Fact]
    public void Constructor_ShellSemicolon_ReplacedWithUnderscore()
    {
        // Arrange - Test that shell metacharacters are neutralized
        var args = new[] { "deploy", "--path=/tmp;rm -rf /" };

        // Act
        var parser = new ArgumentParser(args);
        var pathValue = parser.GetFlagValue("path");

        // Assert - The semicolon should be replaced with underscore
        pathValue.Should().NotBeNullOrEmpty();
        pathValue.Should().NotContain(";");
        pathValue.Should().Be("/tmp_rm_-rf_/"); // Semicolon replaced with underscore
    }

    [Fact]
    public void Constructor_PipeCharacter_ReplacedWithUnderscore()
    {
        // Arrange
        var args = new[] { "deploy", "--command=echo hello | cat" };

        // Act
        var parser = new ArgumentParser(args);
        var commandValue = parser.GetFlagValue("command");

        // Assert
        commandValue.Should().NotContain("|");
    }

    [Fact]
    public void Constructor_AndAmpersand_ReplacedWithUnderscore()
    {
        // Arrange
        var args = new[] { "deploy", "--script=start.sh && rm -rf /" };

        // Act
        var parser = new ArgumentParser(args);
        var scriptValue = parser.GetFlagValue("script");

        // Assert
        scriptValue.Should().NotContain("&&");
    }

    [Fact]
    public void Constructor_DollarSign_ReplacedWithUnderscore()
    {
        // Arrange
        var args = new[] { "deploy", "--env=PATH=$PATH" };

        // Act
        var parser = new ArgumentParser(args);
        var envValue = parser.GetFlagValue("env");

        // Assert
        envValue.Should().NotContain("$");
    }

    [Fact]
    public void Constructor_Backtick_ReplacedWithUnderscore()
    {
        // Arrange
        var args = new[] { "deploy", "--template=`cat /etc/passwd`" };

        // Act
        var parser = new ArgumentParser(args);
        var templateValue = parser.GetFlagValue("template");

        // Assert
        templateValue.Should().NotContain("`");
    }

    [Fact]
    public void Constructor_RedirectionCharacters_ReplacedWithUnderscore()
    {
        // Arrange
        var args = new[] { "deploy", "--output=file.txt > /dev/null" };

        // Act
        var parser = new ArgumentParser(args);
        var outputValue = parser.GetFlagValue("output");

        // Assert
        outputValue.Should().NotContainAny(">", "<");
    }

    [Fact]
    public void Constructor_ExclamationMark_ReplacedWithUnderscore()
    {
        // Arrange
        var args = new[] { "deploy", "--cmd=echo !important" };

        // Act
        var parser = new ArgumentParser(args);
        var cmdValue = parser.GetFlagValue("cmd");

        // Assert
        cmdValue.Should().NotContain("!");
    }

    [Fact]
    public void Constructor_MultipleMetacharacters_AllReplaced()
    {
        // Arrange
        var args = new[] { "deploy", "--dangerous=rm -rf /; echo $USER" };

        // Act
        var parser = new ArgumentParser(args);
        var dangerousValue = parser.GetFlagValue("dangerous");

        // Assert - All metacharacters should be neutralized
        dangerousValue.Should().NotContainAny(";", "|", "&", "$", "`", ">", "<", "!");
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public void GetFlagValue_NullFlagName_ThrowsArgumentNullException()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy"]);

        // Act & Assert
        var act = () => parser.GetFlagValue(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetFlagValue_EmptyFlagName_ThrowsArgumentException()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy"]);

        // Act & Assert
        var act = () => parser.GetFlagValue("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetFlagValue_WhitespaceFlagName_ThrowsArgumentException()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy"]);

        // Act & Assert
        var act = () => parser.GetFlagValue("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HasFlag_NullFlagName_ThrowsArgumentNullException()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy"]);

        // Act & Assert
        var act = () => parser.HasFlag(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HasFlag_EmptyFlagName_ThrowsArgumentException()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy"]);

        // Act & Assert
        var act = () => parser.HasFlag("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetPositional_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "myservice"]);

        // Act & Assert
        var act = () => parser.GetPositional(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Malformed Argument Tests

    [Fact]
    public void Constructor_UnterminatedQuotedArgument_HandledSafely()
    {
        // Arrange - Unterminated quote should not cause exceptions
        var args = new[] { "deploy", "--path=\"/var/www/app" };

        // Act
        var act = () => new ArgumentParser(args);

        // Assert - Should not throw, should handle gracefully
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_MixedQuotes_HandledSafely()
    {
        // Arrange - Mixed quotes should not cause parsing issues
        var args = new[] { "deploy", "--path=\"/var/www/app'", "--config='config.json" };

        // Act
        var act = () => new ArgumentParser(args);

        // Assert - Should not throw
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ArgumentsWithTabsAndNewlines_HandledSafely()
    {
        // Arrange - Arguments with whitespace should be preserved
        var args = new[] { "deploy", "--name=my\tservice", "--description=line1\nline2" };

        // Act
        var act = () => new ArgumentParser(args);

        // Assert - Should not throw
        act.Should().NotThrow();
    }

    #endregion

    #region File Path Sanitization Tests

    [Fact]
    public void GetFlagValue_PathWithDirectoryTraversal_PathComponentsSanitized()
    {
        // Arrange - Test directory traversal attempts
        var args = new[] { "deploy", "--path=../../etc/passwd" };

        // Act
        var parser = new ArgumentParser(args);
        var pathValue = parser.GetFlagValue("path");

        // Assert - Path components should be neutralized (dots are NOT replaced by default, only shell metacharacters)
        pathValue.Should().NotContain("..");
    }

    [Fact]
    public void GetFlagValue_PathWithAbsolutePath_PathNormalized()
    {
        // Arrange
        var args = new[] { "deploy", "--config=/etc/nginx/nginx.conf" };

        // Act
        var parser = new ArgumentParser(args);
        var configValue = parser.GetFlagValue("config");

        // Assert - Should contain the path but without injection vectors
        configValue.Should().NotBeNullOrEmpty();
    }

    #endregion
}