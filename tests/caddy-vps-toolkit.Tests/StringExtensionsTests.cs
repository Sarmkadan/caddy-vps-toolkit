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
    /// <summary>
    /// Tests for the StringExtensions class.
    /// </summary>
    public sealed class StringExtensionsTests
    {
        [Fact]
        public void ToKebabCase_CamelCaseString_ReturnsLowercaseWithHyphens()
        {
            /// <summary>
            /// Verifies that the ToKebabCase method correctly converts a camel case string to kebab case.
            /// </summary>
            /// <param name="input">The input string to convert.</param>
            /// <param name="expected">The expected output string.</param>
            const string input = "helloWorldTest";
            const string expected = "hello-world-test";

            // Act
            var result = input.ToKebabCase();

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void Truncate_StringLongerThanMaxLength_TruncatesWithDefaultSuffix()
        {
            /// <summary>
            /// Verifies that the Truncate method correctly truncates a string to the specified length.
            /// </summary>
            /// <param name="input">The input string to truncate.</param>
            /// <param name="maxLength">The maximum length of the output string.</param>
            /// <param name="expected">The expected output string.</param>
            const string input = "Hello World!";
            const int maxLength = 8;
            const string expected = "Hello...";

            // Act
            var result = input.Truncate(maxLength);

            // Assert
            result.Should().Be(expected);
            result.Length.Should().Be(maxLength);
        }

        [Fact]
        public void IsValidEmail_ValidEmailFormat_ReturnsTrue()
        {
            /// <summary>
            /// Verifies that the IsValidEmail method correctly checks if a string is a valid email address.
            /// </summary>
            /// <param name="email">The email address to check.</param>
            /// <param name="expected">The expected result.</param>
            const string validEmail = "user@example.com";
            const string invalidEmail = "not-an-email";
            bool expectedValid = true;
            bool expectedInvalid = false;

            // Act & Assert
            validEmail.IsValidEmail().Should().Be(expectedValid);
            invalidEmail.IsValidEmail().Should().Be(expectedInvalid);
        }

        [Fact]
        public void EscapeShell_StringWithSingleQuote_ProducesShellSafeOutput()
        {
            /// <summary>
            /// Verifies that the EscapeShell method correctly escapes a string to make it safe for use in a shell.
            /// </summary>
            /// <param name="input">The input string to escape.</param>
            /// <param name="expected">The expected output string.</param>
            // POSIX single-quote escaping: close the quote, emit an escaped
            // quote, then reopen - 'hello'\''world'.
            const string input = "hello'world";
            const string expected = "'hello'\\''world'";

            // Act
            var result = input.EscapeShell();

            // Assert
            result.Should().Be(expected);
        }
    }
}
