#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Caddy reverse proxy global configuration
    /// </summary>
    public sealed class CaddyConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string AdminEmail { get; set; }

        public int AdminPort { get; set; } = 2019;

        public string AdminHost { get; set; } = "localhost";

        public bool EnableMetrics { get; set; } = true;

        public bool EnableLogOutput { get; set; } = true;

        public string LogLevel { get; set; } = "info";

        public string LogFormat { get; set; } = "{common_log}";

        public int HttpPort { get; set; } = 80;

        public int HttpsPort { get; set; } = 443;

        public bool AutoHttpsDisabled { get; set; }

        public string TlsPolicy { get; set; } = "clients";

        public int IdleTimeout { get; set; } = 120;

        public int ReadTimeout { get; set; } = 30;

        public int WriteTimeout { get; set; } = 30;

        public List<string> OnDemandTls { get; set; } = new();

        public string CertificateEmail { get; set; }

        public bool StrictSniRequired { get; set; }

        public Dictionary<string, string> CustomGlobals { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public void Validate()
        {
            if (AdminPort <= 0 || AdminPort > 65535)
                throw new ValidationException("Admin port must be between 1 and 65535");

            if (HttpPort <= 0 || HttpPort > 65535)
                throw new ValidationException("HTTP port must be between 1 and 65535");

            if (HttpsPort <= 0 || HttpsPort > 65535)
                throw new ValidationException("HTTPS port must be between 1 and 65535");

            if (IdleTimeout < 0 || ReadTimeout < 0 || WriteTimeout < 0)
                throw new ValidationException("Timeouts cannot be negative");
        }

        public void SetDefaultValues()
        {
            if (string.IsNullOrWhiteSpace(AdminEmail))
                AdminEmail = "admin@localhost";

            if (string.IsNullOrWhiteSpace(CertificateEmail))
                CertificateEmail = AdminEmail;
        }

        public string GenerateCaddyfileGlobals()
        {
            var lines = new List<string>();

            if (EnableMetrics)
                lines.Add("metrics");

            lines.Add($"admin {AdminHost}:{AdminPort}");

            if (AutoHttpsDisabled)
                lines.Add("auto_https off");

            lines.Add($"http_port {HttpPort}");
            lines.Add($"https_port {HttpsPort}");

            if (OnDemandTls.Count > 0)
                lines.Add($"on_demand_tls {{\n  ask http://localhost:2019/config/apps/http/servers\n}}");

            return string.Join("\n", lines);
        }
    }
}
