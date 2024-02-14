#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using CaddyVpsToolkit.Utilities;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests
{
    public sealed class StringExtensionsTests
    {
        [Fact]
        public void ToKebabCase_CamelCaseString_ReturnsLowercaseWithHyphens()
        {
            // Arrange
            const string input = "helloWorldTest";

            // Act
            var result = input.ToKebabCase();

            // Assert
            result.Should().Be("hello-world-test");
        }

        [Fact]
        public void Truncate_StringLongerThanMaxLength_TruncatesWithDefaultSuffix()
        {
            // Arrange
            const string input = "Hello World!";

            // Act
            var result = input.Truncate(8);

            // Assert
            result.Should().Be("Hello...");
            result.Length.Should().Be(8);
        }

        [Fact]
        public void IsValidEmail_ValidEmailFormat_ReturnsTrue()
        {
            // Arrange
            const string validEmail = "user@example.com";
            const string invalidEmail = "not-an-email";

            // Act & Assert
            validEmail.IsValidEmail().Should().BeTrue();
            invalidEmail.IsValidEmail().Should().BeFalse();
        }

        [Fact]
        public void EscapeShell_StringWithSingleQuote_ProducesShellSafeOutput()
        {
            // Arrange
            const string input = "hello'world";

            // Act
            var result = input.EscapeShell();

            // Assert
            // Shell-safe form wraps in single quotes and escapes interior quotes as '\''
            result.Should().Be("'hello'\\''world'");
        }
    }
}
