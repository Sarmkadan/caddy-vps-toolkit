#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CaddyVpsToolkit.Events
{
    /// <summary>
    /// Provides JSON serialization and deserialization extensions for <see cref="ServiceCreatedEventHandler"/> using System.Text.Json
    /// </summary>
    /// <remarks>
    /// This class uses camelCase property naming policy for JSON serialization to match JavaScript/TypeScript conventions.
    /// </remarks>
    public static class ServiceCreatedEventHandlerJsonExtensions
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Serializes the <see cref="ServiceCreatedEventHandler"/> to a JSON string using camelCase property naming
        /// </summary>
        /// <param name="value">The event handler to serialize. Cannot be null.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability</param>
        /// <returns>A JSON string representation of the event handler</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
        public static string ToJson(this ServiceCreatedEventHandler value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (indented)
            {
                var indentedOptions = new JsonSerializerOptions(_options)
                {
                    WriteIndented = true
                };
                return JsonSerializer.Serialize(value, indentedOptions);
            }

            return JsonSerializer.Serialize(value, _options);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="ServiceCreatedEventHandler"/> instance
        /// </summary>
        /// <param name="json">The JSON string to deserialize. Cannot be null or empty.</param>
        /// <returns>A <see cref="ServiceCreatedEventHandler"/> instance, or null if deserialization fails</returns>
        /// <exception cref="ArgumentException"><paramref name="json"/> is <see langword="null"/> or <see cref="string.Empty"/></exception>
        public static ServiceCreatedEventHandler? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            return JsonSerializer.Deserialize<ServiceCreatedEventHandler>(json, _options);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="ServiceCreatedEventHandler"/> instance
        /// </summary>
        /// <param name="json">The JSON string to deserialize. Cannot be null or empty.</param>
        /// <param name="value">Receives the deserialized <see cref="ServiceCreatedEventHandler"/> instance if successful</param>
        /// <returns>True if deserialization succeeds; otherwise, false</returns>
        /// <exception cref="ArgumentException"><paramref name="json"/> is <see langword="null"/> or <see cref="string.Empty"/></exception>
        public static bool TryFromJson(string json, out ServiceCreatedEventHandler? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                value = JsonSerializer.Deserialize<ServiceCreatedEventHandler>(json, _options);
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