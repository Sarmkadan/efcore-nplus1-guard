using System;
using System.Collections.Generic;
using System.IO;
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
	private const string Styles =
		" body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif; margin: 0; padding: 20px; background: #f8f9fa; color: #333; }\n" +
		" .container { max-width: 1200px; margin: 0 auto; background: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); padding: 30px; }\n" +
		" h1 { color: #2c3e50; margin-top: 0; border-bottom: 2px solid #3498db; padding-bottom: 10px; }\n" +
		" .summary { background: #e8f4fc; padding: 15px; border-radius: 5px; margin-bottom: 20px; border-left: 4px solid #3498db; }\n" +
		" .summary h2 { margin-top: 0; color: #2980b9; font-size: 1.1em; }\n" +
		" table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }\n" +
		" th, td { padding: 12px 15px; text-align: left; border-bottom: 1px solid #ddd; }\n" +
		" th { background: #3498db; color: white; font-weight: 600; position: sticky; top: 0; cursor: pointer; }\n" +
		" th:hover { background: #2980b9; }\n" +
		" tr:hover { background: #f5f9fc; }\n" +
		" tr:nth-child(even) { background: #f9f9f9; }\n" +
		" .severity-high { background: #fff5f5 !important; }\n" +
		" .severity-medium { background: #fff9f5 !important; }\n" +
		" .severity-low { background: #f5fff5 !important; }\n" +
		" details { margin-top: 5px; }\n" +
		" summary { cursor: pointer; color: #2980b9; font-family: monospace; font-size: 0.9em; }\n" +
		" pre { background: #f4f4f4; padding: 10px; border-radius: 4px; overflow-x: auto; margin: 5px 0; }\n" +
		" .stack-trace { font-family: monospace; font-size: 0.85em; white-space: pre-wrap; word-wrap: break-word; }\n" +
		" .count-badge { display: inline-block; padding: 3px 8px; border-radius: 12px; font-size: 0.85em; font-weight: 600; }\n" +
		" .count-high { background: #e74c3c; color: white; }\n" +
		" .count-medium { background: #f39c12; color: white; }\n" +
		" .count-low { background: #27ae60; color: white; }\n" +
		" .occurrence-bar-container { display: flex; align-items: center; gap: 8px; width: 200px; }\n" +
		" .occurrence-bar { height: 12px; background: linear-gradient(90deg, #3498db, #2980b9); border-radius: 6px; transition: width 0.3s ease; }\n" +
		" .occurrence-value { min-width: 40px; text-align: right; font-family: monospace; font-size: 0.9em; }\n" +
		" .timestamp { color: #7f8c8d; font-size: 0.9em; margin-top: 20px; }\n" +
		" .sort-indicator { margin-left: 5px; font-size: 0.8em; }\n";

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
		AppendHeader(sb, title);
		AppendSummary(sb, incidents);
		AppendIncidentTable(sb, incidents);
		AppendFooter(sb);
		return sb.ToString();
	}

	private void AppendHeader(StringBuilder sb, string title)
	{
		sb.Append("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n");
		sb.Append(" <meta charset=\"utf-8\">\n <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n <title>");
		sb.Append(_htmlEncoder.Encode(title));
		sb.Append("</title>\n <style>\n");
		sb.Append(Styles);
		sb.Append(" </style>\n<script>\n");
		AppendSortingScript(sb);
		sb.Append("</script>\n</head>\n<body>\n <div class=\"container\">\n <h1>");
		sb.Append(_htmlEncoder.Encode(title));
		sb.Append("</h1>\n");
	}

	private static void AppendSortingScript(StringBuilder sb)
	{
		sb.Append("function sortTable(columnIndex) {\n");
		sb.Append(" const table = document.querySelector('table');\n const tbody = table.querySelector('tbody');\n const rows = Array.from(tbody.querySelectorAll('tr'));\n const headers = table.querySelectorAll('th');\n const currentDir = headers[columnIndex].getAttribute('data-sort-dir');\n \n");
		sb.Append(" // Reset all sort indicators\n headers.forEach(h => {\n h.removeAttribute('data-sort-dir');\n const span = h.querySelector('.sort-indicator');\n if (span) span.textContent = '↕';\n });\n \n");
		sb.Append(" // Set new sort direction\n const newDir = currentDir === 'asc' ? 'desc' : 'asc';\n headers[columnIndex].setAttribute('data-sort-dir', newDir);\n const sortIndicator = headers[columnIndex].querySelector('.sort-indicator');\n if (sortIndicator) sortIndicator.textContent = newDir === 'asc' ? '↑' : '↓';\n \n");
		sb.Append(" // Sort rows\n rows.sort((a, b) => {\n const aVal = a.cells[columnIndex].getAttribute('data-count') || a.cells[columnIndex].textContent.trim();\n const bVal = b.cells[columnIndex].getAttribute('data-count') || b.cells[columnIndex].textContent.trim();\n return newDir === 'asc' ? aVal - bVal : bVal - aVal;\n });\n \n");
		sb.Append(" // Re-append sorted rows\n rows.forEach(row => tbody.appendChild(row));\n}\n");
	}

	private static void AppendSummary(StringBuilder sb, IReadOnlyList<NPlusOneIncident> incidents)
	{
		sb.Append(" <div class=\"summary\">\n <h2>Summary</h2>\n <p><strong>Total incidents:</strong> ");
		sb.Append(incidents.Count);
		sb.Append("</p>\n");

		if (incidents.Count > 0)
		{
			sb.Append(" <p><span class=\"count-badge count-high\">");
			sb.Append(incidents.Count(i => i.Severity == NPlusOneSeverity.High));
			sb.Append(" High</span> <span class=\"count-badge count-medium\">");
			sb.Append(incidents.Count(i => i.Severity == NPlusOneSeverity.Medium));
			sb.Append(" Medium</span> <span class=\"count-badge count-low\">");
			sb.Append(incidents.Count(i => i.Severity == NPlusOneSeverity.Low));
			sb.Append(" Low</span></p>\n");
		}

		sb.Append(" </div>\n");
	}

	private void AppendIncidentTable(StringBuilder sb, IReadOnlyList<NPlusOneIncident> incidents)
	{
		if (incidents.Count == 0)
		{
			sb.Append(" <p>No N+1 incidents detected.</p>\n");
			return;
		}

		sb.Append(" <table>\n <thead>\n <tr>\n <th>SQL Query</th>\n");
		sb.Append(" <th onclick=\"sortTable(0)\" data-sort-dir=\"none\">Count <span class=\"sort-indicator\">↕</span></th>\n");
		sb.Append(" <th>Severity</th>\n <th>Occurrences</th>\n <th>Stack Trace</th>\n </tr>\n </thead>\n <tbody>\n");

		foreach (var incident in incidents)
		{
			AppendIncidentRow(sb, incident);
		}

		sb.Append(" </tbody>\n </table>\n");
	}

	private void AppendIncidentRow(StringBuilder sb, NPlusOneIncident incident)
	{
		var severityClass = incident.Severity switch
		{
			NPlusOneSeverity.High => "severity-high",
			NPlusOneSeverity.Medium => "severity-medium",
			NPlusOneSeverity.Low => "severity-low",
			_ => ""
		};

		sb.Append(" <tr class=\"");
		sb.Append(severityClass);
		sb.Append("\">\n <td>");
		sb.Append(_htmlEncoder.Encode(incident.SqlQuery ?? "Unknown query"));
		sb.Append("</td>\n <td data-count=\"");
		sb.Append(incident.Count);
		sb.Append("\">");
		sb.Append(incident.Count);
		sb.Append("</td>\n <td>");
		sb.Append(_htmlEncoder.Encode(incident.Severity.ToString()));
		sb.Append("</td>\n <td><div class=\"occurrence-bar-container\"><div class=\"occurrence-bar\" style=\"width:");
		sb.Append(incident.Count * 2);
		sb.Append("px\"></div><span class=\"occurrence-value\">");
		sb.Append(incident.Count);
		sb.Append("</span></div></td>\n <td>");
		AppendCallSite(sb, incident.StackTrace);
		sb.Append("</td>\n </tr>\n");
	}

	private void AppendCallSite(StringBuilder sb, string? callSite)
	{
		if (string.IsNullOrEmpty(callSite))
		{
			sb.Append("—");
			return;
		}

		sb.Append("<details>\n <summary>Show stack trace</summary>\n <div class=\"stack-trace\">");
		sb.Append(_htmlEncoder.Encode(callSite));
		sb.Append("</div>\n</details>");
	}

	private static void AppendFooter(StringBuilder sb)
	{
		sb.Append(" </div>\n <div class=\"timestamp\">\n Generated at: ");
		sb.Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));
		sb.Append("\n </div>\n</body>\n</html>");
	}

	/// <summary>
	/// Validates that the file path does not contain directory traversal sequences.
	/// </summary>
	/// <param name="path">The file path to validate.</param>
	/// <returns>The resolved absolute path if valid.</returns>
	/// <exception cref="ArgumentException">Thrown if the path contains directory traversal sequences.</exception>
	private static string ValidateFilePath(string path)
	{
		// Get the full path to resolve any relative segments and normalize the path
		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(path);
		}
		catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
		{
			throw new ArgumentException(
				"The file path is invalid or too long.",
				nameof(path),
				ex);
		}

		// Check for directory traversal attempts by comparing the resolved path
		// against a normalized version of the input path
		string normalizedInputPath = Path.GetFullPath(path);

		// If the normalized paths don't match, it means traversal sequences were resolved
		// (e.g., "subdir/../file.txt" becomes "file.txt")
		if (!string.Equals(fullPath, normalizedInputPath, StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentException(
				"The file path contains directory traversal sequences (e.g., '..' or absolute paths) that could write outside the intended directory.",
				nameof(path));
		}

		// Additional check: ensure the path is not rooted outside the current directory structure
		if (Path.IsPathRooted(fullPath))
		{
			// For rooted paths, ensure they don't escape the current working directory
			string currentDirectory = Directory.GetCurrentDirectory();
			string currentDirectoryFullPath = Path.GetFullPath(currentDirectory);

			// Check if the resolved path starts with the current directory
			if (!fullPath.StartsWith(currentDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException(
					"The file path resolves to a location outside the current working directory.",
					nameof(path));
			}
		}

		return fullPath;
	}

	/// <summary>
	/// Writes an HTML report to the specified file path.
	/// </summary>
	/// <param name="incidents">The list of N+1 incidents to include in the report.</param>
	/// <param name="path">The file system path where the HTML report should be saved.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> or <paramref name="path"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or whitespace.</exception>
	/// <exception cref="ArgumentException">
	/// Thrown if <paramref name="path"/> contains directory traversal sequences that would write outside the intended directory.
	/// </exception>
	public void WriteToFile(IReadOnlyList<NPlusOneIncident> incidents, string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		// Validate the path to prevent directory traversal attacks
		string validatedPath = ValidateFilePath(path);

		var html = Generate(incidents);
		System.IO.File.WriteAllText(validatedPath, html);
	}
}
