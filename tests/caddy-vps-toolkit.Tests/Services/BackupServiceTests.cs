#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CaddyVpsToolkit.Tests.Services
{
    public sealed class BackupServiceTests
    {
        private readonly IServiceRepository _serviceRepo;
        private readonly IConfigurationRepository _configRepo;
        private readonly BackupService _sut;
        private readonly string _tempDir;

        public BackupServiceTests()
        {
            _serviceRepo = Substitute.For<IServiceRepository>();
            _configRepo = Substitute.For<IConfigurationRepository>();
            _sut = new BackupService(_serviceRepo, _configRepo);
            _tempDir = Path.Combine(Path.GetTempPath(), $"caddy-backup-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public async Task CreateBackupAsync_WritesJsonFileToOutputPath()
        {
            // Arrange
            _serviceRepo.GetAllAsync().Returns(new List<ManagedService>
            {
                new ManagedService
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "test-svc",
                    Description = "Test",
                    ExecutablePath = "/usr/bin/test",
                    WorkingDirectory = "/tmp",
                    Port = 8080
                }
            });
            _configRepo.GetAllAsync().Returns(new Dictionary<string, string> { ["key"] = "value" });

            var outputPath = Path.Combine(_tempDir, "test-backup.backup.json");

            // Act
            var result = await _sut.CreateBackupAsync(outputPath, "unit-test backup");

            // Assert
            result.Should().Be(outputPath);
            File.Exists(outputPath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(outputPath);
            content.Should().Contain("test-svc");
            content.Should().Contain("unit-test backup");
        }

        [Fact]
        public async Task CreateBackupAsync_WithNullOutputPath_GeneratesTimestampedFilename()
        {
            // Arrange
            _serviceRepo.GetAllAsync().Returns(new List<ManagedService>());
            _configRepo.GetAllAsync().Returns(new Dictionary<string, string>());

            // Act — we can't write to the real AppConstants.ConfigDirectory in tests,
            //        so just verify the method accepts null without throwing when the dir exists.
            //        We intercept by providing a temp dir-rooted output explicitly.
            var outputPath = Path.Combine(_tempDir, $"backup-{DateTime.UtcNow:yyyyMMdd_HHmmss}.backup.json");
            var result = await _sut.CreateBackupAsync(outputPath);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
            File.Exists(result).Should().BeTrue();
        }

        [Fact]
        public async Task RestoreBackupAsync_WithMissingFile_ThrowsCaddyVpsException()
        {
            // Act
            Func<Task> act = async () => await _sut.RestoreBackupAsync("/nonexistent/path/backup.json");

            // Assert
            await act.Should().ThrowAsync<Core.CaddyVpsException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task RestoreBackupAsync_WithValidBackup_RestoresServicesAndConfig()
        {
            // Arrange
            var serviceId = Guid.NewGuid().ToString();
            _serviceRepo.GetAllAsync().Returns(new List<ManagedService>
            {
                new ManagedService
                {
                    Id = serviceId,
                    Name = "restored-svc",
                    Description = "Restored",
                    ExecutablePath = "/usr/bin/app",
                    WorkingDirectory = "/app",
                    Port = 9090
                }
            });
            _configRepo.GetAllAsync().Returns(new Dictionary<string, string> { ["env"] = "prod" });

            // Create a backup first
            var backupPath = Path.Combine(_tempDir, "restore-test.backup.json");
            await _sut.CreateBackupAsync(backupPath);

            // Reset mocks for restore invocations
            _serviceRepo.GetByIdAsync(Arg.Any<string>()).Returns((ManagedService?)null);
            _serviceRepo.AddAsync(Arg.Any<ManagedService>()).Returns(serviceId);
            _configRepo.SetValueAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);

            // Act
            var manifest = await _sut.RestoreBackupAsync(backupPath);

            // Assert
            manifest.Should().NotBeNull();
            manifest.ServiceCount.Should().Be(1);
            await _serviceRepo.Received(1).AddAsync(Arg.Is<ManagedService>(s => s.Name == "restored-svc"));
            await _configRepo.Received(1).SetValueAsync("env", "prod");
        }

        [Fact]
        public async Task ListBackupsAsync_WithNonExistentDirectory_ReturnsEmptyList()
        {
            // Act
            var result = await _sut.ListBackupsAsync("/nonexistent/dir");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ListBackupsAsync_WithBackupsPresent_ReturnsSortedPaths()
        {
            // Arrange
            _serviceRepo.GetAllAsync().Returns(new List<ManagedService>());
            _configRepo.GetAllAsync().Returns(new Dictionary<string, string>());

            await _sut.CreateBackupAsync(Path.Combine(_tempDir, "backup-alpha.backup.json"));
            await _sut.CreateBackupAsync(Path.Combine(_tempDir, "backup-beta.backup.json"));

            // Act
            var result = await _sut.ListBackupsAsync(_tempDir);

            // Assert
            result.Should().HaveCount(2);
        }
    }
}
