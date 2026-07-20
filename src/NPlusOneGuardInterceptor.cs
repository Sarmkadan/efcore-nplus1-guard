using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Intercepts database commands to detect N+1 query patterns.
/// </summary>
public sealed class NPlusOneGuardInterceptor : DbCommandInterceptor
{
    private readonly NPlusOneGuardOptions _options;
    private readonly Action<NPlusOneIncident>? _onDetected;
    private readonly QueryTracker _tracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="NPlusOneGuardInterceptor"/> class.
    /// </summary>
    /// <param name="options">The guard options.</param>
    /// <param name="onDetected">Optional callback invoked when an N+1 incident is detected.</param>
    public NPlusOneGuardInterceptor(NPlusOneGuardOptions options, Action<NPlusOneIncident>? onDetected = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _onDetected = onDetected;
        _tracker = new QueryTracker(options);
    }

    /// <summary>
    /// Intercepts synchronous reader execution.
    /// </summary>
    /// <param name="command">The database command.</param>
    /// <param name="eventData">Event data.</param>
    /// <param name="result">The interception result.</param>
    /// <returns>The interception result.</returns>
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (eventData == null)
        {
            throw new ArgumentNullException(nameof(eventData));
        }

        TrackQuery(command.CommandText);

        return base.ReaderExecuting(command, eventData, result);
    }

    /// <summary>
    /// Intercepts asynchronous reader execution.
    /// </summary>
    /// <param name="command">The database command.</param>
    /// <param name="eventData">Event data.</param>
    /// <param name="result">The interception result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The interception result.</returns>
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (eventData == null)
        {
            throw new ArgumentNullException(nameof(eventData));
        }

        await TrackQueryAsync(command.CommandText, cancellationToken).ConfigureAwait(false);

        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private void TrackQuery(string commandText)
    {
        if (commandText == null)
        {
            throw new ArgumentNullException(nameof(commandText));
        }

        foreach (var pattern in _options.IgnoredQueryPatterns)
        {
            if (commandText.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        // Check for TagWith("nplus1:ignore") comments before tracking
        if (ShouldIgnoreQuery(commandText))
        {
            return;
        }

        _tracker.TrackExecution(commandText, _onDetected);
    }

    private async ValueTask TrackQueryAsync(string commandText, CancellationToken cancellationToken)
    {
        if (commandText == null)
        {
            throw new ArgumentNullException(nameof(commandText));
        }

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var pattern in _options.IgnoredQueryPatterns)
        {
            if (commandText.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        // Check for TagWith("nplus1:ignore") comments before tracking
        if (ShouldIgnoreQuery(commandText))
        {
            return;
        }

        _tracker.TrackExecution(commandText, _onDetected);
    }

    /// <summary>
    /// Checks if a query should be ignored based on TagWith("nplus1:ignore") comments.
    /// EF Core's TagWith method adds SQL comments like: -- nplus1:ignore or /* nplus1:ignore */
    /// </summary>
    /// <param name="commandText">The SQL command text to check.</param>
    /// <returns>True if the query should be ignored; otherwise false.</returns>
    private bool ShouldIgnoreQuery(string commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText))
        {
            return false;
        }

        // Check for SQL comments containing nplus1:ignore
        // Supports both -- style comments (single line) and /* */ style comments (multi-line)

        // Remove string literals first to avoid false positives inside strings
        var withoutStrings = Regex.Replace(commandText, @"'[^']*'", "''");

        // Check for -- nplus1:ignore (case insensitive)
        var singleLineMatch = Regex.Match(withoutStrings, @"--\s*nplus1:\s*ignore", RegexOptions.IgnoreCase);
        if (singleLineMatch.Success)
        {
            return true;
        }

        // Check for /* nplus1:ignore */ (case insensitive)
        var multiLineMatch = Regex.Match(withoutStrings, @"/\*[^*]*\*+(?:[^/*][^*]*\*+)*/*\s*nplus1:\s*ignore\s*\*/", RegexOptions.IgnoreCase);
        if (multiLineMatch.Success)
        {
            return true;
        }

        return false;
    }
}
