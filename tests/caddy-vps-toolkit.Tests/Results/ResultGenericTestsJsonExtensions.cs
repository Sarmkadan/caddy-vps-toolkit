#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Text.Json;
using CaddyVpsToolkit.Results;

namespace CaddyVpsToolkit.Tests.Results
{
    /// <summary>
    /// Provides JSON serialization and deserialization extensions for <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of data contained in the result.</typeparam>
    public static class ResultJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes a <see cref="Result{T}"/> instance to a JSON string.
        /// </summary>
        /// <typeparam name="T">The type of data in the result.</typeparam>
        /// <param name="value">The result instance to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson<T>(this Result<T> value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="Result{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of data in the result.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized result instance, or null if the JSON is invalid.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is invalid and cannot be deserialized.</exception>
        public static Result<T>? FromJson<T>(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            return JsonSerializer.Deserialize<Result<T>>(json, _jsonOptions);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="Result{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of data in the result.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized result instance, or null if deserialization failed.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson<T>(string json, out Result<T>? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                value = JsonSerializer.Deserialize<Result<T>>(json, _jsonOptions);
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
