#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
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
    }
}
