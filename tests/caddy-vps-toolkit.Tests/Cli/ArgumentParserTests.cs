#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using CaddyVpsToolkit.Cli;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Cli;

/// <summary>
/// Comprehensive tests for ArgumentParser class covering flags, values with =, repeated args,
/// unknown args, and missing required arguments.
/// </summary>
public sealed class ArgumentParserTests
{
    #region Flag Tests

    [Fact]
    public void HasFlag_SingleBooleanFlag_ReturnsTrue()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--verbose"]);

        // Act
        var hasFlag = parser.HasFlag("verbose");

        // Assert
        hasFlag.Should().BeTrue();
    }

    [Fact]
    public void HasFlag_MultipleBooleanFlags_ReturnsTrueForEach()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--verbose", "--force", "--dry-run"]);

        // Act & Assert
        parser.HasFlag("verbose").Should().BeTrue();
        parser.HasFlag("force").Should().BeTrue();
        parser.HasFlag("dry-run").Should().BeTrue();
    }

    [Fact]
    public void HasFlag_FlagNotPresent_ReturnsFalse()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--verbose"]);

        // Act
        var hasFlag = parser.HasFlag("force");

        // Assert
        hasFlag.Should().BeFalse();
    }

    [Fact]
    public void HasFlag_CaseInsensitiveMatching_ReturnsTrue()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--VERBOSE"]);

        // Act & Assert
        parser.HasFlag("verbose").Should().BeTrue();
        parser.HasFlag("VERBOSE").Should().BeTrue();
        parser.HasFlag("Verbose").Should().BeTrue();
    }

    [Fact]
    public void GetFlagValue_BooleanFlag_ReturnsEmptyString()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--verbose"]);

        // Act
        var value = parser.GetFlagValue("verbose");

        // Assert
        value.Should().BeEmpty();
    }

    #endregion

    #region Values with = Tests

    [Fact]
    public void GetFlagValue_EqualsFormat_SingleValue_ReturnsValue()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--name=my-service"]);

        // Act
        var value = parser.GetFlagValue("name");

        // Assert
        value.Should().Be("my-service");
    }

    [Fact]
    public void GetFlagValue_EqualsFormat_MultipleValues_ReturnsCorrectValues()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--name=my-service", "--port=8080", "--host=localhost"]);

        // Act & Assert
        parser.GetFlagValue("name").Should().Be("my-service");
        parser.GetFlagValue("port").Should().Be("8080");
        parser.GetFlagValue("host").Should().Be("localhost");
    }

    [Fact]
    public void GetFlagValue_EqualsFormat_WithSpecialCharacters_ReturnsValue()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--path=/var/www/app", "--url=https://example.com"]);

        // Act & Assert
        parser.GetFlagValue("path").Should().Be("/var/www/app");
        parser.GetFlagValue("url").Should().Be("https://example.com");
    }

    [Fact]
    public void GetFlagValue_EqualsFormat_FlagNotPresent_ReturnsNull()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--name=my-service"]);

        // Act
        var value = parser.GetFlagValue("port");

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void GetFlagValue_EqualsFormat_CaseInsensitiveMatching_ReturnsValue()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--NAME=my-service"]);

        // Act
        var value = parser.GetFlagValue("name");

        // Assert
        value.Should().Be("my-service");
    }

    #endregion

    #region Space-Separated Values Tests

    [Fact]
    public void GetFlagValue_SpaceFormat_SingleValue_ReturnsValue()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--name", "my-service"]);

        // Act
        var value = parser.GetFlagValue("name");

        // Assert
        value.Should().Be("my-service");
    }

    [Fact]
    public void GetFlagValue_SpaceFormat_MultipleValues_ReturnsCorrectValues()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--name", "my-service", "--port", "8080", "--host", "localhost"]);

        // Act & Assert
        parser.GetFlagValue("name").Should().Be("my-service");
        parser.GetFlagValue("port").Should().Be("8080");
        parser.GetFlagValue("host").Should().Be("localhost");
    }

    [Fact]
    public void GetFlagValue_SpaceFormat_ValueFollowedByFlag_ReturnsEmptyString()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--name", "--verbose"]);

        // Act
        var value = parser.GetFlagValue("name");

        // Assert
        value.Should().BeEmpty();
    }

    [Fact]
    public void GetFlagValue_SpaceFormat_ValueAtEndOfArgs_ReturnsEmptyString()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--name"]);

        // Act
        var value = parser.GetFlagValue("name");

        // Assert
        value.Should().BeEmpty();
    }

    #endregion

    #region Repeated Args Tests

    [Fact]
    public void GetFlagValue_RepeatedArgs_LastValueWins()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--name", "first", "--name", "second"]);

        // Act
        var value = parser.GetFlagValue("name");

        // Assert
        value.Should().Be("second");
    }

    [Fact]
    public void GetFlagValue_RepeatedEqualsFormat_LastValueWins()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--port=8080", "--port=9090"]);

        // Act
        var value = parser.GetFlagValue("port");

        // Assert
        value.Should().Be("9090");
    }

    [Fact]
    public void GetFlagValue_MixedFormats_LastValueWins()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--port", "8080", "--port=9090"]);

        // Act
        var value = parser.GetFlagValue("port");

        // Assert
        value.Should().Be("9090");
    }

    [Fact]
    public void HasFlag_RepeatedBooleanFlags_ReturnsTrue()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--verbose", "--force"]);

        // Act & Assert
        parser.HasFlag("verbose").Should().BeTrue();
        parser.HasFlag("force").Should().BeTrue();
    }

    #endregion

    #region Unknown Args Tests

    [Fact]
    public void HasFlag_UnknownFlag_ReturnsFalse()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--unknown-flag"]);

        // Act
        var hasFlag = parser.HasFlag("known-flag");

        // Assert
        hasFlag.Should().BeFalse();
    }

    [Fact]
    public void GetFlagValue_UnknownFlag_ReturnsNull()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--unknown=value"]);

        // Act
        var value = parser.GetFlagValue("known-flag");

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void GetAllFlags_ReturnsAllFlagNames()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--verbose", "--name=test", "--port", "8080", "--force"]);

        // Act
        var flags = parser.GetAllFlags();

        // Assert
        flags.Should().BeEquivalentTo(["verbose", "name", "port", "force"]);
    }

    [Fact]
    public void GetAllFlags_EmptyArgs_ReturnsEmptyList()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy"]);

        // Act
        var flags = parser.GetAllFlags();

        // Assert
        flags.Should().BeEmpty();
    }

    #endregion

    #region Missing Required Tests

    [Fact]
    public void GetFlagValue_MissingRequiredFlag_ReturnsNull()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy"]);

        // Act
        var value = parser.GetFlagValue("required-flag");

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void HasFlag_MissingFlag_ReturnsFalse()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy"]);

        // Act
        var hasFlag = parser.HasFlag("required-flag");

        // Assert
        hasFlag.Should().BeFalse();
    }

    [Fact]
    public void GetFlagValue_RequiredFlagWithNoValue_ReturnsEmptyString()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--required-flag"]);

        // Act
        var value = parser.GetFlagValue("required-flag");

        // Assert
        value.Should().BeEmpty();
    }

    #endregion

    #region Positional Arguments Tests

    [Fact]
    public void GetCommand_WithArgs_ReturnsCommand()
    {
        // Arrange
        var parser = new ArgumentParser(["DEPLOY", "--verbose"]);

        // Act
        var command = parser.GetCommand();

        // Assert
        command.Should().Be("deploy");
    }

    [Fact]
    public void GetCommand_NoArgs_ReturnsEmptyString()
    {
        // Arrange
        var parser = new ArgumentParser([]);

        // Act
        var command = parser.GetCommand();

        // Assert
        command.Should().BeEmpty();
    }

    [Fact]
    public void GetPositional_SinglePositional_ReturnsValue()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "myservice"]);

        // Act
        var positional = parser.GetPositional(0);

        // Assert
        positional.Should().Be("myservice");
    }

    [Fact]
    public void GetPositional_MultiplePositionals_ReturnsValues()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "myservice", "production", "v1.0"]);

        // Act & Assert
        parser.GetPositional(0).Should().Be("myservice");
        parser.GetPositional(1).Should().Be("production");
        parser.GetPositional(2).Should().Be("v1.0");
    }

    [Fact]
    public void GetPositional_OutOfRange_ReturnsNull()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "myservice"]);

        // Act & Assert
        parser.GetPositional(0).Should().Be("myservice");
        parser.GetPositional(1).Should().BeNull();
        parser.GetPositional(10).Should().BeNull();
    }

    [Fact]
    public void GetAllPositional_ReturnsAllNonFlagArguments()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "myservice", "--verbose", "production", "--force"]);

        // Act
        var positionals = parser.GetAllPositional();

        // Assert
        positionals.Should().BeEquivalentTo(["myservice", "production"]);
    }

    [Fact]
    public void GetAllPositional_OnlyFlags_ReturnsEmptyList()
    {
        // Arrange
        var parser = new ArgumentParser(["deploy", "--verbose", "--force"]);

        // Act
        var positionals = parser.GetAllPositional();

        // Assert
        positionals.Should().BeEmpty();
    }

    #endregion
}
