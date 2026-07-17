using System;
using System.Text.Json;

namespace EfCoreNPlusOneGuard
{
	/// <summary>
	/// Provides System.Text.Json based serialization helpers for <see cref="QueryTracker"/>.
	/// </summary>
	public static class QueryTrackerJsonExtensions
	{
		private static readonly JsonSerializerOptions _options = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		/// <summary>
		/// Serializes the <see cref="QueryTracker"/> instance to a JSON string.
		/// </summary>
		/// <param name="value">The <see cref="QueryTracker"/> instance to serialize.</param>
		/// <param name="indented">If <c>true</c>, the output JSON will be formatted with indentation.</param>
		/// <returns>A JSON representation of <paramref name="value"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
		public static string ToJson(this QueryTracker value, bool indented = false)
		{
			ArgumentNullException.ThrowIfNull(value);

			var options = indented
				? new JsonSerializerOptions(_options) { WriteIndented = true }
				: _options;

			return JsonSerializer.Serialize(value, options);
		}

		/// <summary>
		/// Deserializes a JSON string into a <see cref="QueryTracker"/> instance.
		/// </summary>
		/// <param name="json">The JSON string representing a <see cref="QueryTracker"/>.
		/// Must not be <c>null</c>, empty, or whitespace.</param>
		/// <returns>The deserialized <see cref="QueryTracker"/>, or <c>null</c> if the JSON represents a null value.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
		/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized into a <see cref="QueryTracker"/>.</exception>
		public static QueryTracker? FromJson(string json)
		{
			ArgumentNullException.ThrowIfNull(json);

			return string.IsNullOrWhiteSpace(json)
				? null
				: JsonSerializer.Deserialize<QueryTracker>(json, _options);
		}

		/// <summary>
		/// Attempts to deserialize a JSON string into a <see cref="QueryTracker"/> instance.
		/// </summary>
		/// <param name="json">The JSON string representing a <see cref="QueryTracker"/>.
		/// Must not be <c>null</c>, empty, or whitespace.</param>
		/// <param name="value">
		/// When this method returns, contains the deserialized <see cref="QueryTracker"/> if the operation succeeded;
		/// otherwise, <c>null</c>.
		/// </param>
		/// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
		public static bool TryFromJson(string json, out QueryTracker? value)
		{
			ArgumentNullException.ThrowIfNull(json);

			value = default;

			if (string.IsNullOrWhiteSpace(json))
			{
				return false;
			}

			try
			{
				value = JsonSerializer.Deserialize<QueryTracker>(json, _options);
				return true;
			}
			catch (JsonException)
			{
				return false;
			}
		}
	}
}