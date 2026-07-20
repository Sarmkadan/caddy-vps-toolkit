#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CaddyVpsToolkit.Core;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Manifest describing the contents and metadata of a configuration backup.
    /// Serialized to JSON when a backup archive is created.
    /// </summary>
    public sealed class BackupManifest
    {
        /// <summary>Unique identifier for this backup.</summary>
        public string BackupId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>UTC timestamp when the backup was created.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Application version at the time the backup was created.</summary>
        public string AppVersion { get; set; } = AppConstants.AppVersion;

        /// <summary>Snapshot of all managed service configurations.</summary>
        public List<ManagedService> Services { get; set; } = new();

        /// <summary>Snapshot of all application configuration key-value pairs.</summary>
        public Dictionary<string, string> Configuration { get; set; } = new();

        /// <summary>Full content of the Caddyfile at backup time. Null if no Caddyfile was found.</summary>
        public string? CaddyfileContent { get; set; }

        /// <summary>Number of services included in this backup.</summary>
        public int ServiceCount => Services.Count;

        /// <summary>Optional human-readable description provided at backup time.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>SHA-256 checksum of the backup file for integrity verification.</summary>
        public string? Sha256Checksum { get; set; }

        /// <summary>
        /// Computes the SHA-256 checksum of the backup manifest JSON representation.
        /// </summary>
        public string ComputeSha256Checksum()
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Result of a backup integrity verification operation.
        /// </summary>
        public sealed class BackupIntegrityResult
        {
            /// <summary>True if the backup file is intact and has not been modified.</summary>
            public bool IsIntact => IsSuccess;

            /// <summary>True if the integrity check passed successfully.</summary>
            public bool IsSuccess { get; }

            /// <summary>Error message if the integrity check failed.</summary>
            public string? ErrorMessage { get; }

            /// <summary>Error code if the integrity check failed.</summary>
            public string? ErrorCode { get; }

            /// <summary>SHA-256 checksum stored in the backup manifest.</summary>
            public string? ExpectedChecksum { get; }

            /// <summary>Computed SHA-256 checksum of the backup file.</summary>
            public string? ComputedChecksum { get; }

            /// <summary>Backup manifest associated with this verification.</summary>
            public BackupManifest? Manifest { get; }

            /// <summary>
            /// Creates a successful integrity verification result.
            /// </summary>
            public BackupIntegrityResult(BackupManifest manifest, string computedChecksum)
            {
                IsSuccess = true;
                Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
                ComputedChecksum = computedChecksum ?? throw new ArgumentNullException(nameof(computedChecksum));
                ExpectedChecksum = manifest.Sha256Checksum;
            }

            /// <summary>
            /// Creates a failed integrity verification result.
            /// </summary>
            public BackupIntegrityResult(string expectedChecksum, string computedChecksum, BackupManifest? manifest = null)
            {
                IsSuccess = false;
                ErrorMessage = "Backup integrity check failed: checksum mismatch";
                ErrorCode = "BACKUP_INTEGRITY_CHECK_FAILED";
                ExpectedChecksum = expectedChecksum;
                ComputedChecksum = computedChecksum;
                Manifest = manifest;
            }
        }
    }
}
