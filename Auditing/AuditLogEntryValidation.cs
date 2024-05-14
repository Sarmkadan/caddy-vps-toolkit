using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Auditing
{
    /// <summary>
    /// Provides validation methods for <see cref="AuditLogEntry"/> instances.
    /// </summary>
    public static class AuditLogEntryValidation
    {
        /// <summary>
        /// Validates an <see cref="AuditLogEntry"/> instance and returns a list of validation problems.
        /// </summary>
        /// <param name="value">The audit log entry to validate.</param>
        /// <returns>A read-only list of validation error messages. Empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this AuditLogEntry value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            if (string.IsNullOrWhiteSpace(value.Id))
            {
                problems.Add("Id cannot be null or whitespace.");
            }

            if (value.Timestamp == default || value.Timestamp == DateTime.MinValue)
            {
                problems.Add("Timestamp must be a valid DateTime (cannot be default or MinValue).");
            }

            if (string.IsNullOrWhiteSpace(value.Action))
            {
                problems.Add("Action cannot be null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(value.Actor))
            {
                problems.Add("Actor cannot be null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(value.Target))
            {
                problems.Add("Target cannot be null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(value.Result))
            {
                problems.Add("Result cannot be null or whitespace.");
            }

            if (value.Details != null)
            {
                foreach (var key in value.Details.Keys)
                {
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        problems.Add("Details dictionary contains a null or whitespace key.");
                        break;
                    }
                }
            }

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the specified <see cref="AuditLogEntry"/> is valid.
        /// </summary>
        /// <param name="value">The audit log entry to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(this AuditLogEntry value) => value.Validate().Count == 0;

        /// <summary>
        /// Validates an <see cref="AuditLogEntry"/> and throws an exception if it is invalid.
        /// </summary>
        /// <param name="value">The audit log entry to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the audit log entry is invalid.</exception>
        public static void EnsureValid(this AuditLogEntry value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();
            if (problems.Count > 0)
            {
                throw new ArgumentException($"AuditLogEntry validation failed: {string.Join("; ", problems)}", nameof(value));
            }
        }
    }
}