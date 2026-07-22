using System.Text.Json;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Reports N+1 query incidents to a JSON Lines file.
/// </summary>
public sealed class JsonIncidentReporter : FileBasedIncidentReporter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonIncidentReporter"/> class.
    /// </summary>
    /// <param name="filePath">Path to the JSON Lines output file.</param>
    public JsonIncidentReporter(string filePath)
        : base(filePath, append: true)
    {
    }

    /// <summary>
    /// Formats a single incident as JSON.
    /// </summary>
    /// <param name="incident">The incident to format.</param>
    /// <returns>A JSON-formatted string representing the incident.</returns>
    protected override string FormatIncident(NPlusOneIncident incident)
    {
        return Serialize(incident);
    }

    /// <summary>
    /// Serializes an N+1 query incident to JSON.
    /// </summary>
    /// <param name="incident">The incident to serialize.</param>
    /// <returns>A JSON string representing the incident.</returns>
    public static string Serialize(NPlusOneIncident incident)
    {
        return JsonSerializer.Serialize(incident);
    }
}
