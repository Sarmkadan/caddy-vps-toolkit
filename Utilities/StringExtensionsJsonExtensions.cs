#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Text.Json;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Provides JSON serialization and deserialization extension methods for the <see cref="StringExtensions"/> type.
    /// </summary>
    public static partial class StringExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes information about the <see cref="StringExtensions"/> type to a JSON string representation.
        /// </summary>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the StringExtensions type metadata.</returns>
        public static string ToJson(bool indented = false)
        {
            var result = new
            {
                Type = nameof(StringExtensions),
                Methods = new[] { "IsNullOrWhiteSpace", "ToTitleCase", "ToKebabCase", "ToCamelCase", "Truncate", "IsValidEmail", "IsValidUrl", "IsNumeric", "Repeat", "EscapeShell", "SafeSubstring", "StartsWithAny" },
                Properties = Array.Empty<string>()
            };

            return JsonSerializer.Serialize(result, indented ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true } : _jsonOptions);
        }

        /// <summary>
        /// Returns a placeholder value representing the StringExtensions type.
        /// </summary>
        /// <param name="json">The JSON string (unused, for API consistency).</param>
        /// <returns>Always returns a new object reference as a placeholder.</returns>
        /// <exception cref="JsonException">Never thrown.</exception>
        public static object? FromJson(string json)
        {
            // Since StringExtensions is a static class, return a placeholder object
            return new object();
        }

        /// <summary>
        /// Attempts to process a JSON string as a placeholder for StringExtensions operations.
        /// </summary>
        /// <param name="json">The JSON string to process.</param>
        /// <param name="value">Receives an object if successful.</param>
        /// <returns>Always returns true since there's no actual deserialization to fail.</returns>
        public static bool TryFromJson(string json, out object? value)
        {
            value = new object();
            return true;
        }
    }
}
