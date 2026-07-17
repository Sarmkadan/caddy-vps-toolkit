#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Validation helpers for SystemdUnitService and related types
    /// </summary>
    public static class SystemdUnitServiceValidation
    {
        /// <summary>
        /// Validates a SystemdUnitStatus instance
        /// </summary>
        /// <param name="status">The status to validate</param>
        /// <returns>List of validation errors; empty list if valid</returns>
        /// <exception cref="ArgumentNullException">Thrown if status is null</exception>
        public static IReadOnlyList<string> Validate(this SystemdUnitStatus status)
        {
            ArgumentNullException.ThrowIfNull(status);

            var errors = new List<string>();

            // Validate UnitName
            if (string.IsNullOrWhiteSpace(status.UnitName))
            {
                errors.Add("UnitName cannot be null or whitespace");
            }

            // Validate ActiveState
            if (string.IsNullOrWhiteSpace(status.ActiveState))
            {
                errors.Add("ActiveState cannot be null or whitespace");
            }
            else if (!status.ActiveState.Equals("active", StringComparison.OrdinalIgnoreCase) &&
                     !status.ActiveState.Equals("inactive", StringComparison.OrdinalIgnoreCase) &&
                     !status.ActiveState.Equals("activating", StringComparison.OrdinalIgnoreCase) &&
                     !status.ActiveState.Equals("deactivating", StringComparison.OrdinalIgnoreCase) &&
                     !status.ActiveState.Equals("failed", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"ActiveState '{status.ActiveState}' is not a valid systemd state");
            }

            // Validate Type
            if (string.IsNullOrWhiteSpace(status.Type))
            {
                errors.Add("Type cannot be null or whitespace");
            }
            else if (!status.Type.Equals("simple", StringComparison.OrdinalIgnoreCase) &&
                     !status.Type.Equals("forking", StringComparison.OrdinalIgnoreCase) &&
                     !status.Type.Equals("oneshot", StringComparison.OrdinalIgnoreCase) &&
                     !status.Type.Equals("dbus", StringComparison.OrdinalIgnoreCase) &&
                     !status.Type.Equals("notify", StringComparison.OrdinalIgnoreCase) &&
                     !status.Type.Equals("idle", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Type '{status.Type}' is not a valid systemd service type");
            }

            // Validate MainPid
            if (status.MainPid < 0)
            {
                errors.Add("MainPid cannot be negative");
            }

            // Validate TimeoutStartUsec - allow 0 as valid default value
            if (status.TimeoutStartUsec > 0 && status.TimeoutStartUsec < 1000000)
            {
                errors.Add("TimeoutStartUsec should be at least 1 second (1000000 microseconds) for proper timeout configuration");
            }

            return errors.AsReadOnly();
        }

        /// <summary>
        /// Checks if a SystemdUnitStatus instance is valid
        /// </summary>
        /// <param name="status">The status to check</param>
        /// <returns>True if valid; false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown if status is null</exception>
        public static bool IsValid(this SystemdUnitStatus status)
        {
            ArgumentNullException.ThrowIfNull(status);
            return status.Validate().Count is 0;
        }

        /// <summary>
        /// Ensures a SystemdUnitStatus instance is valid, throwing ArgumentException if not
        /// </summary>
        /// <param name="status">The status to validate</param>
        /// <exception cref="ArgumentNullException">Thrown if status is null</exception>
        /// <exception cref="ArgumentException">Thrown if status is not valid, containing all validation errors</exception>
        public static void EnsureValid(this SystemdUnitStatus status)
        {
            ArgumentNullException.ThrowIfNull(status);

            var errors = status.Validate();

            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    $"SystemdUnitStatus validation failed:{Environment.NewLine}- {
                    string.Join($"{Environment.NewLine}- ", errors)}");
            }
        }
    }
}
