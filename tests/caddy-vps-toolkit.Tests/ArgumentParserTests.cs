// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Cli;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Core;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CaddyVpsToolkit.Tests
{
    public class ArgumentParserTests
    {
        [Fact]
        public void GetCommand_CommandWithMixedCase_ReturnsLowercaseCommand()
        {
            // Arrange
            var parser = new ArgumentParser(new[] { "ADD", "--name=api", "--port=8080" });

            // Act
            var command = parser.GetCommand();

            // Assert
            command.Should().Be("add");
        }

        [Fact]
        public void GetFlagValue_EqualsSignFormat_ExtractsValueCorrectly()
        {
            // Arrange
            var parser = new ArgumentParser(new[] { "add", "--name=my-api", "--port=8080" });

            // Act & Assert
            parser.GetFlagValue("name").Should().Be("my-api");
            parser.GetFlagValue("port").Should().Be("8080");
        }

        [Fact]
        public void GetFlagValue_SpaceSeparatedFormat_ExtractsValueCorrectly()
        {
            // Arrange
            var parser = new ArgumentParser(new[] { "add", "--name", "my-api" });

            // Act
            var value = parser.GetFlagValue("name");

            // Assert
            value.Should().Be("my-api");
        }

        [Fact]
        public async Task ServiceRepository_GetByIdAsync_WithMockedRepository_ReturnsExpectedService()
        {
            // Arrange
            var serviceId = Guid.NewGuid().ToString();
            var expectedService = new ManagedService
            {
                Id = serviceId,
                Name = "test-api",
                Description = "Integration API",
                ExecutablePath = "/usr/bin/dotnet",
                WorkingDirectory = "/var/app",
                Port = 5000,
                Type = ServiceType.ApiService
            };

            var repoMock = Substitute.For<IServiceRepository>();
            repoMock.GetByIdAsync(serviceId).Returns(expectedService);

            // Act
            var service = await repoMock.GetByIdAsync(serviceId);

            // Assert
            service.Should().NotBeNull();
            service!.Name.Should().Be("test-api");
            service.Port.Should().Be(5000);
            service.Type.Should().Be(ServiceType.ApiService);
            await repoMock.Received(1).GetByIdAsync(serviceId);
        }
    }
}
