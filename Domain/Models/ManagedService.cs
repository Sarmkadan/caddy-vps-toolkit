#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Core;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Represents a service managed by the VPS toolkit. Encapsulates all metadata needed to
    /// generate systemd unit files, Caddy reverse proxy routes, and health check configurations.
    /// </summary>
    public sealed class ManagedService
    {
        /// <summary>Unique service identifier, auto-generated as a GUID.</summary>
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Human-readable service name (3-255 chars). Used to derive systemd unit names.</summary>
        [Required]
        [StringLength(255, MinimumLength = 3)]
        public string Name { get; set; }

        /// <summary>Service description for display and systemd unit Description field.</summary>
        [Required]
        public string Description { get; set; }

        /// <summary>Service type classification (e.g., Web, API, Worker).</summary>
        public ServiceType Type { get; set; }

        /// <summary>Absolute path to the service executable or entrypoint script.</summary>
        [Required]
        public string ExecutablePath { get; set; }

        /// <summary>Working directory for the process (used as WorkingDirectory in systemd unit).</summary>
        [Required]
        public string WorkingDirectory { get; set; }

        /// <summary>Command-line arguments passed to the executable.</summary>
        public string Arguments { get; set; }

        /// <summary>Current runtime status. Updated by health checks and systemd polling.</summary>
        public ServiceStatus Status { get; set; } = ServiceStatus.Stopped;

        /// <summary>Primary port the service listens on. Must be between 1 and 65535.</summary>
        public int Port { get; set; }

        /// <summary>Host/IP the service binds to. Defaults to "localhost" for security.</summary>
        public string HostBinding { get; set; } = "localhost";

        /// <summary>Whether the service should start automatically on system boot.</summary>
        public bool AutoStart { get; set; }

        /// <summary>Startup priority for ordering service dependencies (lower = earlier).</summary>
        public int Priority { get; set; }

        /// <summary>Environment variables in KEY=VALUE format, one per line.</summary>
        public string EnvironmentVariables { get; set; }

        /// <summary>UTC timestamp when the service record was created.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the last status or configuration change.</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Override for the systemd unit filename. If null, derived from <see cref="Name"/>.</summary>
        public string SystemdUnitName { get; set; }

        /// <summary>Additional ports exposed by the service beyond the primary <see cref="Port"/>.</summary>
        public List<ServicePort> ExposedPorts { get; set; } = new();

        /// <summary>Health check configuration (endpoint, interval, thresholds). Null if no health check.</summary>
        public HealthCheckConfig HealthCheck { get; set; }

        /// <summary>Whether the service is enabled for management. Disabled services are ignored by health checks.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Validates required fields and port range. Throws <see cref="ValidationException"/> on failure.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new ValidationException("Service name is required");

            if (string.IsNullOrWhiteSpace(ExecutablePath))
                throw new ValidationException("Executable path is required");

            if (Port <= 0 || Port > 65535)
                throw new ValidationException($"Port must be between 1 and 65535, got {Port}");

            if (string.IsNullOrWhiteSpace(WorkingDirectory))
                throw new ValidationException("Working directory is required");
        }

        /// <summary>
        /// Updates the service status and sets <see cref="UpdatedAt"/> to current UTC time.
        /// </summary>
        /// <param name="newStatus">The new status to assign.</param>
        public void UpdateStatus(ServiceStatus newStatus)
        {
            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns the systemd unit name. Uses <see cref="SystemdUnitName"/> if set,
        /// otherwise derives it as "vps-{name}.service" with lowercase and hyphen-separated words.
        /// </summary>
        public string GetSystemdUnitName()
        {
            return SystemdUnitName ?? $"vps-{Name.ToLower().Replace(" ", "-")}.service";
        }

        /// <summary>
        /// Returns a name safe for use as a Caddy identifier (named matcher, snippet name, etc.).
        /// Caddy identifiers do not allow hyphens; replaces them with underscores.
        /// </summary>
        public string GetCaddySafeName()
        {
            return Name.ToLower().Replace(" ", "_").Replace("-", "_");
        }
    }
}
