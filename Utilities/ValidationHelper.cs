// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Centralized validation helper for common validation scenarios.
    /// Returns validation results with detailed error messages.
    /// </summary>
    public class ValidationHelper
    {
        public static ValidationResult ValidatePort(int port)
        {
            var errors = new List<string>();

            if (port <= 0 || port > 65535)
                errors.Add($"Port must be between 1 and 65535, got: {port}");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }

        public static ValidationResult ValidateDomain(string domain)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(domain))
                errors.Add("Domain cannot be empty");
            else if (!Regex.IsMatch(domain, @"^([a-z0-9]([a-z0-9-]*[a-z0-9])?\.)+[a-z]{2,}$", RegexOptions.IgnoreCase))
                errors.Add($"Invalid domain format: {domain}");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }

        public static ValidationResult ValidateFilePath(string path)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(path))
                errors.Add("Path cannot be empty");
            else
            {
                try
                {
                    var fullPath = System.IO.Path.GetFullPath(path);
                }
                catch
                {
                    errors.Add($"Invalid file path: {path}");
                }
            }

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }

        public static ValidationResult ValidateServiceName(string serviceName)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(serviceName))
                errors.Add("Service name cannot be empty");
            else if (serviceName.Length < 3)
                errors.Add("Service name must be at least 3 characters");
            else if (!Regex.IsMatch(serviceName, @"^[a-z0-9][a-z0-9-]*[a-z0-9]$", RegexOptions.IgnoreCase))
                errors.Add("Service name can only contain alphanumeric characters and hyphens");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }

        public static ValidationResult ValidateRange(int value, int min, int max, string fieldName)
        {
            var errors = new List<string>();

            if (value < min || value > max)
                errors.Add($"{fieldName} must be between {min} and {max}, got: {value}");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }

        public static ValidationResult ValidateNotNull<T>(T value, string fieldName)
        {
            var errors = new List<string>();

            if (value == null)
                errors.Add($"{fieldName} cannot be null");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }

        public static ValidationResult ValidateNotEmpty(string value, string fieldName)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(value))
                errors.Add($"{fieldName} cannot be empty");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }

        public static ValidationResult Combine(params ValidationResult[] results)
        {
            var allErrors = new List<string>();

            foreach (var result in results.Where(r => !r.IsValid))
                allErrors.AddRange(result.Errors);

            return new ValidationResult { IsValid = allErrors.Count == 0, Errors = allErrors };
        }
    }

    /// <summary>
    /// Validation result container
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();

        public string GetErrorMessage()
        {
            return string.Join("; ", Errors);
        }

        public static ValidationResult Success() => new() { IsValid = true };
        public static ValidationResult Failure(params string[] errors)
            => new() { IsValid = false, Errors = errors.ToList() };
    }
}
