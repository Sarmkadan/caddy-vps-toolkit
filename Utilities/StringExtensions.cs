// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Extension methods for string manipulation and validation.
    /// Improves code readability by adding domain-specific string operations.
    /// </summary>
    public static class StringExtensions
    {
        // Compiled once; avoids per-call regex compilation overhead.
        private static readonly Regex _kebabCaseRegex = new(
            "(?<!^)(?=[A-Z])", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

        // SearchValues<char> lets the JIT use SIMD-accelerated scanning for IsNumeric.
        private static readonly SearchValues<char> _digitChars =
            SearchValues.Create("0123456789");

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
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return string.Create(value.Length, value, static (span, src) =>
            {
                src.AsSpan().CopyTo(span);
                span[0] = char.ToUpper(span[0]);
                for (int i = 1; i < span.Length; i++)
                    span[i] = char.ToLower(span[i]);
            });
        }

        /// <summary>
        /// Convert camelCase to kebab-case
        /// </summary>
        public static string ToKebabCase(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return _kebabCaseRegex.Replace(value, "-").ToLower();
        }

        /// <summary>
        /// Convert kebab-case to camelCase
        /// </summary>
        public static string ToCamelCase(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
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
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength - suffix.Length) + suffix;
        }

        /// <summary>
        /// Check if string is valid email format
        /// </summary>
        public static bool IsValidEmail(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
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
            return !string.IsNullOrWhiteSpace(value) &&
                   Uri.TryCreate(value, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Check if string contains only digits
        /// </summary>
        public static bool IsNumeric(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            // ContainsAnyExcept with pre-built SearchValues uses SIMD on supported hardware.
            return !value.AsSpan().ContainsAnyExcept(_digitChars);
        }

        /// <summary>
        /// Repeat string multiple times
        /// </summary>
        public static string Repeat(this string value, int count)
        {
            if (count <= 0 || string.IsNullOrWhiteSpace(value))
                return string.Empty;

            // string.Create allocates exactly once; no intermediate arrays or LINQ.
            return string.Create(value.Length * count, (value, count), static (span, state) =>
            {
                var (str, cnt) = state;
                var src = str.AsSpan();
                for (int i = 0; i < cnt; i++)
                    src.CopyTo(span.Slice(i * str.Length));
            });
        }

        /// <summary>
        /// Escape string for use in shell commands
        /// </summary>
        public static string EscapeShell(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return "'" + value.Replace("'", "'\\''") + "'";
        }

        /// <summary>
        /// Safe substring that won't throw if indices are invalid
        /// </summary>
        public static string SafeSubstring(this string value, int startIndex, int length)
        {
            if (string.IsNullOrWhiteSpace(value) || startIndex >= value.Length)
                return string.Empty;

            int actualLength = Math.Min(length, value.Length - startIndex);
            return value.Substring(startIndex, actualLength);
        }

        /// <summary>
        /// Check if string starts with any of the provided prefixes
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWithAny(this string value, params string[] prefixes)
        {
            if (value is null || prefixes is null || prefixes.Length == 0) return false;
            var span = value.AsSpan();
            // Span.StartsWith avoids temporary string allocation for each prefix test.
            foreach (var prefix in prefixes)
            {
                if (prefix is not null && span.StartsWith(prefix.AsSpan(), StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
