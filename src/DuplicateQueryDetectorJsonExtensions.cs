using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides JSON serialization helpers for <see cref="DuplicateQueryDetector"/>.
    /// </summary>
    public static class DuplicateQueryDetectorJsonExtensions
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes the <see cref="DuplicateQueryDetector"/> to JSON.
        /// </summary>
        /// <param name="value">The <see cref="DuplicateQueryDetector"/> to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>The JSON string representation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static string ToJson(this DuplicateQueryDetector value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(Options) { WriteIndented = true }
                : Options;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a <see cref="DuplicateQueryDetector"/> from JSON.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized <see cref="DuplicateQueryDetector"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null or empty.</exception>
        /// <exception cref="JsonException">Thrown if JSON is invalid.</exception>
        public static DuplicateQueryDetector? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);
            return JsonSerializer.Deserialize<DuplicateQueryDetector>(json, Options);
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="DuplicateQueryDetector"/> from JSON.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">When this method returns, contains the deserialized <see cref="DuplicateQueryDetector"/> if successful, or null if deserialization failed.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is empty.</exception>
        public static bool TryFromJson(string json, out DuplicateQueryDetector? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);
            value = JsonSerializer.Deserialize<DuplicateQueryDetector>(json, Options);
            return value is not null;
        }
    }
}
