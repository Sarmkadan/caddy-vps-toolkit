using System;
using System.Text.Json;

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
        /// <returns>The deserialized domain event, or null if deserialization fails.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
        public static DomainEvent? FromJson(string json)
        {
            ArgumentNullException.ThrowIfNull(json);
            ArgumentException.ThrowIfNullOrWhiteSpace(json, nameof(json));

            return JsonSerializer.Deserialize<DomainEvent>(json, _options);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a domain event instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize. Cannot be null or empty.</param>
        /// <param name="value">Receives the deserialized domain event if successful; otherwise, null.</param>
        /// <returns>True if deserialization succeeds; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
        public static bool TryFromJson(string json, out DomainEvent? value)
        {
            ArgumentNullException.ThrowIfNull(json);
            ArgumentException.ThrowIfNullOrWhiteSpace(json, nameof(json));

            try
            {
                value = JsonSerializer.Deserialize<DomainEvent>(json, _options);
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
