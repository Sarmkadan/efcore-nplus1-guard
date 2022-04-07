using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;

namespace EfCoreNPlusOneGuard;

public enum NPlusOneSeverity
{
    Low,
    Medium,
    High
}

public class NPlusOneIncident
{
    public string SqlQuery { get; set; } = string.Empty;
    public int Count { get; set; }
    public NPlusOneSeverity Severity { get; set; }
    public string StackTrace { get; set; } = string.Empty;
}

public class HtmlIncidentReportWriter
{
    private readonly HtmlEncoder _htmlEncoder = HtmlEncoder.Default;

    public string Generate(IReadOnlyList<NPlusOneIncident> incidents, string title = "N+1 Report")
    {
        var sb = new StringBuilder();

        // HTML header
        sb.Append("<!DOCTYPE html>\n");
        sb.Append("<html lang=\"en\">\n");
        sb.Append("<head>\n");
        sb.Append("    <meta charset=\"utf-8\">\n");
        sb.Append("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n");
        sb.Append("    <title>");
        sb.Append(_htmlEncoder.Encode(title));
        sb.Append("</title>\n");
        sb.Append("    <style>\n");
        sb.Append("        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif; margin: 0; padding: 20px; background: #f8f9fa; color: #333; }\n");
        sb.Append("        .container { max-width: 1200px; margin: 0 auto; background: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); padding: 30px; }\n");
        sb.Append("        h1 { color: #2c3e50; margin-top: 0; border-bottom: 2px solid #3498db; padding-bottom: 10px; }\n");
        sb.Append("        .summary { background: #e8f4fc; padding: 15px; border-radius: 5px; margin-bottom: 20px; border-left: 4px solid #3498db; }\n");
        sb.Append("        .summary h2 { margin-top: 0; color: #2980b9; font-size: 1.1em; }\n");
        sb.Append("        table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }\n");
        sb.Append("        th, td { padding: 12px 15px; text-align: left; border-bottom: 1px solid #ddd; }\n");
        sb.Append("        th { background: #3498db; color: white; font-weight: 600; position: sticky; top: 0; }\n");
        sb.Append("        tr:hover { background: #f5f9fc; }\n");
        sb.Append("        tr:nth-child(even) { background: #f9f9f9; }\n");
        sb.Append("        .severity-high { background: #fff5f5 !important; }\n");
        sb.Append("        .severity-medium { background: #fff9f5 !important; }\n");
        sb.Append("        .severity-low { background: #f5fff5 !important; }\n");
        sb.Append("        details { margin-top: 5px; }\n");
        sb.Append("        summary { cursor: pointer; color: #2980b9; font-family: monospace; font-size: 0.9em; }\n");
        sb.Append("        pre { background: #f4f4f4; padding: 10px; border-radius: 4px; overflow-x: auto; margin: 5px 0; }\n");
        sb.Append("        .stack-trace { font-family: monospace; font-size: 0.85em; white-space: pre-wrap; word-wrap: break-word; }\n");
        sb.Append("        .count-badge { display: inline-block; padding: 3px 8px; border-radius: 12px; font-size: 0.85em; font-weight: 600; }\n");
        sb.Append("        .count-high { background: #e74c3c; color: white; }\n");
        sb.Append("        .count-medium { background: #f39c12; color: white; }\n");
        sb.Append("        .count-low { background: #27ae60; color: white; }\n");
        sb.Append("        .timestamp { color: #7f8c8d; font-size: 0.9em; margin-top: 20px; }\n");
        sb.Append("    </style>\n");
        sb.Append("</head>\n");
        sb.Append("<body>\n");
        sb.Append("    <div class=\"container\">\n");
        sb.Append("        <h1>");
        sb.Append(_htmlEncoder.Encode(title));
        sb.Append("</h1>\n");

        // Summary section
        sb.Append("        <div class=\"summary\">\n");
        sb.Append("            <h2>Summary</h2>\n");
        sb.Append("            <p><strong>Total incidents:</strong> ");
        sb.Append(incidents.Count);
        sb.Append("</p>\n");

        if (incidents.Count > 0)
        {
            var highCount = incidents.Count(i => i.Severity == NPlusOneSeverity.High);
            var mediumCount = incidents.Count(i => i.Severity == NPlusOneSeverity.Medium);
            var lowCount = incidents.Count(i => i.Severity == NPlusOneSeverity.Low);

            sb.Append("            <p>");
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

        sb.Append("        </div>\n");

        // Incidents table
        if (incidents.Count > 0)
        {
            sb.Append("        <table>\n");
            sb.Append("            <thead>\n");
            sb.Append("                <tr>\n");
            sb.Append("                    <th>SQL Query</th>\n");
            sb.Append("                    <th>Count</th>\n");
            sb.Append("                    <th>Severity</th>\n");
            sb.Append("                    <th>Stack Trace</th>\n");
            sb.Append("                </tr>\n");
            sb.Append("            </thead>\n");
            sb.Append("            <tbody>\n");

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
                var countBadgeClass = incident.Count switch
                {
                    > 100 => "count-high",
                    > 10 => "count-medium",
                    _ => "count-low"
                };

                sb.Append("                <tr class=\"");
                sb.Append(severityClass);
                sb.Append("\">\n");

                // SQL Query
                sb.Append("                    <td>");
                sb.Append(_htmlEncoder.Encode(incident.SqlQuery ?? "Unknown query"));
                sb.Append("</td>\n");

                // Count
                sb.Append("                    <td>");
                sb.Append("<span class=\"count-badge ");
                sb.Append(countBadgeClass);
                sb.Append("\">");
                sb.Append(incident.Count);
                sb.Append("</span></td>\n");

                // Severity
                sb.Append("                    <td>");
                sb.Append(severityText);
                sb.Append("</td>\n");

                // Stack Trace
                sb.Append("                    <td>");
                if (!string.IsNullOrEmpty(incident.StackTrace))
                {
                    sb.Append("<details>\n");
                    sb.Append("    <summary>Show stack trace</summary>\n");
                    sb.Append("    <div class=\"stack-trace\">");
                    sb.Append(_htmlEncoder.Encode(incident.StackTrace));
                    sb.Append("</div>\n");
                    sb.Append("</details>");
                }
                else
                {
                    sb.Append("&mdash;");
                }
                sb.Append("</td>\n");

                sb.Append("                </tr>\n");
            }

            sb.Append("            </tbody>\n");
            sb.Append("        </table>\n");
        }
        else
        {
            sb.Append("        <p>No N+1 incidents detected.</p>\n");
        }

        sb.Append("    </div>\n");
        sb.Append("    <div class=\"timestamp\">\n");
        sb.Append("        Generated at: ");
        sb.Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));
        sb.Append("\n");
        sb.Append("    </div>\n");
        sb.Append("</body>\n");
        sb.Append("</html>");

        return sb.ToString();
    }

    public void WriteToFile(IReadOnlyList<NPlusOneIncident> incidents, string path)
    {
        var html = Generate(incidents);
        System.IO.File.WriteAllText(path, html);
    }
}