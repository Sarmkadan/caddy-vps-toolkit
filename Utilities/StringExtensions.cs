// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Extension methods for string manipulation and validation.
    /// Improves code readability by adding domain-specific string operations.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Check if string is null, empty, or whitespace
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Convert string to title case (first letter uppercase)
        /// </summary>
        public static string ToTitleCase(this string value)
        {
            if (value.IsNullOrWhiteSpace())
                return value;

            return char.ToUpper(value[0]) + value.Substring(1).ToLower();
        }

        /// <summary>
        /// Convert camelCase to kebab-case
        /// </summary>
        public static string ToKebabCase(this string value)
        {
            if (value.IsNullOrWhiteSpace())
                return value;

            return Regex.Replace(value, "(?<!^)(?=[A-Z])", "-").ToLower();
        }

        /// <summary>
        /// Convert kebab-case to camelCase
        /// </summary>
        public static string ToCamelCase(this string value)
        {
            if (value.IsNullOrWhiteSpace())
                return value;

            var parts = value.Split('-');
            if (parts.Length == 1)
                return value;

            return parts[0].ToLower() + string.Concat(parts.Skip(1).Select(p => p.ToTitleCase()));
        }

        /// <summary>
        /// Truncate string to max length with ellipsis
        /// </summary>
        public static string Truncate(this string value, int maxLength, string suffix = "...")
        {
            if (value.IsNullOrWhiteSpace() || value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength - suffix.Length) + suffix;
        }

        /// <summary>
        /// Check if string is valid email format
        /// </summary>
        public static bool IsValidEmail(this string value)
        {
            if (value.IsNullOrWhiteSpace())
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(value);
                return addr.Address == value;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if string is valid URL format
        /// </summary>
        public static bool IsValidUrl(this string value)
        {
            return !value.IsNullOrWhiteSpace() &&
                   Uri.TryCreate(value, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Check if string contains only digits
        /// </summary>
        public static bool IsNumeric(this string value)
        {
            return !value.IsNullOrWhiteSpace() && value.All(char.IsDigit);
        }

        /// <summary>
        /// Repeat string multiple times
        /// </summary>
        public static string Repeat(this string value, int count)
        {
            if (count <= 0 || value.IsNullOrWhiteSpace())
                return string.Empty;

            return string.Concat(Enumerable.Repeat(value, count));
        }

        /// <summary>
        /// Escape string for use in shell commands
        /// </summary>
        public static string EscapeShell(this string value)
        {
            if (value.IsNullOrWhiteSpace())
                return value;

            // Wrap in single quotes and escape any single quotes within
            return "'" + value.Replace("'", "'\\''") + "'";
        }

        /// <summary>
        /// Safe substring that won't throw if indices are invalid
        /// </summary>
        public static string SafeSubstring(this string value, int startIndex, int length)
        {
            if (value.IsNullOrWhiteSpace() || startIndex >= value.Length)
                return string.Empty;

            int actualLength = Math.Min(length, value.Length - startIndex);
            return value.Substring(startIndex, actualLength);
        }

        /// <summary>
        /// Check if string starts with any of the provided prefixes
        /// </summary>
        public static bool StartsWithAny(this string value, params string[] prefixes)
        {
            return prefixes.Any(p => value.StartsWith(p));
        }
    }
}
