#nullable enable
using System;
using System.Text.Json;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides JSON serialization and deserialization extensions for <see cref="NPlusOneGuardOptions"/>.
    /// </summary>
    public static class NPlusOneGuardOptionsJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes the <see cref="NPlusOneGuardOptions"/> to a JSON string.
        /// </summary>
        /// <param name="value">The options to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
        /// <returns>A JSON string representation of the options.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static string ToJson(this NPlusOneGuardOptions value, bool indented = false) =>
            value is null
                ? throw new ArgumentNullException(nameof(value))
                : JsonSerializer.Serialize(value, indented
                    ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
                    : _jsonOptions);

        /// <summary>
        /// Deserializes a JSON string into a <see cref="NPlusOneGuardOptions"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized options, or null if the JSON is empty or whitespace.</returns>
        /// <exception cref="ArgumentException"><paramref name="json"/> is null or empty.</exception>
        /// <exception cref="JsonException">Thrown when JSON parsing fails.</exception>
        public static NPlusOneGuardOptions? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<NPlusOneGuardOptions>(json, _jsonOptions);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string into a <see cref="NPlusOneGuardOptions"/> instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">Receives the deserialized options if successful, otherwise null.</param>
        /// <returns>True if deserialization succeeded; false otherwise (including JsonException).</returns>
        /// <exception cref="ArgumentException"><paramref name="json"/> is null.</exception>
        public static bool TryFromJson(string json, out NPlusOneGuardOptions? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            value = default;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                value = JsonSerializer.Deserialize<NPlusOneGuardOptions>(json, _jsonOptions);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}