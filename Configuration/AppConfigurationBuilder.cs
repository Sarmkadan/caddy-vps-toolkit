// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace CaddyVpsToolkit.Configuration
{
    /// <summary>
    /// Fluent builder for application configuration.
    /// Supports JSON files, environment variables, and programmatic configuration.
    /// </summary>
    public class AppConfigurationBuilder
    {
        private readonly Dictionary<string, object> _configuration = new();

        public AppConfigurationBuilder WithJsonFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Configuration file not found: {path}");

            try
            {
                var json = File.ReadAllText(path);
                var obj = JObject.Parse(json);

                foreach (var prop in obj.Properties())
                {
                    _configuration[prop.Name] = prop.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration from {path}: {ex.Message}");
            }

            return this;
        }

        public AppConfigurationBuilder WithEnvironmentVariables(string prefix = "")
        {
            foreach (var envVar in Environment.GetEnvironmentVariables().Keys)
            {
                var key = envVar.ToString();
                if (string.IsNullOrEmpty(prefix) || key.StartsWith(prefix))
                {
                    var value = Environment.GetEnvironmentVariable(key);
                    _configuration[key] = value;
                }
            }

            return this;
        }

        public AppConfigurationBuilder WithSetting(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key required", nameof(key));

            _configuration[key] = value;
            return this;
        }

        public AppConfigurationBuilder WithDefaults(Dictionary<string, object> defaults)
        {
            foreach (var kvp in defaults)
            {
                if (!_configuration.ContainsKey(kvp.Key))
                    _configuration[kvp.Key] = kvp.Value;
            }

            return this;
        }

        public AppConfiguration Build()
        {
            return new AppConfiguration(_configuration);
        }
    }

    /// <summary>
    /// Configuration container with type-safe access
    /// </summary>
    public class AppConfiguration
    {
        private readonly Dictionary<string, object> _config;

        public AppConfiguration(Dictionary<string, object> config)
        {
            _config = config ?? new Dictionary<string, object>();
        }

        public string GetString(string key, string defaultValue = null)
        {
            return _config.TryGetValue(key, out var value) ? value?.ToString() : defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (_config.TryGetValue(key, out var value))
            {
                if (int.TryParse(value?.ToString(), out var intValue))
                    return intValue;
            }
            return defaultValue;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (_config.TryGetValue(key, out var value))
            {
                if (bool.TryParse(value?.ToString(), out var boolValue))
                    return boolValue;
            }
            return defaultValue;
        }

        public T GetObject<T>(string key, T defaultValue = default) where T : class
        {
            return _config.TryGetValue(key, out var value) ? value as T : defaultValue;
        }

        public bool Exists(string key)
        {
            return _config.ContainsKey(key);
        }

        public Dictionary<string, object> GetAll()
        {
            return new Dictionary<string, object>(_config);
        }
    }
}
