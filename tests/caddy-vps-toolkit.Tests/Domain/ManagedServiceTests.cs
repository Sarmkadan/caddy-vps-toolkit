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

/// <summary>
/// Tests for the ManagedService class.
/// </summary>
namespace CaddyVpsToolkit.Tests.Domain
{
    public sealed class ManagedServiceTests
    {
        /// <summary>
        /// Verifies that a valid ManagedService instance does not throw a ValidationException.
        /// </summary>
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

        /// <summary>
        /// Verifies that a ManagedService instance with a missing Name property throws a ValidationException.
        /// </summary>
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

        /// <summary>
        /// Verifies that a ManagedService instance with an invalid Port property throws a ValidationException.
        /// </summary>
        /// <param name="port">The Port value to test.</param>
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

        /// <summary>
        /// Verifies that the UpdateStatus method changes the Status and UpdatedAt properties.
        /// </summary>
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

        /// <summary>
        /// Verifies that the GetSystemdUnitName method returns the explicit name when provided.
        /// </summary>
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

        /// <summary>
        /// Verifies that the GetSystemdUnitName method generates a name from the Name property when not provided.
        /// </summary>
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
