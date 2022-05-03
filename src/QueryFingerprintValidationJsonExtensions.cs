using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides JSON serialization helpers for <see cref="QueryFingerprintValidation"/>.
    /// </summary>
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
        /// <param name="value">The QueryFingerprint to validate and serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>JSON string representation of the validation result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this QueryFingerprint value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var result = new QueryFingerprintValidationResult
            {
                CommandTextHash = value.CommandTextHash,
                NormalizedSql = value.NormalizedSql,
                CallSite = value.CallSite,
                IsValid = value.IsValid(),
                ValidationErrors = value.Validate().ToArray()
            };

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
        /// <returns>A QueryFingerprintValidationResult instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
        /// <exception cref="JsonException">Thrown when JSON is invalid.</exception>
        public static QueryFingerprintValidationResult FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);

            return JsonSerializer.Deserialize<QueryFingerprintValidationResult>(json, JsonOptions)
                   ?? throw new JsonException("Failed to deserialize QueryFingerprintValidationResult.");
        }

        /// <summary>
        /// Tries to deserialize a <see cref="QueryFingerprintValidationResult"/> from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized QueryFingerprintValidationResult, or null if failed.</param>
        /// <returns>True if deserialization succeeded, false otherwise.</returns>
        public static bool TryFromJson(string json, out QueryFingerprintValidationResult? value)
        {
            try
            {
                ArgumentException.ThrowIfNullOrEmpty(json);
                value = JsonSerializer.Deserialize<QueryFingerprintValidationResult>(json, JsonOptions);
                return value is not null;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Represents the result of validating a QueryFingerprint.
        /// </summary>
        public sealed class QueryFingerprintValidationResult
        {
            /// <summary>
            /// Gets the SHA256 hash of the normalized SQL.
            /// </summary>
            public string CommandTextHash { get; init; } = string.Empty;

            /// <summary>
            /// Gets the normalized SQL string.
            /// </summary>
            public string NormalizedSql { get; init; } = string.Empty;

            /// <summary>
            /// Gets the call site information.
            /// </summary>
            public string CallSite { get; init; } = string.Empty;

            /// <summary>
            /// Gets a value indicating whether the fingerprint is valid.
            /// </summary>
            public bool IsValid { get; init; }

            /// <summary>
            /// Gets the validation error messages, if any.
            /// </summary>
            public string[] ValidationErrors { get; init; } = Array.Empty<string>();
        }
    }
}