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
    public class AppConfigurationBuilderTests
    {
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
