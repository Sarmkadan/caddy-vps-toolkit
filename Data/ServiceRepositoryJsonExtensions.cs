#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Data
{
    /// <summary>
    /// Provides System.Text.Json serialization extensions for <see cref="ServiceRepository"/> and <see cref="ManagedService"/>.
    /// </summary>
    public static class ServiceRepositoryJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        /// <summary>
        /// Serializes a <see cref="ManagedService"/> to a JSON string.
        /// </summary>
        /// <param name="value">The service to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>A JSON string representation of the service.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this ManagedService value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="ManagedService"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized service, or null if the JSON is null or empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
        public static ManagedService? FromJson(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<ManagedService>(json, _jsonOptions);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="ManagedService"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized service, or null on failure.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        public static bool TryFromJson(string json, out ManagedService? value)
        {
            value = default;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                value = JsonSerializer.Deserialize<ManagedService>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Serializes a collection of services to a JSON array string.
        /// </summary>
        /// <param name="values">The services to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>A JSON array string representation of the services.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
        public static string ToJson(this IEnumerable<ManagedService> values, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(values);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(values, options);
        }

        /// <summary>
        /// Deserializes a JSON array string to a list of <see cref="ManagedService"/>.
        /// </summary>
        /// <param name="json">The JSON array string to deserialize.</param>
        /// <returns>A list of deserialized services.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
        public static IReadOnlyList<ManagedService> FromJsonToList(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            return string.IsNullOrWhiteSpace(json)
                ? Array.Empty<ManagedService>()
                : JsonSerializer.Deserialize<IReadOnlyList<ManagedService>>(json, _jsonOptions)
                    ?? Array.Empty<ManagedService>();
        }

        /// <summary>
        /// Attempts to deserialize a JSON array string to a list of <see cref="ManagedService"/>.
        /// </summary>
        /// <param name="json">The JSON array string to deserialize.</param>
        /// <param name="values">Receives the deserialized services, or an empty list on failure.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        public static bool TryFromJsonToList(string json, out IReadOnlyList<ManagedService> values)
        {
            values = Array.Empty<ManagedService>();

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                var result = JsonSerializer.Deserialize<IReadOnlyList<ManagedService>>(json, _jsonOptions);
                values = result ?? Array.Empty<ManagedService>();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}