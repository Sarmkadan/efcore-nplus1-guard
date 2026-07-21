using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Generates HTML reports for N+1 query incidents detected in Entity Framework Core applications.
/// Provides methods to create comprehensive HTML documents that visualize detected N+1 patterns
/// with severity levels, SQL queries, occurrence counts, and stack traces for debugging purposes.
/// </summary>
public class HtmlIncidentReportWriter
{
    private readonly HtmlEncoder _htmlEncoder = HtmlEncoder.Default;

    /// <summary>
    /// Generates an HTML report document containing all detected N+1 incidents.
    /// </summary>
    /// <param name="incidents">The list of N+1 incidents to include in the report.</param>
    /// <param name="title">The title for the HTML report. Defaults to "N+1 Report".</param>
    /// <returns>A complete HTML document as a string containing formatted incident data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> or <paramref name="title"/> is <see langword="null"/>.</exception>
    public string Generate(IReadOnlyList<NPlusOneIncident> incidents, string title = "N+1 Report")
    {
        ArgumentNullException.ThrowIfNull(incidents);
        ArgumentNullException.ThrowIfNull(title);

        var sb = new StringBuilder();

        // HTML header
        sb.Append("<!DOCTYPE html>\n");
        sb.Append("<html lang=\"en\">\n");
        sb.Append("<head>\n");
        sb.Append(" <meta charset=\"utf-8\">\n");
        sb.Append(" <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n");
        sb.Append(" <title>");
        sb.Append(_htmlEncoder.Encode(title));
        sb.Append("</title>\n");
        sb.Append(" <style>\n");
        sb.Append(" body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif; margin: 0; padding: 20px; background: #f8f9fa; color: #333; }\n");
        sb.Append(" .container { max-width: 1200px; margin: 0 auto; background: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); padding: 30px; }\n");
        sb.Append(" h1 { color: #2c3e50; margin-top: 0; border-bottom: 2px solid #3498db; padding-bottom: 10px; }\n");
        sb.Append(" .summary { background: #e8f4fc; padding: 15px; border-radius: 5px; margin-bottom: 20px; border-left: 4px solid #3498db; }\n");
        sb.Append(" .summary h2 { margin-top: 0; color: #2980b9; font-size: 1.1em; }\n");
        sb.Append(" table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }\n");
        sb.Append(" th, td { padding: 12px 15px; text-align: left; border-bottom: 1px solid #ddd; }\n");
        sb.Append(" th { background: #3498db; color: white; font-weight: 600; position: sticky; top: 0; cursor: pointer; }\n");
        sb.Append(" th:hover { background: #2980b9; }\n");
        sb.Append(" tr:hover { background: #f5f9fc; }\n");
        sb.Append(" tr:nth-child(even) { background: #f9f9f9; }\n");
        sb.Append(" .severity-high { background: #fff5f5 !important; }\n");
        sb.Append(" .severity-medium { background: #fff9f5 !important; }\n");
        sb.Append(" .severity-low { background: #f5fff5 !important; }\n");
        sb.Append(" details { margin-top: 5px; }\n");
        sb.Append(" summary { cursor: pointer; color: #2980b9; font-family: monospace; font-size: 0.9em; }\n");
        sb.Append(" pre { background: #f4f4f4; padding: 10px; border-radius: 4px; overflow-x: auto; margin: 5px 0; }\n");
        sb.Append(" .stack-trace { font-family: monospace; font-size: 0.85em; white-space: pre-wrap; word-wrap: break-word; }\n");
        sb.Append(" .count-badge { display: inline-block; padding: 3px 8px; border-radius: 12px; font-size: 0.85em; font-weight: 600; }\n");
        sb.Append(" .count-high { background: #e74c3c; color: white; }\n");
        sb.Append(" .count-medium { background: #f39c12; color: white; }\n");
        sb.Append(" .count-low { background: #27ae60; color: white; }\n");
        sb.Append(" .occurrence-bar-container { display: flex; align-items: center; gap: 8px; width: 200px; }\n");
        sb.Append(" .occurrence-bar { height: 12px; background: linear-gradient(90deg, #3498db, #2980b9); border-radius: 6px; transition: width 0.3s ease; }\n");
        sb.Append(" .occurrence-value { min-width: 40px; text-align: right; font-family: monospace; font-size: 0.9em; }\n");
        sb.Append(" .timestamp { color: #7f8c8d; font-size: 0.9em; margin-top: 20px; }\n");
        sb.Append(" .sort-indicator { margin-left: 5px; font-size: 0.8em; }\n");
        sb.Append(" </style>\n");
        sb.Append("<script>\n");
        sb.Append("function sortTable(columnIndex) {\n");
        sb.Append("  const table = document.querySelector('table');\n");
        sb.Append("  const tbody = table.querySelector('tbody');\n");
        sb.Append("  const rows = Array.from(tbody.querySelectorAll('tr'));\n");
        sb.Append("  const headers = table.querySelectorAll('th');\n");
        sb.Append("  const currentDir = headers[columnIndex].getAttribute('data-sort-dir');\n");
        sb.Append("  \n");
        sb.Append("  // Reset all sort indicators\n");
        sb.Append("  headers.forEach(h => {\n");
        sb.Append("    h.removeAttribute('data-sort-dir');\n");
        sb.Append("    const span = h.querySelector('.sort-indicator');\n");
        sb.Append("    if (span) span.textContent = '↕';\n");
        sb.Append("  });\n");
        sb.Append("  \n");
        sb.Append("  // Set new sort direction\n");
        sb.Append("  const newDir = currentDir === 'asc' ? 'desc' : 'asc';\n");
        sb.Append("  headers[columnIndex].setAttribute('data-sort-dir', newDir);\n");
        sb.Append("  const sortIndicator = headers[columnIndex].querySelector('.sort-indicator');\n");
        sb.Append("  if (sortIndicator) sortIndicator.textContent = newDir === 'asc' ? '↑' : '↓';\n");
        sb.Append("  \n");
        sb.Append("  // Sort rows\n");
        sb.Append("  rows.sort((a, b) => {\n");
        sb.Append("    const aVal = a.cells[columnIndex].getAttribute('data-count') || a.cells[columnIndex].textContent.trim();\n");
        sb.Append("    const bVal = b.cells[columnIndex].getAttribute('data-count') || b.cells[columnIndex].textContent.trim();\n");
        sb.Append("    return newDir === 'asc' ? aVal - bVal : bVal - aVal;\n");
        sb.Append("  });\n");
        sb.Append("  \n");
        sb.Append("  // Re-append sorted rows\n");
        sb.Append("  rows.forEach(row => tbody.appendChild(row));\n");
        sb.Append("}\n");
        sb.Append("</script>\n");
        sb.Append("</head>\n");
        sb.Append("<body>\n");
        sb.Append(" <div class=\"container\">\n");
        sb.Append(" <h1>");
        sb.Append(_htmlEncoder.Encode(title));
        sb.Append("</h1>\n");

        // Summary section
        sb.Append(" <div class=\"summary\">\n");
        sb.Append(" <h2>Summary</h2>\n");
        sb.Append(" <p><strong>Total incidents:</strong> ");
        sb.Append(incidents.Count);
        sb.Append("</p>\n");

        if (incidents.Count > 0)
        {
            var highCount = incidents.Count(i => i.Severity == NPlusOneSeverity.High);
            var mediumCount = incidents.Count(i => i.Severity == NPlusOneSeverity.Medium);
            var lowCount = incidents.Count(i => i.Severity == NPlusOneSeverity.Low);

            sb.Append(" <p>");
            sb.Append("<span class=\"count-badge count-high\">");
            sb.Append(highCount);
            sb.Append(" High</span> ");
            sb.Append("<span class=\"count-badge count-medium\">");
            sb.Append(mediumCount);
            sb.Append(" Medium</span> ");
            sb.Append("<span class=\"count-badge count-low\">");
            sb.Append(lowCount);
            sb.Append(" Low</span></p>\n");
        }

        sb.Append(" </div>\n");

        // Incidents table
        if (incidents.Count > 0)
        {
            sb.Append(" <table>\n");
            sb.Append(" <thead>\n");
            sb.Append(" <tr>\n");
            sb.Append(" <th>SQL Query</th>\n");
            sb.Append(" <th onclick=\"sortTable(0)\" data-sort-dir=\"none\">Count <span class=\"sort-indicator\">↕</span></th>\n");
            sb.Append(" <th>Severity</th>\n");
            sb.Append(" <th>Occurrences</th>\n");
            sb.Append(" <th>Stack Trace</th>\n");
            sb.Append(" </tr>\n");
            sb.Append(" </thead>\n");
            sb.Append(" <tbody>\n");

            foreach (var incident in incidents)
            {
                var severityClass = incident.Severity switch
                {
                    NPlusOneSeverity.High => "severity-high",
                    NPlusOneSeverity.Medium => "severity-medium",
                    NPlusOneSeverity.Low => "severity-low",
                    _ => ""
                };

                var severityText = incident.Severity.ToString();

                sb.Append(" <tr class=\"");
                sb.Append(severityClass);
                sb.Append("\">\n");

                // SQL Query
                sb.Append(" <td>");
                sb.Append(_htmlEncoder.Encode(incident.SqlQuery ?? "Unknown query"));
                sb.Append("</td>\n");

                // Count (now just the numeric value for sorting)
                sb.Append(" <td data-count=\"");
                sb.Append(incident.Count);
                sb.Append("\">");
                sb.Append(incident.Count);
                sb.Append("</td>\n");

                // Severity
                sb.Append(" <td>");
                sb.Append(severityText);
                sb.Append("</td>\n");

                // Occurrences (per-fingerprint occurrence bar)
                sb.Append(" <td>");
                sb.Append("<div class=\"occurrence-bar-container\">");
                sb.Append("<div class=\"occurrence-bar\" style=\"width:");
                sb.Append(incident.Count * 2); // Scale up for better visibility
                sb.Append("px\"></div>");
                sb.Append("<span class=\"occurrence-value\">");
                sb.Append(incident.Count);
                sb.Append("</span>");
                sb.Append("</div></td>\n");

                // Stack Trace
                sb.Append(" <td>");
                if (!string.IsNullOrEmpty(incident.StackTrace))
                {
                    sb.Append("<details>\n");
                    sb.Append(" <summary>Show stack trace</summary>\n");
                    sb.Append(" <div class=\"stack-trace\">");
                    sb.Append(_htmlEncoder.Encode(incident.StackTrace));
                    sb.Append("</div>\n");
                    sb.Append("</details>");
                }
                else
                {
                    sb.Append("—");
                }
                sb.Append("</td>\n");

                sb.Append(" </tr>\n");
            }

            sb.Append(" </tbody>\n");
            sb.Append(" </table>\n");
        }
        else
        {
            sb.Append(" <p>No N+1 incidents detected.</p>\n");
        }

        sb.Append(" </div>\n");
        sb.Append(" <div class=\"timestamp\">\n");
        sb.Append(" Generated at: ");
        sb.Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));
        sb.Append("\n");
        sb.Append(" </div>\n");
        sb.Append("</body>\n");
        sb.Append("</html>");

        return sb.ToString();
    }

    /// <summary>
    /// Writes an HTML report to the specified file path.
    /// </summary>
    /// <param name="incidents">The list of N+1 incidents to include in the report.</param>
    /// <param name="path">The file system path where the HTML report should be saved.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> or <paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or whitespace.</exception>
    public void WriteToFile(IReadOnlyList<NPlusOneIncident> incidents, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var html = Generate(incidents);
        System.IO.File.WriteAllText(path, html);
    }
}