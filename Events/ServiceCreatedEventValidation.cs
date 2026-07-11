#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using CaddyVpsToolkit.Core;

namespace CaddyVpsToolkit.Events
{
    /// <summary>
    /// Provides validation helpers for <see cref="ServiceCreatedEvent"/> instances
    /// </summary>
    public static class ServiceCreatedEventValidation
    {
        /// <summary>
        /// Validates a <see cref="ServiceCreatedEvent"/> instance
        /// </summary>
        /// <param name="value">The event to validate</param>
        /// <returns>A list of validation errors; empty if valid</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
        public static IReadOnlyList<string> Validate(this ServiceCreatedEvent value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = new List<string>();

            // Validate ServiceName
            if (string.IsNullOrWhiteSpace(value.ServiceName))
            {
                errors.Add($"ServiceName is required and cannot be empty or whitespace.");
            }
            else if (value.ServiceName.Length > 256)
            {
                errors.Add($"ServiceName must be 256 characters or less. Current length: {value.ServiceName.Length}.");
            }

            // ServiceType is an enum - all values are valid, no validation needed

            // Validate Port
            if (value.Port <= 0)
            {
                errors.Add("Port must be a positive integer greater than 0.");
            }
            else if (value.Port > 65535)
            {
                errors.Add("Port must be 65535 or less.");
            }

            // Validate ExecutablePath
            if (string.IsNullOrWhiteSpace(value.ExecutablePath))
            {
                errors.Add("ExecutablePath is required and cannot be empty or whitespace.");
            }
            else if (value.ExecutablePath.Length > 4096)
            {
                errors.Add($"ExecutablePath must be 4096 characters or less. Current length: {value.ExecutablePath.Length}.");
            }

            return errors.AsReadOnly();
        }

        /// <summary>
        /// Determines whether a <see cref="ServiceCreatedEvent"/> instance is valid
        /// </summary>
        /// <param name="value">The event to check</param>
        /// <returns>True if valid; otherwise false</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
        public static bool IsValid(this ServiceCreatedEvent value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return value.Validate().Count == 0;
        }

        /// <summary>
        /// Ensures that a <see cref="ServiceCreatedEvent"/> instance is valid, throwing an exception if not
        /// </summary>
        /// <param name="value">The event to validate</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid</exception>
        public static void EnsureValid(this ServiceCreatedEvent value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = value.Validate();
            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    $"ServiceCreatedEvent is not valid. Validation errors: {string.Join(" ", errors)}");
            }
        }
    }
}
