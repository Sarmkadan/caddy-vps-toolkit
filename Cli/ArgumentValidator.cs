#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Cli
{
    /// <summary>
    /// Validates command-line arguments against command descriptors.
    /// Ensures required arguments are present and flags are recognized.
    /// </summary>
    public sealed class ArgumentValidator
    {
        public ValidationResult Validate(ArgumentParser parser, CommandDescriptor descriptor)
        {
            var errors = new List<string>();

            if (descriptor is null)
                return new ValidationResult { IsValid = false, Errors = new List<string> { "Command not found" } };

            // Check required arguments
            for (int i = 0; i < descriptor.RequiredArguments.Count; i++)
            {
                var argName = descriptor.RequiredArguments[i];
                var argValue = parser.GetPositional(i);
                if (string.IsNullOrEmpty(argValue))
                    errors.Add($"Missing required argument: {argName}");
            }

            // Validate required arguments count
            var positionals = parser.GetAllPositional();
            if (positionals.Count < descriptor.RequiredArguments.Count)
            {
                for (int i = positionals.Count; i < descriptor.RequiredArguments.Count; i++)
                    errors.Add($"Missing required argument: {descriptor.RequiredArguments[i]}");
            }

            // Check for unknown flags
            var providedFlags = parser.GetAllFlags();
            foreach (var flag in providedFlags)
            {
                if (!descriptor.OptionalFlags.Contains(flag))
                    errors.Add($"Unknown flag: --{flag}");
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Result of argument validation
    /// </summary>
    public sealed class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();

        public string GetErrorMessage()
        {
            return string.Join(Environment.NewLine, Errors);
        }
    }
}
