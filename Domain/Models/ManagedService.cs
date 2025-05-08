// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CaddyVpsToolkit.Core;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Represents a service managed by the VPS toolkit
    /// </summary>
    public class ManagedService
    {
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(255, MinimumLength = 3)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        public ServiceType Type { get; set; }

        [Required]
        public string ExecutablePath { get; set; }

        [Required]
        public string WorkingDirectory { get; set; }

        public string Arguments { get; set; }

        public ServiceStatus Status { get; set; } = ServiceStatus.Stopped;

        public int Port { get; set; }

        public string HostBinding { get; set; } = "localhost";

        public bool AutoStart { get; set; }

        public int Priority { get; set; }

        public string EnvironmentVariables { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string SystemdUnitName { get; set; }

        public List<ServicePort> ExposedPorts { get; set; } = new();

        public HealthCheckConfig HealthCheck { get; set; }

        public bool IsEnabled { get; set; } = true;

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

        public void UpdateStatus(ServiceStatus newStatus)
        {
            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }

        public string GetSystemdUnitName()
        {
            return SystemdUnitName ?? $"vps-{Name.ToLower().Replace(" ", "-")}.service";
        }
    }
}
