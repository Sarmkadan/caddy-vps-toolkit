#nullable enable
using System;
using CaddyVpsToolkit.Utilities;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests
{
    /// <summary>
    /// Unit tests for <see cref="StringExtensions"/>.
    /// Covers all public API methods including happy-path, edge-cases and error-paths.
    /// </summary>
    public sealed class StringExtensionsUnitTests
    {
        [Fact]
        public void IsNullOrWhiteSpace_NullInput_ReturnsTrue()
        {
            // Arrange
            string? input = null;

            // Act
            bool result = input.IsNullOrWhiteSpace();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsNullOrWhiteSpace_EmptyString_ReturnsTrue()
        {
            // Arrange
            string input = "";

            // Act
            bool result = input.IsNullOrWhiteSpace();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsNullOrWhiteSpace_WhitespaceOnly_ReturnsTrue()
        {
            // Arrange
            string input = "   \t\n  ";

            // Act
            bool result = input.IsNullOrWhiteSpace();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsNullOrWhiteSpace_NonWhitespaceString_ReturnsFalse()
        {
            // Arrange
            string input = "hello world";

            // Act
            bool result = input.IsNullOrWhiteSpace();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ToTitleCase_NullInput_ReturnsNull()
        {
            // Arrange
            string? input = null;

            // Act
            string? result = input.ToTitleCase();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ToTitleCase_EmptyString_ReturnsEmpty()
        {
            // Arrange
            string input = "";

            // Act
            string result = input.ToTitleCase();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ToTitleCase_SingleCharacter_ReturnsUppercase()
        {
            // Arrange
            string input = "a";

            // Act
            string result = input.ToTitleCase();

            // Assert
            result.Should().Be("A");
        }

        [Fact]
        public void ToTitleCase_MultipleWords_ReturnsTitleCased()
        {
            // Arrange
            string input = "hello world test";

            // Act
            string result = input.ToTitleCase();

            // Assert
            result.Should().Be("Hello world test");
        }

        [Fact]
        public void ToKebabCase_NullInput_ReturnsNull()
        {
            // Arrange
            string? input = null;

            // Act
            string? result = input.ToKebabCase();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ToKebabCase_EmptyString_ReturnsEmpty()
        {
            // Arrange
            string input = "";

            // Act
            string result = input.ToKebabCase();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ToKebabCase_SingleWord_ReturnsLowercase()
        {
            // Arrange
            string input = "hello";

            // Act
            string result = input.ToKebabCase();

            // Assert
            result.Should().Be("hello");
        }

        [Fact]
        public void ToKebabCase_CamelCase_ReturnsKebabCase()
        {
            // Arrange
            string input = "helloWorldTest";

            // Act
            string result = input.ToKebabCase();

            // Assert
            result.Should().Be("hello-world-test");
        }

        [Fact]
        public void ToKebabCase_AlreadyKebabCase_ReturnsSame()
        {
            // Arrange
            string input = "hello-world-test";

            // Act
            string result = input.ToKebabCase();

            // Assert
            result.Should().Be("hello-world-test");
        }

        [Fact]
        public void ToCamelCase_NullInput_ReturnsNull()
        {
            // Arrange
            string? input = null;

            // Act
            string? result = input.ToCamelCase();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ToCamelCase_EmptyString_ReturnsEmpty()
        {
            // Arrange
            string input = "";

            // Act
            string result = input.ToCamelCase();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ToCamelCase_SingleWord_ReturnsLowercase()
        {
            // Arrange
            string input = "hello";

            // Act
            string result = input.ToCamelCase();

            // Assert
            result.Should().Be("hello");
        }

        [Fact]
        public void ToCamelCase_KebabCase_ReturnsCamelCase()
        {
            // Arrange
            string input = "hello-world-test";

            // Act
            string result = input.ToCamelCase();

            // Assert
            result.Should().Be("helloWorldTest");
        }

        [Fact]
        public void Truncate_NullInput_ReturnsNull()
        {
            // Arrange
            string? input = null;

            // Act
            string? result = input.Truncate(10);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Truncate_EmptyString_ReturnsEmpty()
        {
            // Arrange
            string input = "";

            // Act
            string result = input.Truncate(10);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Truncate_StringShorterThanMaxLength_ReturnsOriginal()
        {
            // Arrange
            string input = "hello";

            // Act
            string result = input.Truncate(10);

            // Assert
            result.Should().Be("hello");
        }

        [Fact]
        public void Truncate_StringEqualToMaxLength_ReturnsOriginal()
        {
            // Arrange
            string input = "hello";

            // Act
            string result = input.Truncate(5);

            // Assert
            result.Should().Be("hello");
        }

        [Fact]
        public void Truncate_StringLongerThanMaxLength_TruncatesWithDefaultSuffix()
        {
            // Arrange
            string input = "Hello World!";

            // Act
            string result = input.Truncate(8);

            // Assert
            result.Should().Be("Hello...");
            result.Length.Should().Be(8);
        }

        [Fact]
        public void Truncate_WithCustomSuffix_UsesProvidedSuffix()
        {
            // Arrange
            string input = "Hello World!";
            string suffix = "[...]";

            // Act
            string result = input.Truncate(8, suffix);

            // Assert
            result.Should().Be("Hel[...]");
            result.Length.Should().Be(8);
        }

        [Fact]
        public void Truncate_ZeroMaxLength_ReturnsOnlySuffix()
        {
            // Arrange
            string input = "Hello";
            string suffix = "...";

            // Act
            string result = input.Truncate(3, suffix);

            // Assert
            result.Should().Be(suffix);
            result.Length.Should().Be(suffix.Length);
        }

        [Fact]
        public void Truncate_NegativeMaxLength_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            string input = "Hello";

            // Act
            Action act = () => input.Truncate(-1);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void IsValidEmail_NullInput_ReturnsFalse()
        {
            // Arrange
            string? input = null;

            // Act
            bool result = input.IsValidEmail();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidEmail_EmptyString_ReturnsFalse()
        {
            // Arrange
            string input = "";

            // Act
            bool result = input.IsValidEmail();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidEmail_WhitespaceOnly_ReturnsFalse()
        {
            // Arrange
            string input = "   ";

            // Act
            bool result = input.IsValidEmail();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidEmail_ValidEmail_ReturnsTrue()
        {
            // Arrange
            string input = "user@example.com";

            // Act
            bool result = input.IsValidEmail();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValidEmail_InvalidEmail_ReturnsFalse()
        {
            // Arrange
            string input = "not-an-email";

            // Act
            bool result = input.IsValidEmail();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidUrl_NullInput_ReturnsFalse()
        {
            // Arrange
            string? input = null;

            // Act
            bool result = input.IsValidUrl();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidUrl_EmptyString_ReturnsFalse()
        {
            // Arrange
            string input = "";

            // Act
            bool result = input.IsValidUrl();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidUrl_ValidHttpUrl_ReturnsTrue()
        {
            // Arrange
            string input = "http://example.com";

            // Act
            bool result = input.IsValidUrl();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValidUrl_ValidHttpsUrl_ReturnsTrue()
        {
            // Arrange
            string input = "https://example.com/path?query=value";

            // Act
            bool result = input.IsValidUrl();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValidUrl_InvalidUrl_ReturnsFalse()
        {
            // Arrange
            string input = "not-a-url";

            // Act
            bool result = input.IsValidUrl();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidUrl_FtpUrl_ReturnsFalse()
        {
            // Arrange
            string input = "ftp://example.com";

            // Act
            bool result = input.IsValidUrl();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsNumeric_NullInput_ReturnsFalse()
        {
            // Arrange
            string? input = null;

            // Act
            bool result = input.IsNumeric();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsNumeric_EmptyString_ReturnsFalse()
        {
            // Arrange
            string input = "";

            // Act
            bool result = input.IsNumeric();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsNumeric_WhitespaceOnly_ReturnsFalse()
        {
            // Arrange
            string input = "   ";

            // Act
            bool result = input.IsNumeric();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsNumeric_AllDigits_ReturnsTrue()
        {
            // Arrange
            string input = "1234567890";

            // Act
            bool result = input.IsNumeric();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsNumeric_NegativeNumber_ReturnsFalse()
        {
            // Arrange
            string input = "-123";

            // Act
            bool result = input.IsNumeric();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsNumeric_DecimalNumber_ReturnsFalse()
        {
            // Arrange
            string input = "12.34";

            // Act
            bool result = input.IsNumeric();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsNumeric_ContainsLetters_ReturnsFalse()
        {
            // Arrange
            string input = "123abc";

            // Act
            bool result = input.IsNumeric();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Repeat_NullInput_ReturnsEmpty()
        {
            // Arrange
            string? input = null;

            // Act
            string result = input.Repeat(3);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Repeat_EmptyString_ReturnsEmpty()
        {
            // Arrange
            string input = "";

            // Act
            string result = input.Repeat(3);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Repeat_ZeroCount_ReturnsEmpty()
        {
            // Arrange
            string input = "hello";

            // Act
            string result = input.Repeat(0);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Repeat_PositiveCount_ReturnsRepeatedString()
        {
            // Arrange
            string input = "ab";

            // Act
            string result = input.Repeat(3);

            // Assert
            result.Should().Be("ababab");
        }

        [Fact]
        public void Repeat_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            string input = "hello";

            // Act
            Action act = () => input.Repeat(-1);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void EscapeShell_NullInput_ReturnsNull()
        {
            // Arrange
            string? input = null;

            // Act
            string? result = input.EscapeShell();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void EscapeShell_EmptyString_ReturnsEmpty()
        {
            // Arrange
            string input = "";

            // Act
            string result = input.EscapeShell();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void EscapeShell_NoSpecialChars_ReturnsQuotedString()
        {
            // Arrange
            string input = "hello";

            // Act
            string result = input.EscapeShell();

            // Assert
            result.Should().Be("'hello'");
        }

        [Fact]
        public void EscapeShell_SingleQuote_ReturnsProperlyEscaped()
        {
            // Arrange
            string input = "hello'world";

            // Act
            string result = input.EscapeShell();

            // Assert
            result.Should().Be("'hello'\\''world'");
        }

        [Fact]
        public void EscapeShell_MultipleSingleQuotes_ReturnsProperlyEscaped()
        {
            // Arrange
            string input = "he'll'o'wo'rld";

            // Act
            string result = input.EscapeShell();

            // Assert
            result.Should().Be("'he'\\''ll'\\''o'\\''wo'\\''rld'");
        }

        [Fact]
        public void SafeSubstring_NullInput_ReturnsEmpty()
        {
            // Arrange
            string? input = null;

            // Act
            string result = input.SafeSubstring(0, 5);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void SafeSubstring_EmptyString_ReturnsEmpty()
        {
            // Arrange
            string input = "";

            // Act
            string result = input.SafeSubstring(0, 5);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void SafeSubstring_ValidIndices_ReturnsSubstring()
        {
            // Arrange
            string input = "Hello World!";

            // Act
            string result = input.SafeSubstring(0, 5);

            // Assert
            result.Should().Be("Hello");
        }

        [Fact]
        public void SafeSubstring_StartIndexBeyondLength_ReturnsEmpty()
        {
            // Arrange
            string input = "Hello";

            // Act
            string result = input.SafeSubstring(10, 5);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void SafeSubstring_NegativeStartIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            string input = "Hello";

            // Act
            Action act = () => input.SafeSubstring(-1, 5);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void SafeSubstring_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            string input = "Hello";

            // Act
            Action act = () => input.SafeSubstring(0, -1);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void SafeSubstring_LengthBeyondEnd_ReturnsToEnd()
        {
            // Arrange
            string input = "Hello";

            // Act
            string result = input.SafeSubstring(2, 10);

            // Assert
            result.Should().Be("llo");
        }

        [Fact]
        public void StartsWithAny_NullInput_ReturnsFalse()
        {
            // Arrange
            string? input = null;

            // Act
            bool result = input.StartsWithAny("hello", "world");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void StartsWithAny_NullPrefixes_ReturnsFalse()
        {
            // Arrange
            string input = "hello";

            // Act
            bool result = input.StartsWithAny(null!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void StartsWithAny_EmptyPrefixes_ReturnsFalse()
        {
            // Arrange
            string input = "hello";

            // Act
            bool result = input.StartsWithAny();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void StartsWithAny_MatchingPrefix_ReturnsTrue()
        {
            // Arrange
            string input = "hello world";

            // Act
            bool result = input.StartsWithAny("hello", "world");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void StartsWithAny_NoMatchingPrefix_ReturnsFalse()
        {
            // Arrange
            string input = "hello world";

            // Act
            bool result = input.StartsWithAny("hi", "there");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void StartsWithAny_WithNullPrefixInArray_ReturnsTrueIfOtherMatches()
        {
            // Arrange
            string input = "hello world";

            // Act
            bool result = input.StartsWithAny(null, "hello", null);

            // Assert
            result.Should().BeTrue();
        }
    }
}