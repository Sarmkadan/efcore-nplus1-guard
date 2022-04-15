using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="NPlusOneIncident"/>.
/// </summary>
public static class NPlusOneIncidentJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the <see cref="NPlusOneIncident"/> to a JSON string.
    /// </summary>
    /// <param name="value">The incident to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the incident.</returns>
    public static string ToJson(this NPlusOneIncident value, bool indented = false)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="NPlusOneIncident"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized incident, or null if the JSON is empty or whitespace.</returns>
    public static NPlusOneIncident? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<NPlusOneIncident>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a <see cref="NPlusOneIncident"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized incident if successful, otherwise null.</param>
    /// <returns>True if deserialization succeeded; false otherwise (including JsonException).</returns>
    public static bool TryFromJson(string json, out NPlusOneIncident? value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<NPlusOneIncident>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
