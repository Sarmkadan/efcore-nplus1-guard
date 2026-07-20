using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Reports N+1 query incidents to a Markdown file.
/// For each incident a small summary table (fingerprint, count, severity) is written
/// followed by a details section that contains the raw SQL in a fenced code block.
/// The implementation uses reflection to avoid compile‑time coupling to the exact
/// shape of <see cref="NPlusOneIncident"/> – this prevents build failures if the
/// incident type changes (e.g., the previous attempt that accessed a non‑existent
/// <c>Fingerprint</c> property).
/// </summary>
public sealed class MarkdownIncidentReporter : IIncidentReporter
{
    private readonly string _filePath;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new <see cref="MarkdownIncidentReporter"/>.
    /// </summary>
    /// <param name="filePath">Path to the Markdown output file.</param>
    public MarkdownIncidentReporter(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    /// <summary>
    /// Writes a markdown representation of the incident to the file.
    /// </summary>
    /// <param name="incident">The incident to report.</param>
    public void Report(NPlusOneIncident incident)
    {
        if (incident == null) throw new ArgumentNullException(nameof(incident));

        lock (_lock)
        {
            var markdown = BuildMarkdownForIncident(incident);
            File.AppendAllText(_filePath, markdown);
        }
    }

    private static string BuildMarkdownForIncident(NPlusOneIncident incident)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## N+1 Incident");
        sb.AppendLine();

        // Summary table
        sb.AppendLine("| Fingerprint | Count | Severity |");
        sb.AppendLine("|---|---|---|");
        sb.AppendLine($"| {EscapePipe(GetPropertyValue(incident, "Fingerprint") ?? GetPropertyValue(incident, "QueryFingerprint") ?? "N/A")} " +
                      $"| {GetPropertyValue(incident, "Count") ?? "N/A"} " +
                      $"| {GetPropertyValue(incident, "Severity") ?? "N/A"} |");
        sb.AppendLine();

        // Details – raw SQL
        sb.AppendLine("### SQL");
        sb.AppendLine("```sql");
        sb.AppendLine(GetPropertyValue(incident, "CommandText") ??
                      GetPropertyValue(incident, "Sql") ??
                      "N/A");
        sb.AppendLine("```");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Retrieves a string representation of a property using reflection.
    /// Returns <c>null</c> if the property does not exist or its value is <c>null</c>.
    /// </summary>
    private static string? GetPropertyValue(object obj, string propertyName)
    {
        var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop == null) return null;

        var value = prop.GetValue(obj);
        return value?.ToString();
    }

    /// <summary>
    /// Escapes pipe characters so that markdown tables render correctly.
    /// </summary>
    private static string EscapePipe(string input) => input.Replace("|", "\\|");
}
