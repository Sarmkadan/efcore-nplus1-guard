using System.Text.Json;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Reports N+1 query incidents to a JSON Lines file.
/// </summary>
public sealed class JsonIncidentReporter : IIncidentReporter
{
    private readonly string _filePath;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonIncidentReporter"/> class.
    /// </summary>
    /// <param name="filePath">Path to the JSON Lines output file.</param>
    public JsonIncidentReporter(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    /// <summary>
    /// Reports an N+1 query incident.
    /// </summary>
    /// <param name="incident">The incident to report.</param>
    public void Report(NPlusOneIncident incident)
    {
        lock (_lock)
        {
            var json = Serialize(incident);
            File.AppendAllText(_filePath, json + "\n");
        }
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