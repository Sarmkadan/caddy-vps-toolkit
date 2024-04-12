#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Events
{
    /// <summary>
    /// Provides validation helpers for <see cref="ServiceCreatedEventHandler"/> instances
    /// </summary>
    public static class ServiceCreatedEventHandlerValidation
    {
        /// <summary>
        /// Validates a <see cref="ServiceCreatedEventHandler"/> instance
        /// </summary>
        /// <param name="value">The handler to validate</param>
        /// <returns>A list of validation errors; empty if valid</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
        public static IReadOnlyList<string> Validate(this ServiceCreatedEventHandler value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = new List<string>();

            // Validate logger dependency
            var loggerField = typeof(ServiceCreatedEventHandler).GetField(
                "_logger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (loggerField == null)
            {
                errors.Add("Logger field '_logger' not found in ServiceCreatedEventHandler.");
            }

            // Validate webhook handler dependency
            var webhookHandlerField = typeof(ServiceCreatedEventHandler).GetField(
                "_webhookHandler",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (webhookHandlerField == null)
            {
                errors.Add("Webhook handler field '_webhookHandler' not found in ServiceCreatedEventHandler.");
            }

            return errors.AsReadOnly();
        }

        /// <summary>
        /// Determines whether a <see cref="ServiceCreatedEventHandler"/> instance is valid
        /// </summary>
        /// <param name="value">The handler to check</param>
        /// <returns>True if valid; otherwise false</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
        public static bool IsValid(this ServiceCreatedEventHandler value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return value.Validate().Count == 0;
        }

        /// <summary>
        /// Ensures that a <see cref="ServiceCreatedEventHandler"/> instance is valid, throwing an exception if not
        /// </summary>
        /// <param name="value">The handler to validate</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid</exception>
        public static void EnsureValid(this ServiceCreatedEventHandler value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = value.Validate();
            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    $"ServiceCreatedEventHandler is not valid. Validation errors: {string.Join(" ", errors)}");
            }
        }
    }
}
