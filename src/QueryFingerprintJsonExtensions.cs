using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides JSON serialization helpers for <see cref="QueryFingerprint"/>.
    /// </summary>
    public static class QueryFingerprintJsonExtensions
    {
        /// <summary>
        /// JSON serializer options with camelCase naming policy.
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Serializes a <see cref="QueryFingerprint"/> to a JSON string.
        /// </summary>
        /// <param name="value">The QueryFingerprint to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>JSON string representation of the QueryFingerprint.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this QueryFingerprint value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = new JsonSerializerOptions(JsonOptions);
            options.WriteIndented = indented;
            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a <see cref="QueryFingerprint"/> from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A QueryFingerprint instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
        /// <exception cref="JsonException">Thrown when JSON is invalid.</exception>
        public static QueryFingerprint FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            return JsonSerializer.Deserialize<QueryFingerprint>(json, JsonOptions)
                ?? throw new JsonException("Failed to deserialize QueryFingerprint.");
        }

        /// <summary>
        /// Tries to deserialize a <see cref="QueryFingerprint"/> from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized QueryFingerprint, or null if failed.</param>
        /// <returns>True if deserialization succeeded, false otherwise.</returns>
        public static bool TryFromJson(string json, out QueryFingerprint? value)
        {
            try
            {
                ArgumentException.ThrowIfNullOrEmpty(json);
                value = JsonSerializer.Deserialize<QueryFingerprint>(json, JsonOptions);
                return value is not null;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }
    }
}
