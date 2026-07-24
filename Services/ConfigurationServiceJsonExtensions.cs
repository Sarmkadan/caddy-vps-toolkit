#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System;
using System.Text.Json;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Results;

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
    /// <exception cref="JsonException">Thrown when serialization fails.</exception>
    public static string ToJson(this ConfigurationService value, bool indented = false)
    {
      ArgumentNullException.ThrowIfNull(value);

      var config = value.GetAllAsync().ConfigureAwait(false).GetAwaiter().GetResult();
      var options = indented
        ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
        : _jsonOptions;

      return JsonSerializer.Serialize(config, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="ConfigurationService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A result containing the deserialized configuration service if successful, or an error message if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static Result<ConfigurationService> FromJson(string json)
    {
      ArgumentException.ThrowIfNullOrEmpty(json);

      try
      {
        var config = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(json, _jsonOptions);
        if (config is null)
        {
          return Result<ConfigurationService>.Failure("Deserialization returned null configuration");
        }

        var service = new ConfigurationService(new InMemoryConfigurationRepository(config));
        return Result<ConfigurationService>.Success(service);
      }
      catch (JsonException ex)
      {
        return Result<ConfigurationService>.Failure(ex.Message, "JSON_PARSE_ERROR");
      }
      catch (Exception ex)
      {
        return Result<ConfigurationService>.Failure(ex.Message, "JSON_DESERIALIZE_ERROR");
      }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="ConfigurationService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized configuration service if successful.</param>
    /// <returns>A result indicating success or failure.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static Result<ConfigurationService> TryFromJson(string json, out ConfigurationService? value)
    {
      ArgumentException.ThrowIfNullOrEmpty(json);

      try
      {
        var result = FromJson(json);
        if (result.IsSuccess)
        {
          value = result.Data;
          return result;
        }
        else
        {
          value = null;
          return result;
        }
      }
      catch (Exception ex)
      {
        value = null;
        return Result<ConfigurationService>.Failure(ex.Message, "JSON_DESERIALIZE_ERROR");
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