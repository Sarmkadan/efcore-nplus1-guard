#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides JSON serialization extensions for <see cref="CallSiteWhitelist"/>.
    /// </summary>
    public static class CallSiteWhitelistJsonExtensions
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new CallSiteWhitelistConverter() }
        };

        /// <summary>
        /// Serializes the whitelist to a JSON string.
        /// </summary>
        /// <param name="value">The whitelist to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>The JSON string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this CallSiteWhitelist value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);
            var options = indented ? new JsonSerializerOptions(Options) { WriteIndented = true } : Options;
            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a whitelist from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized whitelist.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
        /// <exception cref="JsonException">Thrown when JSON parsing fails.</exception>
        public static CallSiteWhitelist? FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrEmpty(json);
            return JsonSerializer.Deserialize<CallSiteWhitelist>(json, Options);
        }

        /// <summary>
        /// Attempts to deserialize a whitelist from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized whitelist if successful; otherwise null.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public static bool TryFromJson(string json, out CallSiteWhitelist? value)
        {
            try
            {
                value = FromJson(json);
                return true;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }
    }

    internal sealed class CallSiteWhitelistConverter : JsonConverter<CallSiteWhitelist>
    {
        /// <summary>
        /// Converts between <see cref="CallSiteWhitelist"/> and JSON representation.
        /// </summary>
        public CallSiteWhitelistConverter()
        {
        }

        private static readonly FieldInfo? EntriesField = typeof(CallSiteWhitelist).GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Reads and converts the JSON to a <see cref="CallSiteWhitelist"/>.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The deserialized whitelist.</returns>
        /// <exception cref="JsonException">Thrown when JSON parsing fails or required properties are missing.</exception>
        public override CallSiteWhitelist? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var whitelist = new CallSiteWhitelist();
            using (var doc = JsonDocument.ParseValue(ref reader))
            {
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var type = element.GetProperty("type").GetString();
                    DateTimeOffset? expiresAtUtc = element.TryGetProperty("expiresAtUtc", out var exp) && exp.ValueKind != JsonValueKind.Null
                        ? exp.GetDateTimeOffset()
                        : null;

                    switch (type)
                    {
                        case "exact":
                            var typeName = element.GetProperty("typeName").GetString();
                            if (typeName == null)
                                throw new JsonException("Missing required property 'typeName' for exact entry.");

                            var methodName = element.TryGetProperty("methodName", out var mn) ? mn.GetString() : null;
                            whitelist.Add(typeName, methodName, expiresAtUtc);
                            break;

                        case "pattern":
                            var pattern = element.GetProperty("pattern").GetString();
                            if (pattern == null)
                                throw new JsonException("Missing required property 'pattern' for pattern entry.");

                            whitelist.AddPattern(pattern, expiresAtUtc);
                            break;

                        case "fingerprint":
                            var fingerprintHash = element.GetProperty("fingerprintHash").GetString();
                            if (fingerprintHash == null)
                                throw new JsonException("Missing required property 'fingerprintHash' for fingerprint entry.");

                            whitelist.AddFingerprint(fingerprintHash, expiresAtUtc);
                            break;

                        default:
                            throw new JsonException($"Unknown entry type '{type}'. Expected 'exact', 'pattern', or 'fingerprint'.");
                    }
                }
            }
            return whitelist;
        }

        /// <summary>
        /// Writes the <see cref="CallSiteWhitelist"/> to JSON.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">The serializer options.</param>
        /// <exception cref="InvalidOperationException">Thrown when the internal _entries field cannot be found.</exception>
        public override void Write(Utf8JsonWriter writer, CallSiteWhitelist value, JsonSerializerOptions options)
        {
            if (EntriesField == null)
                throw new InvalidOperationException("Could not find _entries field in CallSiteWhitelist.");

            var entries = (System.Collections.IEnumerable)EntriesField.GetValue(value)!;

            writer.WriteStartArray();
            foreach (var entry in entries)
            {
                var entryType = entry.GetType();
                var expiresAtUtc = (DateTimeOffset?)entryType.GetProperty("ExpiresAtUtc")!.GetValue(entry);

                if (entryType.Name == "ExactEntry")
                {
                    var typeName = (string)entryType.GetProperty("TypeName")!.GetValue(entry)!;
                    var methodName = (string?)entryType.GetProperty("MethodName")!.GetValue(entry);
                    writer.WriteStartObject();
                    writer.WriteString("type", "exact");
                    writer.WriteString("typeName", typeName);
                    if (methodName != null)
                        writer.WriteString("methodName", methodName);
                    WriteExpiry(writer, expiresAtUtc);
                    writer.WriteEndObject();
                }
                else if (entryType.Name == "PatternEntry")
                {
                    var pattern = (string)entryType.GetProperty("Pattern")!.GetValue(entry)!;
                    writer.WriteStartObject();
                    writer.WriteString("type", "pattern");
                    writer.WriteString("pattern", pattern);
                    WriteExpiry(writer, expiresAtUtc);
                    writer.WriteEndObject();
                }
                else if (entryType.Name == "FingerprintEntry")
                {
                    var fingerprintHash = (string)entryType.GetProperty("FingerprintHash")!.GetValue(entry)!;
                    writer.WriteStartObject();
                    writer.WriteString("type", "fingerprint");
                    writer.WriteString("fingerprintHash", fingerprintHash);
                    WriteExpiry(writer, expiresAtUtc);
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndArray();

            static void WriteExpiry(Utf8JsonWriter writer, DateTimeOffset? expiresAtUtc)
            {
                if (expiresAtUtc is { } value)
                    writer.WriteString("expiresAtUtc", value);
            }
        }
    }
}