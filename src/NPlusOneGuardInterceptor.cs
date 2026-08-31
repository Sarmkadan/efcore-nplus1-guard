using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Intercepts database commands to detect N+1 query patterns.
///
/// Example of registering the interceptor with Entity Framework Core:
/// <code>
/// var options = new NPlusOneGuardOptions()
///     .SetDetectThreshold(2)
///     .SetMinDurationToReporting);
///
/// var optionsBuilder = new DbContextOptionsBuilder<MyDbContext>();
/// optionsBuilder
///     .UseSqlServer(connectionString)
///     .AddInterceptors(new NPlusOneGuardInterceptor(options, incident =>
///     {
///         // Handle the detected N+1 query incident
///         logger.Warning("N+1 query detected: {Sql}", incident.Sql);
///     }));
///
/// var dbContextOptions = optionsBuilder.Options;
/// </code>
/// </summary>
public sealed class NPlusOneGuardInterceptor : DbCommandInterceptor
{
    private readonly NPlusOneGuardOptions _options;
    private readonly Action<NPlusOneIncident>? _onDetected;
    private readonly QueryTracker _tracker;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NPlusOneGuardInterceptor"/> class.
    /// </summary>
    /// <param name="options">The guard options.</param>
    /// <param name="onDetected">Optional callback invoked when an N+1 incident is detected.</param>
    /// <param name="logger">Optional logger used for detection and diagnostic messages.</param>
    public NPlusOneGuardInterceptor(
        NPlusOneGuardOptions options,
        Action<NPlusOneIncident>? onDetected = null,
        ILogger? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _onDetected = onDetected;
        _tracker = new QueryTracker(options);
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<NPlusOneGuardInterceptor>.Instance;

        // Log diagnostics for stale whitelist entries at startup
        if (_options.CallSiteWhitelist is { } whitelist)
        {
            whitelist.LogStaleEntries(_logger);
        }
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

        _tracker.TrackExecution(commandText, HandleDetection);
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

        _tracker.TrackExecution(commandText, HandleDetection);
    }

    private void HandleDetection(NPlusOneIncident incident)
    {
        if (_options.LogOnDetection)
        {
            _logger.LogWarning(
                "N+1 query pattern detected. NormalizedSql: {NormalizedSql}, ExecutionCount: {ExecutionCount}, Severity: {Severity}",
                incident.SqlQuery,
                incident.Count,
                incident.Severity);
        }

        _onDetected?.Invoke(incident);
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
