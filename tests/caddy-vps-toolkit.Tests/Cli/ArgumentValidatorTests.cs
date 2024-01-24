// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;
using CaddyVpsToolkit.Cli;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Cli
{
    public class ArgumentValidatorTests
    {
        private readonly ArgumentValidator _sut;

        public ArgumentValidatorTests()
        {
            _sut = new ArgumentValidator();
        }

        [Fact]
        public void Validate_WithNullDescriptor_ShouldReturnInvalid()
        {
            // Arrange
            var parser = new ArgumentParser(new string[0]);

            // Act
            var result = _sut.Validate(parser, null!);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("Command not found");
        }

        [Fact]
        public void Validate_WithMissingRequiredPositionalArgs_ShouldReturnInvalid()
        {
            // Arrange
            var parser = new ArgumentParser(new[] { "cmd" });
            var descriptor = new CommandDescriptor("cmd", "description")
            { 
                Name = "cmd", 
                RequiredArguments = new List<string> { "serviceId", "status" } 
            };

            // Act
            var result = _sut.Validate(parser, descriptor);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Missing required argument"));
        }

        [Fact]
        public void Validate_WithUnknownFlag_ShouldReturnInvalid()
        {
            // Arrange
            var parser = new ArgumentParser(new[] { "cmd", "--unknown" });
            var descriptor = new CommandDescriptor("cmd", "description")
            { 
                Name = "cmd", 
                RequiredArguments = new List<string>(),
                OptionalFlags = new List<string> { "verbose" }
            };

            // Act
            var result = _sut.Validate(parser, descriptor);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("Unknown flag: --unknown");
        }

        [Fact]
        public void Validate_WithValidArguments_ShouldReturnValid()
        {
            // Arrange
            var parser = new ArgumentParser(new[] { "cmd", "arg1", "--verbose" });
            // Since ArgumentParser parses the first arg as command, "arg1" is positional 0.
            var descriptor = new CommandDescriptor("cmd", "description")
            { 
                Name = "cmd", 
                RequiredArguments = new List<string> { "param1" },
                OptionalFlags = new List<string> { "verbose" }
            };

            // Act
            var result = _sut.Validate(parser, descriptor);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidationResult_GetErrorMessage_ShouldJoinErrors()
        {
            // Arrange
            var result = new ValidationResult { Errors = new List<string> { "Error 1", "Error 2" } };

            // Act
            var message = result.GetErrorMessage();

            // Assert
            message.Should().Be($"Error 1{System.Environment.NewLine}Error 2");
        }
    }
}
