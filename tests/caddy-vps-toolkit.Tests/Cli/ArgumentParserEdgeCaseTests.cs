#nullable enable
using CaddyVpsToolkit.Cli;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Cli;

/// <summary>
/// Edge-case tests for ArgumentParser - null arrays, empty args, flag parsing boundaries,
/// boolean flag detection, and mixed flag formats.
/// </summary>
public sealed class ArgumentParserEdgeCaseTests
{
    [Fact]
    public void Constructor_NullArgs_DoesNotThrow()
    {
        var act = () => new ArgumentParser(null!);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetCommand_EmptyArgs_ReturnsEmptyString()
    {
        var parser = new ArgumentParser([]);

        parser.GetCommand().Should().BeEmpty();
    }

    [Fact]
    public void GetCommand_SingleArg_ReturnsLowercasedCommand()
    {
        var parser = new ArgumentParser(["STATUS"]);

        parser.GetCommand().Should().Be("status");
    }

    [Fact]
    public void GetPositional_OutOfBounds_ReturnsNull()
    {
        var parser = new ArgumentParser(["deploy"]);

        parser.GetPositional(0).Should().BeNull();
        parser.GetPositional(5).Should().BeNull();
    }

    [Fact]
    public void GetPositional_ValidIndex_ReturnsArgument()
    {
        var parser = new ArgumentParser(["deploy", "myservice", "production"]);

        parser.GetPositional(0).Should().Be("myservice");
        parser.GetPositional(1).Should().Be("production");
    }

    [Fact]
    public void GetFlagValue_NullFlagName_ReturnsNull()
    {
        var parser = new ArgumentParser(["cmd", "--verbose"]);

        parser.GetFlagValue(null!).Should().BeNull();
    }

    [Fact]
    public void HasFlag_NullFlagName_ReturnsFalse()
    {
        var parser = new ArgumentParser(["cmd", "--verbose"]);

        parser.HasFlag(null!).Should().BeFalse();
    }

    [Fact]
    public void GetFlagValue_BooleanFlag_ReturnsEmptyStringWhenPresent()
    {
        var parser = new ArgumentParser(["cmd", "--verbose"]);

        parser.GetFlagValue("verbose").Should().BeEmpty();
    }

    [Fact]
    public void GetFlagValue_BooleanFlag_ReturnsNullWhenAbsent()
    {
        var parser = new ArgumentParser(["cmd"]);

        parser.GetFlagValue("verbose").Should().BeNull();
    }

    [Fact]
    public void GetFlagValue_EqualsFormat_ParsesCorrectly()
    {
        var parser = new ArgumentParser(["cmd", "--port=8080"]);

        parser.GetFlagValue("port").Should().Be("8080");
    }

    [Fact]
    public void GetFlagValue_SpaceFormat_ParsesCorrectly()
    {
        var parser = new ArgumentParser(["cmd", "--port", "8080"]);

        parser.GetFlagValue("port").Should().Be("8080");
    }

    [Fact]
    public void GetFlagValue_ValueFlag_LastArgWithoutValue_ReturnsEmptyString()
    {
        var parser = new ArgumentParser(["cmd", "--port"]);

        parser.GetFlagValue("port").Should().BeEmpty();
    }

    [Fact]
    public void HasFlag_PresentFlag_ReturnsTrue()
    {
        var parser = new ArgumentParser(["cmd", "--force"]);

        parser.HasFlag("force").Should().BeTrue();
    }

    [Fact]
    public void HasFlag_AbsentFlag_ReturnsFalse()
    {
        var parser = new ArgumentParser(["cmd"]);

        parser.HasFlag("force").Should().BeFalse();
    }

    [Fact]
    public void GetFlagValue_ValueFollowedByAnotherFlag_ReturnsEmptyString()
    {
        var parser = new ArgumentParser(["cmd", "--name", "--verbose"]);

        // --name is followed by --verbose (another flag), so no value
        parser.GetFlagValue("name").Should().BeEmpty();
    }

    [Fact]
    public void HasFlag_CaseInsensitiveBooleanFlags()
    {
        var parser = new ArgumentParser(["cmd", "--VERBOSE"]);

        parser.HasFlag("verbose").Should().BeTrue();
        parser.HasFlag("VERBOSE").Should().BeTrue();
    }
}
