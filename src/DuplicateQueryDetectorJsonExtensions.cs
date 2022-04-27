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
        /// Tries to deserialize a <see cref="DuplicateQueryDetector"/> from JSON.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized <see cref="DuplicateQueryDetector"/>.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static bool TryFromJson(string json, out DuplicateQueryDetector? value)
        {
            try
            {
                ArgumentException.ThrowIfNullOrEmpty(json);
                value = JsonSerializer.Deserialize<DuplicateQueryDetector>(json, Options);
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
