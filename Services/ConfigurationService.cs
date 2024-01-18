// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Data;
using Newtonsoft.Json;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Service for managing application configuration
    /// </summary>
    public class ConfigurationService
    {
        private readonly IConfigurationRepository _repository;
        private readonly Dictionary<string, string> _cache;

        public ConfigurationService(IConfigurationRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cache = new Dictionary<string, string>();
            InitializeDefaults();
        }

        /// <summary>
        /// Get configuration value
        /// </summary>
        public async Task<string> GetValueAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));

            if (_cache.TryGetValue(key, out var value))
                return value;

            value = await _repository.GetValueAsync(key);
            if (!string.IsNullOrEmpty(value))
                _cache[key] = value;

            return value;
        }

        /// <summary>
        /// Get configuration value with default fallback
        /// </summary>
        public async Task<string> GetValueAsync(string key, string defaultValue)
        {
            var value = await GetValueAsync(key);
            return !string.IsNullOrEmpty(value) ? value : defaultValue;
        }

        /// <summary>
        /// Set configuration value
        /// </summary>
        public async Task SetValueAsync(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));

            await _repository.SetValueAsync(key, value);
            _cache[key] = value;
        }

        /// <summary>
        /// Get configuration as typed value
        /// </summary>
        public async Task<T> GetValueAsync<T>(string key, T defaultValue = default)
        {
            var value = await GetValueAsync(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            try
            {
                if (typeof(T) == typeof(int))
                    return (T)(object)int.Parse(value);
                else if (typeof(T) == typeof(bool))
                    return (T)(object)bool.Parse(value);
                else if (typeof(T) == typeof(long))
                    return (T)(object)long.Parse(value);
                else if (typeof(T) == typeof(double))
                    return (T)(object)double.Parse(value);
                else if (typeof(T) == typeof(string))
                    return (T)(object)value;

                return JsonConvert.DeserializeObject<T>(value);
            }
            catch (Exception ex)
            {
                throw new ValidationException($"Failed to parse configuration '{key}': {ex.Message}");
            }
        }

        /// <summary>
        /// Delete configuration key
        /// </summary>
        public async Task<bool> DeleteAsync(string key)
        {
            var result = await _repository.DeleteAsync(key);
            _cache.Remove(key);
            return result;
        }

        /// <summary>
        /// Get all configuration
        /// </summary>
        public async Task<Dictionary<string, string>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        /// <summary>
        /// Load configuration from JSON file
        /// </summary>
        public async Task LoadFromFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                filePath = Path.Combine(AppConstants.ConfigDirectory, AppConstants.AppSettingsFileName);

            if (!File.Exists(filePath))
                return;

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                if (config != null)
                {
                    foreach (var kvp in config)
                    {
                        await SetValueAsync(kvp.Key, kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CaddyVpsException($"Failed to load configuration from file: {ex.Message}", "CONFIG_LOAD_ERROR", null, ex);
            }
        }

        /// <summary>
        /// Save configuration to JSON file
        /// </summary>
        public async Task SaveToFileAsync(string filePath = null)
        {
            filePath = filePath ?? Path.Combine(AppConstants.ConfigDirectory, AppConstants.AppSettingsFileName);

            try
            {
                var config = await GetAllAsync();
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);

                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                throw new CaddyVpsException($"Failed to save configuration: {ex.Message}", "CONFIG_SAVE_ERROR", null, ex);
            }
        }

        /// <summary>
        /// Set application-level configuration
        /// </summary>
        public async Task SetCaddyAdminPortAsync(int port)
        {
            if (port <= 0 || port > 65535)
                throw new ValidationException("Port must be between 1 and 65535");

            await SetValueAsync("caddy:admin:port", port.ToString());
        }

        /// <summary>
        /// Get Caddy admin port
        /// </summary>
        public async Task<int> GetCaddyAdminPortAsync()
        {
            return await GetValueAsync("caddy:admin:port", AppConstants.DefaultCaddyAdminPort.ToString()).ContinueWith(t => int.Parse(t.Result));
        }

        /// <summary>
        /// Set logging level
        /// </summary>
        public async Task SetLoggingLevelAsync(string level)
        {
            var validLevels = new[] { "debug", "info", "warning", "error", "critical" };
            if (!Array.Exists(validLevels, e => e == level.ToLower()))
                throw new ValidationException($"Invalid logging level: {level}");

            await SetValueAsync("logging:level", level.ToLower());
        }

        /// <summary>
        /// Get logging level
        /// </summary>
        public async Task<string> GetLoggingLevelAsync()
        {
            return await GetValueAsync("logging:level", "info");
        }

        /// <summary>
        /// Enable or disable health check monitoring
        /// </summary>
        public async Task SetHealthCheckEnabledAsync(bool enabled)
        {
            await SetValueAsync("healthcheck:enabled", enabled.ToString().ToLower());
        }

        /// <summary>
        /// Get health check enabled status
        /// </summary>
        public async Task<bool> IsHealthCheckEnabledAsync()
        {
            var value = await GetValueAsync("healthcheck:enabled", "true");
            return bool.Parse(value);
        }

        private void InitializeDefaults()
        {
            // Set default values if not already set
            _cache["app:version"] = AppConstants.AppVersion;
            _cache["app:name"] = AppConstants.AppName;
        }
    }
}
