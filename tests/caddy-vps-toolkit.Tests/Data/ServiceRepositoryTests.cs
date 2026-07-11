#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Data
{
    /// <summary>
    /// Tests for the ServiceRepository class.
    /// </summary>
    public sealed class ServiceRepositoryTests : IDisposable
    {
        private readonly ServiceRepository _sut;
        private readonly string _tempDbPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceRepositoryTests"/> class.
        /// </summary>
        public ServiceRepositoryTests()
        {
            // Ensure the directory for the static AppConstants.DatabasePath exists to avoid constructor crash
            var defaultDbPath = CaddyVpsToolkit.Core.AppConstants.DatabasePath;
            var defaultDir = Path.GetDirectoryName(defaultDbPath);
            if (!string.IsNullOrEmpty(defaultDir) && !Directory.Exists(defaultDir))
            {
                Directory.CreateDirectory(defaultDir);
            }
            
            _sut = new ServiceRepository();
            
            // Use reflection to set to an in-memory database shared across connections
            var connectionStringField = typeof(ServiceRepository).GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance);
            connectionStringField?.SetValue(_sut, $"Data Source=file:testdb_{Guid.NewGuid()}?mode=memory&cache=shared;Version=3;");
            
            // Call InitializeDatabase again to create tables in the memory db
            var initMethod = typeof(ServiceRepository).GetMethod("InitializeDatabase", BindingFlags.NonPublic | BindingFlags.Instance);
            initMethod?.Invoke(_sut, null);
        }

        /// <summary>
        /// Tests that adding a service inserts it and returns the correct ID.
        /// </summary>
        [Fact]
        public async Task AddAsync_ShouldInsertAndReturnId()
        {
            // Arrange
            var service = new ManagedService
            {
                Name = "IntegrationTestService",
                Description = "Test",
                ExecutablePath = "/bin/test",
                WorkingDirectory = "/tmp",
                Port = 8080
            };

            // Act
            var result = await _sut.AddAsync(service);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().Be(service.Id);
        }

        /// <summary>
        /// Tests that getting a service by ID returns the inserted service.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnInsertedService()
        {
            // Arrange
            var service = new ManagedService
            {
                Name = "GetTestService",
                Description = "Test",
                ExecutablePath = "/bin/test",
                WorkingDirectory = "/tmp",
                Port = 8081
            };
            await _sut.AddAsync(service);

            // Act
            var retrieved = await _sut.GetByIdAsync(service.Id);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved.Name.Should().Be("GetTestService");
            retrieved.Port.Should().Be(8081);
        }

        /// <summary>
        /// Tests that updating a service modifies the existing service.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldModifyExistingService()
        {
            // Arrange
            var service = new ManagedService
            {
                Name = "UpdateTestService",
                Description = "Test",
                ExecutablePath = "/bin/test",
                WorkingDirectory = "/tmp",
                Port = 8082
            };
            await _sut.AddAsync(service);

            // Act
            service.Description = "Updated Description";
            service.Port = 9090;
            var updateResult = await _sut.UpdateAsync(service);
            var retrieved = await _sut.GetByIdAsync(service.Id);

            // Assert
            updateResult.Should().BeTrue();
            retrieved.Description.Should().Be("Updated Description");
            retrieved.Port.Should().Be(9090);
        }

        /// <summary>
        /// Tests that deleting a service removes it.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldRemoveService()
        {
            // Arrange
            var service = new ManagedService
            {
                Name = "DeleteTestService",
                Description = "Test",
                ExecutablePath = "/bin/test",
                WorkingDirectory = "/tmp",
                Port = 8083
            };
            await _sut.AddAsync(service);

            // Act
            var deleteResult = await _sut.DeleteAsync(service.Id);
            var retrieved = await _sut.GetByIdAsync(service.Id);

            // Assert
            deleteResult.Should().BeTrue();
            retrieved.Should().BeNull();
        }

        /// <summary>
        /// Tests that getting the count of services returns the correct count.
        /// </summary>
        [Fact]
        public async Task GetCountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            var initialCount = await _sut.GetCountAsync();
            var service = new ManagedService
            {
                Name = "CountTestService",
                Description = "Test",
                ExecutablePath = "/bin/test",
                WorkingDirectory = "/tmp",
                Port = 8084
            };
            await _sut.AddAsync(service);

            // Act
            var newCount = await _sut.GetCountAsync();

            // Assert
            newCount.Should().Be(initialCount + 1);
        }

        /// <summary>
        /// Disposes of the test instance.
        /// </summary>
        public void Dispose()
        {
            var tempDir = Path.GetDirectoryName(_tempDbPath);
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { /* Ignore */ }
            }
        }
    }
}
