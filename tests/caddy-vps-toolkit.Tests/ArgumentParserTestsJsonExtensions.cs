using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace CaddyVpsToolkit.Tests;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="ArgumentParserTests"/>.
/// </summary>
public static class ArgumentParserTestsJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
		WriteIndented = false
	};

	/// <summary>
	/// Serializes the <see cref="ArgumentParserTests"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The instance to serialize. Must not be null.</param>
	/// <param name="indented">Whether to format the JSON with indentation.</param>
	/// <returns>A JSON string representation of the instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static string ToJson(this ArgumentParserTests value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		var options = indented
			? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
			: _jsonOptions;

		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Deserializes a JSON string to an <see cref="ArgumentParserTests"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize. Must not be null or whitespace.</param>
	/// <returns>The deserialized instance, or null if the JSON is empty or whitespace.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
	/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
	public static ArgumentParserTests? FromJson(string json)
	{
		ArgumentNullException.ThrowIfNull(json);

		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}

		return JsonSerializer.Deserialize<ArgumentParserTests>(json, _jsonOptions);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string to an <see cref="ArgumentParserTests"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize. Must not be null.</param>
	/// <param name="value">Receives the deserialized instance if successful.</param>
	/// <returns>True if deserialization succeeded; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
	public static bool TryFromJson(string json, out ArgumentParserTests? value)
	{
		ArgumentNullException.ThrowIfNull(json);

		value = default;

		if (string.IsNullOrWhiteSpace(json))
		{
			return true;
		}

		try
		{
			value = JsonSerializer.Deserialize<ArgumentParserTests>(json, _jsonOptions);
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}
}