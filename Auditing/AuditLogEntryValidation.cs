using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Auditing
{
    public static class AuditLogEntryValidation
    {
        public static IReadOnlyList<string> Validate(this AuditLogEntry value)
        {
            var problems = new List<string>();

            if (value == null)
            {
                problems.Add("AuditLogEntry instance cannot be null.");
                return problems;
            }

            if (string.IsNullOrWhiteSpace(value.Id))
            {
                problems.Add("Id cannot be null or whitespace.");
            }

            if (value.Timestamp == default)
            {
                problems.Add("Timestamp must be a valid DateTime (cannot be default).");
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

            return problems;
        }

        public static bool IsValid(this AuditLogEntry value)
        {
            return value.Validate().Count == 0;
        }

        public static void EnsureValid(this AuditLogEntry value)
        {
            var problems = value.Validate();
            if (problems.Count > 0)
            {
                throw new ArgumentException($"AuditLogEntry validation failed: {string.Join("; ", problems)}");
            }
        }
    }
}
