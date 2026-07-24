#nullable enable

using System;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Provides sanitization methods for template values to prevent injection attacks.
    /// </summary>
    public static class TemplateValueSanitizer
    {
        /// <summary>
        /// Sanitizes a string value for use in a Caddyfile.
        /// Rejects control characters, braces, and newlines.
        /// </summary>
        /// <param name="value">The value to sanitize.</param>
        /// <returns>The sanitized value.</returns>
        /// <exception cref="ArgumentException">Thrown when the value contains invalid characters.</exception>
        public static string SanitizeCaddyValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            foreach (char c in value)
            {
                if (char.IsControl(c) || c == '{' || c == '}' || c == '\n' || c == '\r')
                {
                    throw new ArgumentException($"Invalid character '{c}' in Caddyfile template value: {value}");
                }
            }
            return value;
        }
    }
}
