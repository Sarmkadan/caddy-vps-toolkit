#nullable enable
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
    public sealed class ValidationHelper
    {
        private const int MinPort = 1;
        private const int MaxPort = 65535;
        private const string DomainPattern = @"^([a-z0-9]([a-z0-9-]*[a-z0-9])?\.)+[a-z]{2,}$";
        private const int MinServiceNameLength = 3;
        private const string ServiceNamePattern = @"^[a-z0-9][a-z0-9-]*[a-z0-9]$";

        public static ValidationResult ValidatePort(int port)
        {
            var errors = new List<string>();

            if (port < MinPort || port > MaxPort)
                errors.Add($"Port must be between {MinPort} and {MaxPort}, got: {port}");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }

        public static ValidationResult ValidateDomain(string domain)
        {
            ArgumentException.ThrowIfNullOrEmpty(domain);
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(domain))
                errors.Add("Domain cannot be empty");
            else if (!Regex.IsMatch(domain, DomainPattern, RegexOptions.IgnoreCase))
                errors.Add($"Invalid domain format: {domain}");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }

        public static ValidationResult ValidateFilePath(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
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
            ArgumentException.ThrowIfNullOrEmpty(serviceName);
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(serviceName))
                errors.Add("Service name cannot be empty");
            else if (serviceName.Length < MinServiceNameLength)
                errors.Add($"Service name must be at least {MinServiceNameLength} characters");
            else if (!Regex.IsMatch(serviceName, ServiceNamePattern, RegexOptions.IgnoreCase))
                errors.Add("Service name can only contain alphanumeric characters and hyphens");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }

        public static ValidationResult ValidateRange(int value, int min, int max, string fieldName)
        {
            ArgumentException.ThrowIfNullOrEmpty(fieldName);
            var errors = new List<string>();

            if (value < min || value > max)
                errors.Add($"{fieldName} must be between {min} and {max}, got: {value}");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }

        public static ValidationResult ValidateNotNull<T>(T value, string fieldName)
        {
            ArgumentException.ThrowIfNullOrEmpty(fieldName);
            var errors = new List<string>();

            if (value is null)
                errors.Add($"{fieldName} cannot be null");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }

        public static ValidationResult ValidateNotEmpty(string value, string fieldName)
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            ArgumentException.ThrowIfNullOrEmpty(fieldName);
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(value))
                errors.Add($"{fieldName} cannot be empty");

            return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
        }

        public static ValidationResult Combine(params ValidationResult[] results)
        {
            ArgumentNullException.ThrowIfNull(results);
            var allErrors = new List<string>();

            foreach (var result in results.Where(r => !r.IsValid))
                allErrors.AddRange(result.Errors);

            return new ValidationResult { IsValid = allErrors.Count == 0, Errors = allErrors };
        }
    }

    /// <summary>
    /// Validation result container
    /// </summary>
    public sealed class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();

        public string GetErrorMessage()
        {
            return string.Join("; ", Errors);
        }

        public override string ToString()
        {
            return $"ValidationResult {{ IsValid = {IsValid}, Errors = [{string.Join(", ", Errors)}] }}";
        }

        public static ValidationResult Success() => new() { IsValid = true };
        public static ValidationResult Failure(params string[] errors)
            => new() { IsValid = false, Errors = errors.ToList() };
    }
}
