using System.Text.Json;
using Microsoft.Extensions.Logging;

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
    /// <param name="options">
    /// Optional guard options used to configure the reporter's failure degradation mode.
    /// </param>
    /// <param name="logger">Optional logger for rate-limited write-failure warnings.</param>
    /// <param name="aggregator">Optional aggregator whose dropped-incident metric is updated on write failure.</param>
    public JsonIncidentReporter(
        string filePath,
        NPlusOneGuardOptions? options = null,
        ILogger? logger = null,
        IncidentAggregator? aggregator = null)
        : base(filePath, append: true, options, logger, aggregator)
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
