#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Contract for creating and restoring configuration backups.
    /// </summary>
    public interface IBackupService
    {
        /// <summary>Creates a backup of all services and configuration, returning the output file path.</summary>
        /// <param name="outputPath">Optional full path where the backup file should be written. When null, a timestamped file is created in the config directory.</param>
        /// <param name="description">Optional human-readable description stored in the backup manifest.</param>
        /// <returns>The full path of the created backup file.</returns>
        Task<string> CreateBackupAsync(string? outputPath = null, string description = "");

        /// <summary>Restores services and configuration from a previously created backup file.</summary>
        /// <param name="backupFilePath">Path to the backup file to restore.</param>
        /// <returns>The manifest describing the restored backup.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="backupFilePath"/> is null or whitespace.</exception>
        /// <exception cref="CaddyVpsException">Thrown when the backup file is missing, cannot be read, or contains invalid JSON.</exception>
        Task<BackupManifest> RestoreBackupAsync(string backupFilePath);

        /// <summary>Verifies the integrity of a backup file by comparing its SHA-256 checksum.</summary>
        /// <param name="backupFilePath">Path to the backup file to verify.</param>
        /// <returns>The integrity result, including whether the checksum matched.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="backupFilePath"/> is null or whitespace.</exception>
        /// <exception cref="CaddyVpsException">Thrown when the backup file is missing, cannot be read, or contains invalid JSON.</exception>
        Task<BackupManifest.BackupIntegrityResult> VerifyBackupAsync(string backupFilePath);

        /// <summary>Lists all backup files found in the given directory (defaults to the config directory).</summary>
        /// <param name="backupDirectory">Optional directory to search for backup files. When null, the config directory is used.</param>
        /// <returns>A sorted list of backup file paths. Empty if the directory does not exist or contains no backups.</returns>
        Task<IReadOnlyList<string>> ListBackupsAsync(string? backupDirectory = null);
    }

    /// <summary>
    /// Creates and restores JSON backups of managed services, application configuration,
    /// and the Caddyfile. Each backup is a self-contained JSON file that can be transferred
    /// to another host and restored with a single command.
    /// </summary>
    public sealed class BackupService : IBackupService
    {
        private const string BackupFileExtension = ".backup.json";

        private readonly IServiceRepository _serviceRepository;
        private readonly IConfigurationRepository _configRepository;

        /// <summary>
        /// Initializes a new instance of <see cref="BackupService"/>.
        /// </summary>
        /// <param name="serviceRepository">Repository used to read and write managed services.</param>
        /// <param name="configRepository">Repository used to read and write application configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceRepository"/> or <paramref name="configRepository"/> is null.</exception>
        public BackupService(IServiceRepository serviceRepository, IConfigurationRepository configRepository)
        {
            _serviceRepository = serviceRepository ?? throw new ArgumentNullException(nameof(serviceRepository));
            _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
        }

        /// <summary>
        /// Creates a backup of all services and configuration, returning the output file path.
        /// </summary>
        /// <param name="outputPath">Optional full path where the backup file should be written. When null, a timestamped file is created in the config directory.</param>
        /// <param name="description">Optional human-readable description stored in the backup manifest.</param>
        /// <returns>The full path of the created backup file.</returns>
        public async Task<string> CreateBackupAsync(string? outputPath = null, string description = "")
        {
            var services = await _serviceRepository.GetAllAsync();
            var config = await _configRepository.GetAllAsync();

            string? caddyfileContent = null;
            if (File.Exists(AppConstants.CaddyfilePath))
                caddyfileContent = await File.ReadAllTextAsync(AppConstants.CaddyfilePath);

            var manifest = new BackupManifest
            {
                Services = services,
                Configuration = config,
                CaddyfileContent = caddyfileContent,
                Description = description
            };

            // Compute and store SHA-256 checksum for integrity verification
            manifest.Sha256Checksum = manifest.ComputeSha256Checksum();

            var directory = outputPath is not null
                ? Path.GetDirectoryName(outputPath) ?? AppConstants.ConfigDirectory
                : AppConstants.ConfigDirectory;

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var filePath = outputPath ?? PathUtilities.SafeCombine(
                directory,
                $"backup-{manifest.CreatedAt:yyyyMMdd_HHmmss}{BackupFileExtension}");

            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);

            return filePath;
        }

        /// <inheritdoc/>
        public async Task<BackupManifest> RestoreBackupAsync(string backupFilePath)
        {
            if (string.IsNullOrWhiteSpace(backupFilePath))
                throw new ArgumentException("Backup file path must not be empty.", nameof(backupFilePath));

            if (!File.Exists(backupFilePath))
                throw new CaddyVpsException($"Backup file not found: {backupFilePath}", "BACKUP_NOT_FOUND");

            string json;
            try
            {
                json = await File.ReadAllTextAsync(backupFilePath);
            }
            catch (Exception ex)
            {
                throw new CaddyVpsException($"Failed to read backup file: {ex.Message}", "BACKUP_READ_ERROR", null, ex);
            }

            BackupManifest manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<BackupManifest>(json)
                    ?? throw new CaddyVpsException("Backup file is empty or malformed.", "BACKUP_PARSE_ERROR");
            }
            catch (JsonException ex)
            {
                throw new CaddyVpsException($"Backup file contains invalid JSON: {ex.Message}", "BACKUP_PARSE_ERROR", null, ex);
            }

            // Restore services
            foreach (var service in manifest.Services)
            {
                var existing = await _serviceRepository.GetByIdAsync(service.Id);
                if (existing is null)
                    await _serviceRepository.AddAsync(service);
                else
                    await _serviceRepository.UpdateAsync(service);
            }

            // Restore configuration
            foreach (var kvp in manifest.Configuration)
                await _configRepository.SetValueAsync(kvp.Key, kvp.Value);

            // Restore Caddyfile
            if (manifest.CaddyfileContent is not null)
            {
                var caddyDir = Path.GetDirectoryName(AppConstants.CaddyfilePath);
                if (caddyDir is not null && !Directory.Exists(caddyDir))
                    Directory.CreateDirectory(caddyDir);

                await File.WriteAllTextAsync(AppConstants.CaddyfilePath, manifest.CaddyfileContent);
            }

            return manifest;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<string>> ListBackupsAsync(string? backupDirectory = null)
        {
            var directory = backupDirectory ?? AppConstants.ConfigDirectory;

            if (!Directory.Exists(directory))
                return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

            var files = Directory.GetFiles(directory, $"*{BackupFileExtension}");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            return Task.FromResult<IReadOnlyList<string>>(files);
        }

        /// <inheritdoc/>
        public async Task<BackupManifest.BackupIntegrityResult> VerifyBackupAsync(string backupFilePath)
        {
            if (string.IsNullOrWhiteSpace(backupFilePath))
                throw new ArgumentException("Backup file path must not be empty.", nameof(backupFilePath));

            if (!File.Exists(backupFilePath))
                throw new CaddyVpsException($"Backup file not found: {backupFilePath}", "BACKUP_NOT_FOUND");

            string json;
            try
            {
                json = await File.ReadAllTextAsync(backupFilePath);
            }
            catch (Exception ex)
            {
                throw new CaddyVpsException($"Failed to read backup file: {ex.Message}", "BACKUP_READ_ERROR", null, ex);
            }

            BackupManifest manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<BackupManifest>(json)
                    ?? throw new CaddyVpsException("Backup file is empty or malformed.", "BACKUP_PARSE_ERROR");
            }
            catch (JsonException ex)
            {
                throw new CaddyVpsException($"Backup file contains invalid JSON: {ex.Message}", "BACKUP_PARSE_ERROR", null, ex);
            }

            // Verify that the stored checksum matches the computed checksum
            var computedChecksum = manifest.ComputeSha256Checksum();

            if (manifest.Sha256Checksum == computedChecksum)
            {
                return new BackupManifest.BackupIntegrityResult(manifest, computedChecksum);
            }

            return new BackupManifest.BackupIntegrityResult(manifest.Sha256Checksum, computedChecksum, manifest);
        }
    }
}
