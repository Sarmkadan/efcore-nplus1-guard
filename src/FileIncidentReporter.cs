using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Reports N+1 incidents to a text log file.
/// </summary>
public sealed class FileIncidentReporter : IIncidentReporter
{
    private readonly string _filePath;
    private readonly bool _append;
    private readonly object _lock = new object();
    private bool _fileInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileIncidentReporter"/> class.
    /// </summary>
    /// <param name="filePath">The path to the log file.</param>
    /// <param name="append">
    /// When <see langword="true"/> (default), existing content in the file is preserved and new
    /// entries are appended. When <see langword="false"/>, the file is truncated the first time
    /// this instance writes to it, then subsequent writes append.
    /// </param>
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
            WriteLines(new[] { line });
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
            WriteLines(lines);
        }
    }

    /// <summary>
    /// Writes the given lines to the log file, honoring the <c>append</c> setting: when
    /// <see langword="false"/>, the file is truncated on the first write of this instance's
    /// lifetime and subsequently appended to; when <see langword="true"/>, existing content
    /// is always preserved. Must be called while holding <see cref="_lock"/>.
    /// </summary>
    private void WriteLines(IReadOnlyCollection<string> lines)
    {
        var truncate = !_append && !_fileInitialized;
        var encoding = new System.Text.UTF8Encoding(false);

        if (truncate)
        {
            File.WriteAllLines(_filePath, lines, encoding);
        }
        else
        {
            File.AppendAllLines(_filePath, lines, encoding);
        }

        _fileInitialized = true;
    }

    private static string FormatIncident(NPlusOneIncident incident)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
        var fingerprint = incident.SqlQuery ?? string.Empty;
        var count = incident.Count;
        var severity = incident.Severity.ToString();
        var stackTopFrame = incident.StackTrace?.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

        return $"{timestamp} | {fingerprint} | {count} | {severity} | {stackTopFrame}";
    }
}
