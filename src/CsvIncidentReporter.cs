using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Reports N+1 incidents to a CSV file with one row per incident.
/// </summary>
public sealed class CsvIncidentReporter : IIncidentReporter
{
    private readonly string _filePath;
    private readonly bool _append;
    private readonly object _lock = new object();
    private bool _fileInitialized;
    private static readonly System.Text.UTF8Encoding _encoding = new System.Text.UTF8Encoding(false);

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvIncidentReporter"/> class.
    /// </summary>
    /// <param name="filePath">Path to the CSV output file.</param>
    /// <param name="append">
    /// When <see langword="true"/> (default), existing content in the file is preserved and new
    /// entries are appended. When <see langword="false"/>, the file is truncated the first time
    /// this instance writes to it, then subsequent writes append.
    /// </param>
    public CsvIncidentReporter(string filePath, bool append = true)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _append = append;
    }

    /// <summary>
    /// Reports a single incident to the CSV file.
    /// </summary>
    /// <param name="incident">The incident to report.</param>
    public void Report(NPlusOneIncident incident)
    {
        if (incident == null)
        {
            throw new ArgumentNullException(nameof(incident));
        }

        var line = FormatIncident(incident);

        lock (_lock)
        {
            WriteLines(new[] { line });
        }
    }

    /// <summary>
    /// Reports multiple incidents to the CSV file.
    /// </summary>
    /// <param name="incidents">The incidents to report.</param>
    public void ReportBatch(IEnumerable<NPlusOneIncident> incidents)
    {
        if (incidents == null)
        {
            throw new ArgumentNullException(nameof(incidents));
        }

        var lines = new List<string>();
        foreach (var incident in incidents)
        {
            if (incident != null)
            {
                lines.Add(FormatIncident(incident));
            }
        }

        if (lines.Count == 0)
        {
            return;
        }

        lock (_lock)
        {
            WriteLines(lines);
        }
    }

    /// <summary>
    /// Writes the given lines to the CSV file, honoring the <c>append</c> setting: when
    /// <see langword="false"/>, the file is truncated on the first write of this instance's
    /// lifetime and subsequently appended to; when <see langword="true"/>, existing content
    /// is always preserved. Must be called while holding <see cref="_lock"/>.
    /// </summary>
    private void WriteLines(IReadOnlyCollection<string> lines)
    {
        var truncate = !_append && !_fileInitialized;

        if (truncate)
        {
            // Write header and first data line
            var header = "Fingerprint,Count,Severity,DurationMs,CallSite";
            File.WriteAllLines(_filePath, new[] { header }.Concat(lines), _encoding);
        }
        else
        {
            File.AppendAllLines(_filePath, lines, _encoding);
        }

        _fileInitialized = true;
    }

    private static string FormatIncident(NPlusOneIncident incident)
    {
        var fingerprint = EscapeCsvField(incident.SqlQuery ?? string.Empty);
        var count = incident.Count;
        var severity = incident.Severity.ToString();
        var durationMs = "0"; // Duration is not currently tracked in NPlusOneIncident
        var callSite = EscapeCsvField(incident.CallSite ?? string.Empty);

        return $"{fingerprint},{count},{severity},{durationMs},{callSite}";
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