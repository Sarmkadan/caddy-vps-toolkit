#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Provides JSON serialization and deserialization utilities for process execution configuration.
    /// These helpers work with the configuration and result types used by <see cref="ProcessUtilities"/>.
    /// </summary>
    public static class ProcessUtilitiesJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Serializes process execution configuration to a JSON string.
        /// </summary>
        /// <param name="timeoutMs">The timeout in milliseconds for process execution.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the process execution configuration.</returns>
        public static string ToJson(int timeoutMs = 30000, bool indented = false)
        {
            var config = new ProcessExecutionConfig { TimeoutMs = timeoutMs };

            var options = indented
                ? new JsonSerializerOptions(_jsonSerializerOptions)
                {
                    WriteIndented = true
                }
                : _jsonSerializerOptions;

            return JsonSerializer.Serialize(config, options);
        }

        /// <summary>
        /// Deserializes a JSON string to process execution configuration.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized process execution configuration, or null if deserialization fails.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static ProcessExecutionConfig? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                return JsonSerializer.Deserialize<ProcessExecutionConfig>(json, _jsonSerializerOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to process execution configuration.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="config">Receives the deserialized configuration if successful.</param>
        /// <returns>True if deserialization succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson(string json, out ProcessExecutionConfig? config)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                config = JsonSerializer.Deserialize<ProcessExecutionConfig>(json, _jsonSerializerOptions);
                return true;
            }
            catch (JsonException)
            {
                config = null;
                return false;
            }
        }
    }

    /// <summary>
    /// Configuration for process execution with timeout.
    /// This is the serializable configuration used by ProcessUtilities operations.
    /// </summary>
    public sealed class ProcessExecutionConfig
    {
        /// <summary>
        /// Gets or sets the timeout in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;
    }
}
