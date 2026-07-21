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
        /// Validates that a null descriptor throws ArgumentNullException.
        /// </summary>
        [Fact]
        public void Validate_WithNullDescriptor_ShouldThrowArgumentNullException()
        {
            // Arrange
            var parser = new ArgumentParser(new string[0]);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _sut.Validate(parser, null!));
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

        /// <summary>
        /// Validates that a null parser throws ArgumentNullException.
        /// </summary>
        [Fact]
        public void Validate_WithNullParser_ShouldThrowArgumentNullException()
        {
            // Arrange
            var descriptor = new CommandDescriptor("cmd", "description");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _sut.Validate(null!, descriptor));
        }

        /// <summary>
        /// Validates that multiple unknown flags result in multiple error messages.
        /// </summary>
        [Fact]
        public void Validate_WithMultipleUnknownFlags_ShouldReturnMultipleErrors()
        {
            // Arrange
            var parser = new ArgumentParser(new[] { "cmd", "--unknown1", "--unknown2" });
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
            result.Errors.Should().Contain("Unknown flag: --unknown1");
            result.Errors.Should().Contain("Unknown flag: --unknown2");
        }

        /// <summary>
        /// Validates that multiple missing required arguments result in multiple error messages.
        /// </summary>
        [Fact]
        public void Validate_WithMultipleMissingRequiredArgs_ShouldReturnMultipleErrors()
        {
            // Arrange
            var parser = new ArgumentParser(new[] { "cmd", "arg1" });
            var descriptor = new CommandDescriptor("cmd", "description")
            {
                Name = "cmd",
                RequiredArguments = new List<string> { "arg1", "arg2", "arg3" },
                OptionalFlags = new List<string>()
            };

            // Act
            var result = _sut.Validate(parser, descriptor);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Missing required argument: arg2"));
            result.Errors.Should().Contain(e => e.Contains("Missing required argument: arg3"));
        }

        /// <summary>
        /// Validates that both missing required arguments and unknown flags result in combined errors.
        /// </summary>
        [Fact]
        public void Validate_WithMissingArgsAndUnknownFlags_ShouldReturnCombinedErrors()
        {
            // Arrange
            var parser = new ArgumentParser(new[] { "cmd", "arg1", "--unknown" });
            var descriptor = new CommandDescriptor("cmd", "description")
            {
                Name = "cmd",
                RequiredArguments = new List<string> { "arg1", "arg2" },
                OptionalFlags = new List<string> { "verbose" }
            };

            // Act
            var result = _sut.Validate(parser, descriptor);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Missing required argument"));
            result.Errors.Should().Contain("Unknown flag: --unknown");
        }
    }
}
