using System;
using System.Collections.Generic;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Reports N+1 incidents as GitHub Actions annotations.
/// Each incident is emitted as a ::warning:: annotation that appears directly in the CI logs
/// with clickable links to the source file and line number when available.
/// </summary>
public sealed class GitHubAnnotationReporter : IIncidentReporter
{
    private readonly bool _includeStackTrace;
    private readonly bool _includeCallSite;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubAnnotationReporter"/> class.
    /// </summary>
    /// <param name="includeStackTrace">When <see langword="true"/>, includes stack trace information in the annotation message.</param>
    /// <param name="includeCallSite">When <see langword="true"/>, includes call site information in the annotation message.</param>
    public GitHubAnnotationReporter(bool includeStackTrace = false, bool includeCallSite = false)
    {
        _includeStackTrace = includeStackTrace;
        _includeCallSite = includeCallSite;
    }

    /// <summary>
    /// Reports a single incident as a GitHub Actions annotation.
    /// </summary>
    /// <param name="incident">The incident to report.</param>
    public void Report(NPlusOneIncident incident)
    {
        if (incident == null)
        {
            throw new ArgumentNullException(nameof(incident));
        }

        var message = BuildAnnotationMessage(incident);
        Console.Error.WriteLine(message);
    }


    private string BuildAnnotationMessage(NPlusOneIncident incident)
    {
        // GitHub Actions annotation format:
        // ::warning file={file},line={line}::{message}
        // ::error::{message} for errors
        // ::notice::{message} for notices

        var severity = "warning";
        var file = "?";
        var line = "1";
        var messageBuilder = new System.Text.StringBuilder();

        // Build the base message with incident details
        messageBuilder.Append($"N+1 Query Detected: {incident.Count} duplicate {(incident.Count == 1 ? "query" : "queries")}");

        if (!string.IsNullOrEmpty(incident.SqlQuery))
        {
            messageBuilder.Append($" | Query: {Truncate(incident.SqlQuery, 100)}");
        }

        if (incident.Severity != null)
        {
            messageBuilder.Append($" | Severity: {incident.Severity}");
        }

        var baseMessage = messageBuilder.ToString();

        // Try to extract file and line information from stack trace
        if (_includeStackTrace && incident.StackTrace != null)
        {
            var stackFrames = incident.StackTrace.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (stackFrames.Length > 0)
            {
                // Look for file path and line number in stack frames
                // Format: "   at Namespace.Class.Method(File.cs:line 123)"
                foreach (var frame in stackFrames)
                {
                    var fileLineMatch = System.Text.RegularExpressions.Regex.Match(frame, @"in (.*?):line (\d+)");
                    if (fileLineMatch.Success)
                    {
                        file = fileLineMatch.Groups[1].Value.Trim();
                        line = fileLineMatch.Groups[2].Value;
                        break;
                    }

                    // Alternative format: "   at Namespace.Class.Method(File.cs:123)"
                    var altMatch = System.Text.RegularExpressions.Regex.Match(frame, @"(.*?):(\d+)");
                    if (altMatch.Success)
                    {
                        file = altMatch.Groups[1].Value.Trim();
                        line = altMatch.Groups[2].Value;
                        break;
                    }
                }
            }
        }

        // If call site is available and enabled, use it for file/line
        if (_includeCallSite && !string.IsNullOrEmpty(incident.CallSite))
        {
            // Call site format: "FilePath.cs:line 123"
            var callSiteMatch = System.Text.RegularExpressions.Regex.Match(incident.CallSite, @"^(.*?):(\d+)");
            if (callSiteMatch.Success)
            {
                file = callSiteMatch.Groups[1].Value.Trim();
                line = callSiteMatch.Groups[2].Value;
            }
            else
            {
                // Just use call site as file path
                file = incident.CallSite;
            }
        }

        // Build the annotation
        return $"::{severity} file={EscapeGitHubAnnotation(file)},line={line}::{baseMessage}";
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }

    private static string EscapeGitHubAnnotation(string input)
    {
        // Escape characters that have special meaning in GitHub Actions annotations
        // GitHub Actions uses :: as delimiter, so we need to escape it
        return input
            .Replace("::", "﻿::")  // Use Unicode character to break the delimiter
            .Replace("\r", "")
            .Replace("\n", " ")
            .Replace("\t", " ");
    }
}