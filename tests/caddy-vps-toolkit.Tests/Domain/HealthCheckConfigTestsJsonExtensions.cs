#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;
using CaddyVpsToolkit.Tests.Domain;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="HealthCheckConfigTests"/> instances.
/// </summary>
namespace CaddyVpsToolkit.Tests.Domain
{
    /// <summary>
    /// JSON serialization and deserialization helpers for <see cref="HealthCheckConfigTests"/>.
    /// </summary>
    public static class HealthCheckConfigTestsJsonExtensions
    {
        /// <summary>
        /// Private cached JsonSerializerOptions for camelCase serialization.
        /// </summary>
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes a <see cref="HealthCheckConfigTests"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The <see cref="HealthCheckConfigTests"/> instance to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the <see cref="HealthCheckConfigTests"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this HealthCheckConfigTests value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions)
                {
                    WriteIndented = true
                }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="HealthCheckConfigTests"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A <see cref="HealthCheckConfigTests"/> instance if successful; otherwise, null.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
        public static HealthCheckConfigTests? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            return JsonSerializer.Deserialize<HealthCheckConfigTests>(json, _jsonOptions);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="HealthCheckConfigTests"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized <see cref="HealthCheckConfigTests"/> instance if successful; otherwise, null.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson(string json, out HealthCheckConfigTests? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                value = JsonSerializer.Deserialize<HealthCheckConfigTests>(json, _jsonOptions);
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