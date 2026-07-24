#nullable enable

using System;
using System.Text.Json;
using CaddyVpsToolkit.Results;

namespace CaddyVpsToolkit.Events
{
  /// <summary>
  /// Provides JSON serialization and deserialization extensions for <see cref="DomainEvent"/> instances.
  /// </summary>
  public static class DomainEventJsonExtensions
  {
    private static readonly JsonSerializerOptions _options = new()
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializes a domain event to JSON string using camelCase property naming.
    /// </summary>
    /// <param name="value">The domain event to serialize. Cannot be null.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the domain event.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this DomainEvent value, bool indented = false)
    {
      ArgumentNullException.ThrowIfNull(value);

      if (indented)
      {
        var indentedOptions = new JsonSerializerOptions(_options)
        {
          WriteIndented = true
        };
        return JsonSerializer.Serialize(value, indentedOptions);
      }

      return JsonSerializer.Serialize(value, _options);
    }

    /// <summary>
    /// Deserializes a JSON string to a domain event instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize. Cannot be null or empty.</param>
    /// <returns>A result containing the deserialized domain event if successful, or an error message if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static Result<DomainEvent> FromJson(string json)
    {
      ArgumentException.ThrowIfNullOrEmpty(json);

      try
      {
        var domainEvent = JsonSerializer.Deserialize<DomainEvent>(json, _options);
        return domainEvent is not null
          ? Result<DomainEvent>.Success(domainEvent)
          : Result<DomainEvent>.Failure("Deserialization returned null domain event");
      }
      catch (JsonException ex)
      {
        return Result<DomainEvent>.Failure(ex.Message, "JSON_PARSE_ERROR");
      }
      catch (Exception ex)
      {
        return Result<DomainEvent>.Failure(ex.Message, "JSON_DESERIALIZE_ERROR");
      }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a domain event instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize. Cannot be null or empty.</param>
    /// <param name="value">Receives the deserialized domain event if successful.</param>
    /// <returns>A result indicating success or failure.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static Result<DomainEvent> TryFromJson(string json, out DomainEvent? value)
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
        return Result<DomainEvent>.Failure(ex.Message, "JSON_DESERIALIZE_ERROR");
      }
    }
  }
}