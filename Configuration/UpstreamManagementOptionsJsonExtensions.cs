// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===========================================================================

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace CaddyVpsToolkit.Configuration
{
    /// <summary>
    /// Provides extension methods for serializing and deserializing <see cref="UpstreamManagementOptions"/>
    /// instances to and from JSON using System.Text.Json.
    /// </summary>
    public static class UpstreamManagementOptionsJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };

        /// <summary>
        /// Serializes the <see cref="UpstreamManagementOptions"/> value to a JSON string.
        /// </summary>
        /// <param name="value">The options instance to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the options.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static string ToJson(this UpstreamManagementOptions value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string into an <see cref="UpstreamManagementOptions"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized options instance, or <see langword="null"/> if the JSON is empty or whitespace.</returns>
        /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
        public static UpstreamManagementOptions? FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<UpstreamManagementOptions>(json, _jsonOptions);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string into an <see cref="UpstreamManagementOptions"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized options instance if successful; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
        public static bool TryFromJson(string json, out UpstreamManagementOptions? value)
        {
            value = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                return true;
            }

            try
            {
                value = JsonSerializer.Deserialize<UpstreamManagementOptions>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}