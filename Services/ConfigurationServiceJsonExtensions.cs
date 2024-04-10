#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;
using CaddyVpsToolkit.Data;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Provides System.Text.Json serialization extensions for <see cref="ConfigurationService"/>
    /// </summary>
    public static class ConfigurationServiceJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes the <see cref="ConfigurationService"/> to a JSON string.
        /// </summary>
        /// <param name="value">The configuration service to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the configuration service.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this ConfigurationService value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var config = value.GetAllAsync().GetAwaiter().GetResult();
            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(config, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="ConfigurationService"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A new <see cref="ConfigurationService"/> instance populated with the deserialized configuration.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
        public static ConfigurationService? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            var config = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(json, _jsonOptions);
            if (config is null)
            {
                return null;
            }

            var service = new ConfigurationService(new InMemoryConfigurationRepository(config));
            return service;
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="ConfigurationService"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized configuration service, or null if deserialization fails.</param>
        /// <returns>True if deserialization succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        public static bool TryFromJson(string json, out ConfigurationService? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

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

        /// <summary>
        /// In-memory implementation of <see cref="IConfigurationRepository"/> for deserialization.
        /// </summary>
        private sealed class InMemoryConfigurationRepository : IConfigurationRepository
        {
            private readonly System.Collections.Generic.Dictionary<string, string> _config;

            public InMemoryConfigurationRepository(System.Collections.Generic.Dictionary<string, string> config)
            {
                _config = config ?? throw new ArgumentNullException(nameof(config));
            }

            public Task<string> GetValueAsync(string key)
            {
                if (_config.TryGetValue(key, out var value))
                {
                    return Task.FromResult(value);
                }

                return Task.FromResult(string.Empty);
            }

            public Task SetValueAsync(string key, string value)
            {
                _config[key] = value;
                return Task.CompletedTask;
            }

            public Task<bool> DeleteAsync(string key)
            {
                return Task.FromResult(_config.Remove(key));
            }

            public Task<System.Collections.Generic.Dictionary<string, string>> GetAllAsync()
            {
                return Task.FromResult(new System.Collections.Generic.Dictionary<string, string>(_config));
            }
        }
    }
}