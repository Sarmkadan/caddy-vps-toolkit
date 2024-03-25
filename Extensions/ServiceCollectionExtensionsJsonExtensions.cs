#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace CaddyVpsToolkit.Extensions
{
    /// <summary>
    /// Provides System.Text.Json serialization extensions for InfrastructureOptions.
    /// InfrastructureOptions is the configuration class used by ServiceCollectionExtensions.
    /// </summary>
    public static class ServiceCollectionExtensionsJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        /// <summary>
        /// Serializes the InfrastructureOptions instance to a JSON string.
        /// </summary>
        /// <param name="value">The InfrastructureOptions instance to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the InfrastructureOptions instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this InfrastructureOptions value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
                : _jsonSerializerOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to an InfrastructureOptions instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>An InfrastructureOptions instance if deserialization succeeds; otherwise, null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static InfrastructureOptions? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                return JsonSerializer.Deserialize<InfrastructureOptions>(json, _jsonSerializerOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to an InfrastructureOptions instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized InfrastructureOptions instance if successful.</param>
        /// <returns>True if deserialization succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson(string json, out InfrastructureOptions? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                value = JsonSerializer.Deserialize<InfrastructureOptions>(json, _jsonSerializerOptions);
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