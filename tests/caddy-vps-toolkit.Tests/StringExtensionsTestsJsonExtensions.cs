#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Tests
{
    /// <summary>
    /// Provides JSON serialization and deserialization extensions for <see cref="StringExtensionsTests"/> instances.
    /// </summary>
    /// <remarks>
    /// This class offers methods to serialize test fixture instances to JSON and deserialize them back,
    /// enabling test state persistence and transfer scenarios.
    /// </remarks>
    public static class StringExtensionsTestsJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes the <see cref="StringExtensionsTests"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The <see cref="StringExtensionsTests"/> instance to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the <see cref="StringExtensionsTests"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static string ToJson(this StringExtensionsTests value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="StringExtensionsTests"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized <see cref="StringExtensionsTests"/> instance, or <see langword="null"/> if the JSON is invalid.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <see langword="null"/>, empty, or whitespace.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is malformed and cannot be deserialized.</exception>
        public static StringExtensionsTests? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            return JsonSerializer.Deserialize<StringExtensionsTests>(json, _jsonOptions);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="StringExtensionsTests"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">
        /// When this method returns, contains the deserialized <see cref="StringExtensionsTests"/> instance if deserialization succeeded,
        /// or <see langword="null"/> if deserialization failed.
        /// </param>
        /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <see langword="null"/>, empty, or whitespace.</exception>
        public static bool TryFromJson(string json, out StringExtensionsTests? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                value = JsonSerializer.Deserialize<StringExtensionsTests>(json, _jsonOptions);
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