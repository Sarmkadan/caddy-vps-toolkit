// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Systemd unit configuration for service management
    /// </summary>
    public class SystemdUnitConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ServiceId { get; set; }

        [Required]
        public string UnitName { get; set; }

        [Required]
        public string ExecStart { get; set; }

        public string ExecReload { get; set; }

        public string ExecStop { get; set; }

        public string User { get; set; } = "root";

        public string Group { get; set; } = "root";

        public RestartPolicy RestartPolicy { get; set; } = RestartPolicy.Always;

        public int RestartDelaySeconds { get; set; } = 10;

        public int StartLimitIntervalSec { get; set; } = 300;

        public int StartLimitBurst { get; set; } = 5;

        public string Type { get; set; } = "simple";

        public string StandardOutput { get; set; } = "journal";

        public string StandardError { get; set; } = "journal";

        public Dictionary<string, string> Environment { get; set; } = new();

        public List<string> EnvironmentFiles { get; set; } = new();

        public List<string> After { get; set; } = new() { "network.target" };

        public List<string> Wants { get; set; } = new();

        public string WorkingDirectory { get; set; }

        public bool LimitNoFile { get; set; }

        public int LimitNoFileValue { get; set; } = 65535;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(UnitName))
                throw new ValidationException("Unit name is required");

            if (string.IsNullOrWhiteSpace(ExecStart))
                throw new ValidationException("ExecStart is required");

            if (string.IsNullOrWhiteSpace(User))
                throw new ValidationException("User is required");

            if (RestartDelaySeconds < 0)
                throw new ValidationException("Restart delay cannot be negative");
        }

        public string GenerateSystemdContent()
        {
            var lines = new List<string>
            {
                "[Unit]",
                $"Description=Service unit for {UnitName}",
                "After=" + string.Join(" ", After),
            };

            if (Wants.Any())
                lines.Add("Wants=" + string.Join(" ", Wants));

            lines.Add("");
            lines.Add("[Service]");
            lines.Add($"Type={Type}");
            lines.Add($"User={User}");
            lines.Add($"Group={Group}");

            if (!string.IsNullOrWhiteSpace(WorkingDirectory))
                lines.Add($"WorkingDirectory={WorkingDirectory}");

            lines.Add($"ExecStart={ExecStart}");

            if (!string.IsNullOrWhiteSpace(ExecReload))
                lines.Add($"ExecReload={ExecReload}");

            if (!string.IsNullOrWhiteSpace(ExecStop))
                lines.Add($"ExecStop={ExecStop}");

            lines.Add($"Restart={RestartPolicy.ToString().ToLower()}");
            lines.Add($"RestartSec={RestartDelaySeconds}");
            lines.Add($"StandardOutput={StandardOutput}");
            lines.Add($"StandardError={StandardError}");
            lines.Add($"StartLimitIntervalSec={StartLimitIntervalSec}");
            lines.Add($"StartLimitBurst={StartLimitBurst}");

            foreach (var env in Environment)
                lines.Add($"Environment=\"{env.Key}={env.Value}\"");

            foreach (var envFile in EnvironmentFiles)
                lines.Add($"EnvironmentFile={envFile}");

            if (LimitNoFile)
                lines.Add($"LimitNOFILE={LimitNoFileValue}");

            lines.Add("");
            lines.Add("[Install]");
            lines.Add("WantedBy=multi-user.target");

            return string.Join("\n", lines);
        }
    }

    public enum RestartPolicy
    {
        No,
        Always,
        OnSuccess,
        OnFailure,
        OnAbnormal,
        OnWatchdog,
        OnExitStatus
    }
}
