#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Represents a validation finding with severity level and message.
    /// </summary>
    public sealed class ConfigurationFinding
    {
        /// <summary>
        /// Gets or sets the severity level of the finding (e.g., "error", "warning", "info").
        /// </summary>
        public string Severity { get; set; } = "error";

        /// <summary>
        /// Gets or sets the validation message describing the issue.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Returns a string representation of the finding.
        /// </summary>
        /// <returns>A formatted string with severity and message.</returns>
        public override string ToString()
        {
            return $"[{Severity.ToUpper()}] {Message}";
        }
    }
}
