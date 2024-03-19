#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;

namespace CaddyVpsToolkit.Events
{
    /// <summary>
    /// Provides System.Text.Json serialization and deserialization extensions for ServiceCreatedEventHandler
    /// </summary>
    public static class ServiceCreatedEventHandlerJsonExtensions
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Serializes the ServiceCreatedEventHandler to a JSON string
        /// </summary>
        /// <param name="value">The event handler to serialize</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability</param>
        /// <returns>A JSON string representation of the event handler</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
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
        /// Deserializes a JSON string to a ServiceCreatedEventHandler instance
        /// </summary>
        /// <param name="json">The JSON string to deserialize</param>
        /// <returns>A ServiceCreatedEventHandler instance, or null if deserialization fails</returns>
        /// <exception cref="ArgumentException">Thrown when json is null or empty</exception>
        public static ServiceCreatedEventHandler? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            return JsonSerializer.Deserialize<ServiceCreatedEventHandler>(json, _options);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a ServiceCreatedEventHandler instance
        /// </summary>
        /// <param name="json">The JSON string to deserialize</param>
        /// <param name="value">Receives the deserialized ServiceCreatedEventHandler instance if successful</param>
        /// <returns>True if deserialization succeeds; otherwise, false</returns>
        /// <exception cref="ArgumentException">Thrown when json is null or empty</exception>
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