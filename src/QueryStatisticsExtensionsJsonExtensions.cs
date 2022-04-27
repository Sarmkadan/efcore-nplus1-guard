#nullable enable

using System;
using System.Text.Json;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides JSON serialization and deserialization extensions for <see cref="QueryStatistics"/>.
    /// </summary>
    public static class QueryStatisticsExtensionsJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes the <see cref="QueryStatistics"/> instance to a JSON string.
        /// </summary>
        /// <param name="value">The query statistics instance to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the query statistics.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static string ToJson(this QueryStatistics value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                : _jsonOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="QueryStatistics"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A deserialized <see cref="QueryStatistics"/> instance, or <see langword="null"/> if the JSON is empty or whitespace.</returns>
        /// <exception cref="ArgumentException"><paramref name="json"/> is null or empty.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
        public static QueryStatistics? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<QueryStatistics>(json, _jsonOptions);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="QueryStatistics"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized instance if successful; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="json"/> is null.</exception>
        public static bool TryFromJson(string json, out QueryStatistics? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            value = default;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                value = JsonSerializer.Deserialize<QueryStatistics>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}