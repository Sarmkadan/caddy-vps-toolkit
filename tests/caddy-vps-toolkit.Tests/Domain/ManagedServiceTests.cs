#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Domain
{
    public sealed class ManagedServiceTests
    {
        [Fact]
        public void Validate_WithValidData_ShouldNotThrow()
        {
            // Arrange
            var service = new ManagedService
            {
                Name = "Valid Service",
                ExecutablePath = "/bin/bash",
                WorkingDirectory = "/tmp",
                Port = 8080
            };

            // Act
            var act = () => service.Validate();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_WithMissingName_ShouldThrowValidationException()
        {
            // Arrange
            var service = new ManagedService
            {
                ExecutablePath = "/bin/bash",
                WorkingDirectory = "/tmp",
                Port = 8080
            };

            // Act
            var act = () => service.Validate();

            // Assert
            act.Should().Throw<System.ComponentModel.DataAnnotations.ValidationException>().WithMessage("*name is required*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(65536)]
        public void Validate_WithInvalidPort_ShouldThrowValidationException(int port)
        {
            // Arrange
            var service = new ManagedService
            {
                Name = "Service",
                ExecutablePath = "/bin/bash",
                WorkingDirectory = "/tmp",
                Port = port
            };

            // Act
            var act = () => service.Validate();

            // Assert
            act.Should().Throw<System.ComponentModel.DataAnnotations.ValidationException>().WithMessage("*Port must be between 1 and 65535*");
        }

        [Fact]
        public void UpdateStatus_ShouldChangeStatusAndUpdatedAt()
        {
            // Arrange
            var service = new ManagedService();
            var originalTime = service.UpdatedAt;

            // Simulate delay
            System.Threading.Thread.Sleep(10);

            // Act
            service.UpdateStatus(ServiceStatus.Running);

            // Assert
            service.Status.Should().Be(ServiceStatus.Running);
            service.UpdatedAt.Should().BeAfter(originalTime);
        }

        [Fact]
        public void GetSystemdUnitName_WithExplicitName_ShouldReturnExplicitName()
        {
            // Arrange
            var service = new ManagedService { SystemdUnitName = "custom.service" };

            // Act
            var name = service.GetSystemdUnitName();

            // Assert
            name.Should().Be("custom.service");
        }

        [Fact]
        public void GetSystemdUnitName_WithNoExplicitName_ShouldGenerateFromName()
        {
            // Arrange
            var service = new ManagedService { Name = "My App" };

            // Act
            var name = service.GetSystemdUnitName();

            // Assert
            name.Should().Be("vps-my-app.service");
        }
    }
}
