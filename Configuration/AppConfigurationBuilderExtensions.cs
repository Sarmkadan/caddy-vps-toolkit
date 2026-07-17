#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace CaddyVpsToolkit.Configuration
{
    /// <summary>
    /// Extension methods for <see cref="AppConfigurationBuilder"/> to provide additional configuration conveniences.
    /// </summary>
    public static class AppConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds configuration from a JSON file at the specified path, with validation.
        /// </summary>
        /// <param name="builder">The configuration builder instance.</param>
        /// <param name="path">Path to the JSON configuration file.</param>
        /// <param name="required">Whether the file is required to exist.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">File not found when <paramref name="required"/> is true.</exception>
        public static AppConfigurationBuilder WithJsonFile(this AppConfigurationBuilder builder, string path, bool required = true)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrEmpty(path);

            if (required && !System.IO.File.Exists(path))
            {
                throw new System.IO.FileNotFoundException($"Required configuration file not found: {path}");
            }

            return builder.WithJsonFile(path);
        }

        /// <summary>
        /// Adds configuration from environment variables with a custom prefix filter.
        /// </summary>
        /// <param name="builder">The configuration builder instance.</param>
        /// <param name="prefix">Environment variable prefix to filter by (e.g., "APP_").</param>
        /// <param name="includeEmpty">Whether to include environment variables with empty values.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="prefix"/> is null.</exception>
        public static AppConfigurationBuilder WithEnvironmentVariables(
            this AppConfigurationBuilder builder,
            string prefix = "",
            bool includeEmpty = false)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(prefix);

            var existingConfig = builder.Build().GetAll();
            var newConfig = new Dictionary<string, object>(StringComparer.Ordinal);

            foreach (var envVar in Environment.GetEnvironmentVariables().Keys)
            {
                var key = envVar.ToString();
                if (string.IsNullOrEmpty(prefix) || key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var value = Environment.GetEnvironmentVariable(key);
                    if (includeEmpty || !string.IsNullOrEmpty(value))
                    {
                        newConfig[key] = value ?? string.Empty;
                    }
                }
            }

            // Merge with existing configuration using pattern matching
            foreach (var (key, value) in newConfig)
            {
                if (!existingConfig.ContainsKey(key))
                {
                    builder.WithSetting(key, value);
                }
            }

            return builder;
        }

        /// <summary>
        /// Adds configuration from a dictionary of key-value pairs.
        /// </summary>
        /// <param name="builder">The configuration builder instance.</param>
        /// <param name="settings">Dictionary of configuration settings.</param>
        /// <param name="overwriteExisting">Whether to overwrite existing values.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="settings"/> is null.</exception>
        public static AppConfigurationBuilder WithSettings(
            this AppConfigurationBuilder builder,
            Dictionary<string, object> settings,
            bool overwriteExisting = false)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(settings);

            foreach (var kvp in settings)
            {
                if (overwriteExisting || !builder.Build().Exists(kvp.Key))
                {
                    builder.WithSetting(kvp.Key, kvp.Value);
                }
            }

            return builder;
        }

        /// <summary>
        /// Adds default configuration values that are only used if the key doesn't already exist.
        /// </summary>
        /// <param name="builder">The configuration builder instance.</param>
        /// <param name="defaults">Dictionary of default configuration values.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="defaults"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="defaults"/> is null.</exception>
        public static AppConfigurationBuilder WithDefaultSettings(
            this AppConfigurationBuilder builder,
            Dictionary<string, object> defaults)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(defaults);

            return builder.WithDefaults(defaults);
        }

        /// <summary>
        /// Adds configuration from a JSON string directly.
        /// </summary>
        /// <param name="builder">The configuration builder instance.</param>
        /// <param name="json">JSON string containing configuration.</param>
        /// <param name="requiredKeys">Optional list of required keys that must be present.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="json"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="json"/> is empty or whitespace.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="requiredKeys"/> are missing from JSON.</exception>
        public static AppConfigurationBuilder WithJsonString(
            this AppConfigurationBuilder builder,
            string json,
            params string[] requiredKeys)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrWhiteSpace(json);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            var configDict = new Dictionary<string, object>(StringComparer.Ordinal);

            foreach (var prop in root.EnumerateObject())
            {
                configDict[prop.Name] = prop.Value.ToString();
            }

            if (requiredKeys?.Length > 0)
            {
                var missingKeys = new List<string>();
                foreach (var key in requiredKeys)
                {
                    if (!configDict.ContainsKey(key))
                    {
                        missingKeys.Add(key);
                    }
                }

                if (missingKeys.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"JSON configuration is missing required keys: {string.Join(", ", missingKeys)}");
                }
            }

            foreach (var kvp in configDict)
            {
                builder.WithSetting(kvp.Key, kvp.Value);
            }

            return builder;
        }

        /// <summary>
        /// Adds strongly-typed configuration from an object using reflection.
        /// </summary>
        /// <param name="builder">The configuration builder instance.</param>
        /// <param name="configurationObject">Object containing configuration properties.</param>
        /// <param name="keyPrefix">Optional prefix to prepend to property names.</param>
        /// <returns>The configuration builder for method chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="configurationObject"/> is null.</exception>
        public static AppConfigurationBuilder WithObjectConfiguration(
            this AppConfigurationBuilder builder,
            object configurationObject,
            string keyPrefix = "")
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configurationObject);

            var properties = configurationObject.GetType().GetProperties(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(configurationObject);
                    var key = string.IsNullOrEmpty(keyPrefix)
                        ? prop.Name
                        : $"{keyPrefix}{prop.Name}";

                    builder.WithSetting(key, value);
                }
                catch (System.Reflection.TargetInvocationException)
                {
                    // Skip properties that throw when accessed
                    continue;
                }
                catch (System.Reflection.TargetException)
                {
                    // Skip properties that can't be read
                    continue;
                }
            }

            return builder;
        }
    }
}