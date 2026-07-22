using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides JSON serialization and deserialization helpers for <see cref="QueryFingerprintValidationResult"/>.
    /// </summary>
    /// <remarks>
    /// This class contains extension methods for serializing and deserializing <see cref="QueryFingerprintValidationResult"/> instances to and from JSON.
    /// </remarks>
    public static class QueryFingerprintValidationJsonExtensions
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
        /// Serializes a <see cref="QueryFingerprint"/> validation result to a JSON string.
        /// </summary>
        /// <param name="value">The <see cref="QueryFingerprint"/> to validate and serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>JSON string representation of the validation result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static string ToValidationJson(this QueryFingerprint value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var result = value.IsValid()
                ? QueryFingerprintValidationResult.Success(value.CommandTextHash, value.NormalizedSql, value.CallSite)
                : QueryFingerprintValidationResult.Failure(value.CommandTextHash, value.NormalizedSql, value.CallSite, value.Validate().ToArray());

            var options = new JsonSerializerOptions(JsonOptions)
            {
                WriteIndented = indented
            };

            return JsonSerializer.Serialize(result, options);
        }

        /// <summary>
        /// Deserializes a <see cref="QueryFingerprintValidationResult"/> from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A <see cref="QueryFingerprintValidationResult"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
        /// <exception cref="JsonException">Thrown when JSON is invalid.</exception>
        public static QueryFingerprintValidationResult FromValidationJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            return JsonSerializer.Deserialize<QueryFingerprintValidationResult>(json, JsonOptions)
            ?? throw new JsonException("Failed to deserialize QueryFingerprintValidationResult.");
        }

        /// <summary>
        /// Tries to deserialize a <see cref="QueryFingerprintValidationResult"/> from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized <see cref="QueryFingerprintValidationResult"/>, or <see langword="null"/> if failed.</param>
        /// <returns>True if deserialization succeeded, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
        public static bool TryFromValidationJson(string json, out QueryFingerprintValidationResult? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            try
            {
                value = JsonSerializer.Deserialize<QueryFingerprintValidationResult>(json, JsonOptions);
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