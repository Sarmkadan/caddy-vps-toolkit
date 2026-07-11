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
        /// Parses a JSON string produced by <see cref="ToJson"/> (or any valid JSON document)
        /// into a <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="json">The JSON string to parse.</param>
        /// <returns>The parsed <see cref="JsonElement"/> boxed as <see cref="object"/>, or null when the document is the JSON null literal.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        /// <exception cref="JsonException">Thrown when <paramref name="json"/> is not valid JSON.</exception>
        public static object? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            var element = JsonSerializer.Deserialize<JsonElement>(json, _jsonOptions);
            return element.ValueKind == JsonValueKind.Null ? null : element;
        }

        /// <summary>
        /// Attempts to parse a JSON string into a <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="json">The JSON string to parse.</param>
        /// <param name="value">Receives the parsed element on success, or null on failure.</param>
        /// <returns>True if the input is valid JSON; otherwise, false.</returns>
        public static bool TryFromJson(string json, out object? value)
        {
            if (string.IsNullOrEmpty(json))
            {
                value = null;
                return false;
            }

            try
            {
                value = FromJson(json);
                return true;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }
    }
}
