using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Reports N+1 incidents to a text log file.
/// </summary>
public sealed class FileIncidentReporter
{
    private readonly string _filePath;
    private readonly bool _append;
    private readonly object _lock = new object();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileIncidentReporter"/> class.
    /// </summary>
    /// <param name="filePath">The path to the log file.</param>
    /// <param name="append">Whether to append to the file if it exists (default: true).</param>
    public FileIncidentReporter(string filePath, bool append = true)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _append = append;
    }

    /// <summary>
    /// Reports a single incident to the log file.
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
            File.AppendAllText(_filePath, line + Environment.NewLine, _append ? System.Text.Encoding.UTF8 : new System.Text.UTF8Encoding(false));
        }
    }

    /// <summary>
    /// Reports multiple incidents to the log file.
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
            File.AppendAllLines(_filePath, lines, _append ? System.Text.Encoding.UTF8 : new System.Text.UTF8Encoding(false));
        }
    }

    private static string FormatIncident(NPlusOneIncident incident)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var fingerprint = incident.SqlQuery ?? string.Empty;
        var count = incident.Count;
        var stackTopFrame = incident.StackTrace?.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

        return $"{timestamp} | {fingerprint} | {count} | {stackTopFrame}";
    }
}