#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Results;

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
    /// <returns>A result containing the deserialized service if successful, or an error message if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static Result<ManagedService> FromJson(string json)
    {
      ArgumentException.ThrowIfNullOrEmpty(json);

      try
      {
        var service = JsonSerializer.Deserialize<ManagedService>(json, _jsonOptions);
        return service is not null
          ? Result<ManagedService>.Success(service)
          : Result<ManagedService>.Failure("Deserialization returned null service");
      }
      catch (JsonException ex)
      {
        return Result<ManagedService>.Failure(ex.Message, "JSON_PARSE_ERROR");
      }
      catch (Exception ex)
      {
        return Result<ManagedService>.Failure(ex.Message, "JSON_DESERIALIZE_ERROR");
      }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="ManagedService"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized service if successful.</param>
    /// <returns>A result indicating success or failure.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static Result<ManagedService> TryFromJson(string json, out ManagedService? value)
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
        return Result<ManagedService>.Failure(ex.Message, "JSON_DESERIALIZE_ERROR");
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
    /// <returns>A result containing the list of deserialized services if successful, or an error message if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static Result<IReadOnlyList<ManagedService>> FromJsonToList(string json)
    {
      ArgumentException.ThrowIfNullOrEmpty(json);

      try
      {
        var services = JsonSerializer.Deserialize<IReadOnlyList<ManagedService>>(json, _jsonOptions);
        return services is not null
          ? Result<IReadOnlyList<ManagedService>>.Success(services)
          : Result<IReadOnlyList<ManagedService>>.Failure("Deserialization returned null services list");
      }
      catch (JsonException ex)
      {
        return Result<IReadOnlyList<ManagedService>>.Failure(ex.Message, "JSON_PARSE_ERROR");
      }
      catch (Exception ex)
      {
        return Result<IReadOnlyList<ManagedService>>.Failure(ex.Message, "JSON_DESERIALIZE_ERROR");
      }
    }

    /// <summary>
    /// Attempts to deserialize a JSON array string to a list of <see cref="ManagedService"/>.
    /// </summary>
    /// <param name="json">The JSON array string to deserialize.</param>
    /// <param name="values">Receives the deserialized services if successful.</param>
    /// <returns>A result indicating success or failure.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static Result<IReadOnlyList<ManagedService>> TryFromJsonToList(string json, out IReadOnlyList<ManagedService> values)
    {
      ArgumentException.ThrowIfNullOrEmpty(json);

      try
      {
        var result = FromJsonToList(json);
        if (result.IsSuccess)
        {
          values = result.Data;
          return result;
        }
        else
        {
          values = Array.Empty<ManagedService>();
          return result;
        }
      }
      catch (Exception ex)
      {
        values = Array.Empty<ManagedService>();
        return Result<IReadOnlyList<ManagedService>>.Failure(ex.Message, "JSON_DESERIALIZE_ERROR");
      }
    }
  }
}