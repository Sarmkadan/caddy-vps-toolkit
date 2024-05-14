using System.Text.Json;

namespace CaddyVpsToolkit.Domain.Models
{
	/// <summary>
	/// Provides JSON serialization helpers for <see cref="CaddyRoute"/>.
	/// </summary>
	public static class CaddyRouteJsonExtensions
	{
		// Cached serializer options with camelCase naming.
		private static readonly JsonSerializerOptions _options = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		/// <summary>
		/// Serializes the <see cref="CaddyRoute"/> instance to a JSON string.
		/// </summary>
		/// <param name="value">The route to serialize.</param>
		/// <param name="indented">If <c>true</c>, the output will be formatted with indentation.</param>
		/// <returns>A JSON representation of the route.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
		public static string ToJson(this CaddyRoute value, bool indented = false)
		{
			ArgumentNullException.ThrowIfNull(value);

			if (indented)
			{
				// Create a copy of the cached options with indentation enabled.
				var indentedOptions = new JsonSerializerOptions(_options)
				{
					WriteIndented = true
				};
				return JsonSerializer.Serialize(value, indentedOptions);
			}

			return JsonSerializer.Serialize(value, _options);
		}

		/// <summary>
		/// Deserializes a JSON string into a <see cref="CaddyRoute"/> instance.
		/// </summary>
		/// <param name="json">The JSON representation of a route.</param>
		/// <returns>The deserialized <see cref="CaddyRoute"/>, or <c>null</c> if the JSON is <c>null</c> or invalid.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
		public static CaddyRoute? FromJson(string json)
		{
			ArgumentNullException.ThrowIfNull(json);

			return JsonSerializer.Deserialize<CaddyRoute>(json, _options);
		}

		/// <summary>
		/// Attempts to deserialize a JSON string into a <see cref="CaddyRoute"/> instance.
		/// </summary>
		/// <param name="json">The JSON representation of a route.</param>
		/// <param name="value">When this method returns, contains the deserialized <see cref="CaddyRoute"/> if the operation succeeded; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
		public static bool TryFromJson(string json, out CaddyRoute? value)
		{
			ArgumentNullException.ThrowIfNull(json);

			try
			{
				value = JsonSerializer.Deserialize<CaddyRoute>(json, _options);
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