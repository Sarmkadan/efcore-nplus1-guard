using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Reports N+1 incidents to a CSV file with one row per incident.
/// </summary>
public sealed class CsvIncidentReporter : FileBasedIncidentReporter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvIncidentReporter"/> class.
    /// </summary>
    /// <param name="filePath">Path to the CSV output file.</param>
    /// <param name="append">
    /// When <see langword="true"/> (default), existing content in the file is preserved and new
    /// entries are appended. When <see langword="false"/>, the file is truncated the first time
    /// this instance writes to it, then subsequent writes append.
    /// </param>
    /// <param name="options">
    /// Optional guard options used to configure the reporter's failure degradation mode.
    /// </param>
    /// <param name="logger">Optional logger for rate-limited write-failure warnings.</param>
    /// <param name="aggregator">Optional aggregator whose dropped-incident metric is updated on write failure.</param>
    public CsvIncidentReporter(
        string filePath,
        bool append = true,
        NPlusOneGuardOptions? options = null,
        ILogger? logger = null,
        IncidentAggregator? aggregator = null)
        : base(filePath, append, options, logger, aggregator)
    {
    }

    /// <summary>
    /// Formats a single incident as a CSV row.
    /// </summary>
    /// <param name="incident">The incident to format.</param>
    /// <returns>A CSV-formatted string representing the incident.</returns>
    protected override string FormatIncident(NPlusOneIncident incident)
    {
        var fingerprint = EscapeCsvField(incident.SqlQuery ?? string.Empty);
        var count = incident.Count;
        var severity = incident.Severity.ToString();
        var durationMs = "0"; // Duration is not currently tracked in NPlusOneIncident
        var callSite = EscapeCsvField(incident.CallSite ?? string.Empty);

        return $"{fingerprint},{count},{severity},{durationMs},{callSite}";
    }

    /// <summary>
    /// Gets the CSV header line.
    /// </summary>
    /// <returns>The header line.</returns>
    protected override IEnumerable<string> GetHeader()
    {
        yield return "Fingerprint,Count,Severity,DurationMs,CallSite";
    }

    /// <summary>
    /// Escapes a field for CSV output according to RFC 4180.
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        // If field contains comma, quote, or newline, wrap in quotes and escape quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return '"' + field.Replace("\"", "\"\"") + '"';
        }

        return field;
    }
}