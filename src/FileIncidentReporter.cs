using System;
using System.Collections.Generic;
using System.Linq;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Reports N+1 incidents to a text log file.
/// </summary>
public sealed class FileIncidentReporter : FileBasedIncidentReporter
{
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
        : base(filePath, append)
    {
    }

    /// <summary>
    /// Formats a single incident as a log entry.
    /// </summary>
    /// <param name="incident">The incident to format.</param>
    /// <returns>A formatted log entry string.</returns>
    protected override string FormatIncident(NPlusOneIncident incident)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
        var fingerprint = incident.SqlQuery ?? string.Empty;
        var count = incident.Count;
        var severity = incident.Severity.ToString();
        var stackTopFrame = incident.StackTrace?.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        var callSite = incident.CallSite ?? string.Empty;

        if (!string.IsNullOrEmpty(callSite))
        {
            return $"{timestamp} | {fingerprint} | {count} | {severity} | {stackTopFrame} | CallSite: {callSite}";
        }

        return $"{timestamp} | {fingerprint} | {count} | {severity} | {stackTopFrame}";
    }
}
