#nullable enable
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
    /// <summary>
    /// Tests for the <see cref="ArgumentValidator"/> class.
    /// </summary>
    public sealed class ArgumentValidatorTests
    {
        private readonly ArgumentValidator _sut;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentValidatorTests"/> class.
        /// </summary>
        public ArgumentValidatorTests()
        {
            _sut = new ArgumentValidator();
        }

        /// <summary>
        /// Validates that a null descriptor results in an invalid result with the error "Command not found".
        /// </summary>
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

        /// <summary>
        /// Validates that missing required positional arguments cause an invalid result.
        /// </summary>
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

        /// <summary>
        /// Validates that an unknown flag results in an invalid result.
        /// </summary>
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

        /// <summary>
        /// Validates that a command with all required arguments and optional flags passes validation.
        /// </summary>
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

        /// <summary>
        /// Validates that ValidationResult.GetErrorMessage joins errors with a newline.
        /// </summary>
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
