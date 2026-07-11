#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using CaddyVpsToolkit.Configuration;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Configuration
{
    /// <summary>
    /// Tests for the <see cref="AppConfigurationBuilder"/> class.
    /// </summary>
    public sealed class AppConfigurationBuilderTests
    {
        /// <summary>
        /// Verifies that <see cref="AppConfigurationBuilder.WithSetting(string, string)"/> correctly adds a setting to the configuration.
        /// </summary>
        [Fact]
        public void WithSetting_WithValidKey_ShouldAddSetting()
        {
            // Arrange
            var builder = new AppConfigurationBuilder();

            // Act
            builder.WithSetting("TestKey", "TestValue");
            var config = builder.Build();

            // Assert
            config.GetString("TestKey").Should().Be("TestValue");
        }

        /// <summary>
        /// Verifies that <see cref="AppConfigurationBuilder.WithSetting(string, string)"/> throws an <see cref="ArgumentException"/> when given a null key.
        /// </summary>
        [Fact]
        public void WithSetting_WithNullKey_ShouldThrowArgumentException()
        {
            // Arrange
            var builder = new AppConfigurationBuilder();

            // Act
            Action act = () => builder.WithSetting(null!, "Value");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Verifies that <see cref="AppConfigurationBuilder.WithDefaults(Dictionary{string, object})"/> correctly adds only missing keys to the configuration.
        /// </summary>
        [Fact]
        public void WithDefaults_ShouldAddOnlyMissingKeys()
        {
            // Arrange
            var builder = new AppConfigurationBuilder()
                .WithSetting("Key1", "Value1");

            var defaults = new Dictionary<string, object>
            {
                { "Key1", "Default1" },
                { "Key2", "Default2" }
            };

            // Act
            builder.WithDefaults(defaults);
            var config = builder.Build();

            // Assert
            config.GetString("Key1").Should().Be("Value1");
            config.GetString("Key2").Should().Be("Default2");
        }

        /// <summary>
        /// Verifies that <see cref="AppConfigurationBuilder.WithJsonFile(string)"/> throws a <see cref="FileNotFoundException"/> when given a non-existent file path.
        /// </summary>
        [Fact]
        public void WithJsonFile_WithNonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var builder = new AppConfigurationBuilder();

            // Act
            Action act = () => builder.WithJsonFile("nonexistent.json");

            // Assert
            act.Should().Throw<FileNotFoundException>();
        }

        /// <summary>
        /// Verifies that <see cref="AppConfigurationBuilder.GetInt(string)"/> correctly parses a valid integer value from the configuration.
        /// </summary>
        /// <param name="Port">The key to retrieve the integer value for.</param>
        /// <returns>The parsed integer value.</returns>
        [Fact]
        public void GetInt_WithValidNumber_ShouldReturnParsedInt()
        {
            // Arrange
            var builder = new AppConfigurationBuilder().WithSetting("Port", "8080");
            var config = builder.Build();

            // Act
            var result = config.GetInt("Port");

            // Assert
            result.Should().Be(8080);
        }

        /// <summary>
        /// Verifies that <see cref="AppConfigurationBuilder.GetInt(string, int)"/> returns the default value when given an invalid integer value.
        /// </summary>
        /// <param name="Port">The key to retrieve the integer value for.</param>
        /// <param name="defaultValue">The default value to return if the key is not found or has an invalid value.</param>
        /// <returns>The parsed integer value or the default value.</returns>
        [Fact]
        public void GetInt_WithInvalidNumber_ShouldReturnDefault()
        {
            // Arrange
            var builder = new AppConfigurationBuilder().WithSetting("Port", "Invalid");
            var config = builder.Build();

            // Act
            var result = config.GetInt("Port", 80);

            // Assert
            result.Should().Be(80);
        }

        /// <summary>
        /// Verifies that <see cref="AppConfigurationBuilder.GetBool(string)"/> correctly parses a valid boolean value from the configuration.
        /// </summary>
        /// <param name="IsEnabled">The key to retrieve the boolean value for.</param>
        /// <returns>The parsed boolean value.</returns>
        [Fact]
        public void GetBool_WithValidBool_ShouldReturnParsedBool()
        {
            // Arrange
            var builder = new AppConfigurationBuilder().WithSetting("IsEnabled", "True");
            var config = builder.Build();

            // Act
            var result = config.GetBool("IsEnabled");

            // Assert
            result.Should().BeTrue();
        }
    }
}
