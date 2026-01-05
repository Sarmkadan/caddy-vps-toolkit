// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.ComponentModel.DataAnnotations;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Port configuration for service exposure
    /// </summary>
    public class ServicePort
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ServiceId { get; set; }

        [Required]
        [Range(1, 65535)]
        public int InternalPort { get; set; }

        [Required]
        [Range(1, 65535)]
        public int ExternalPort { get; set; }

        [Required]
        public PortProtocol Protocol { get; set; } = PortProtocol.Tcp;

        public string Description { get; set; }

        public bool IsPublic { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public void Validate()
        {
            if (InternalPort < 1 || InternalPort > 65535)
                throw new ValidationException("Internal port must be between 1 and 65535");

            if (ExternalPort < 1 || ExternalPort > 65535)
                throw new ValidationException("External port must be between 1 and 65535");

            if (ExternalPort < 1024 && !IsPublic)
                throw new ValidationException("Privileged ports (< 1024) cannot be non-public");
        }

        public string GetPortMapping()
        {
            return $"{InternalPort}:{ExternalPort}/{Protocol.ToString().ToLower()}";
        }
    }

    public enum PortProtocol
    {
        Tcp,
        Udp
    }
}
