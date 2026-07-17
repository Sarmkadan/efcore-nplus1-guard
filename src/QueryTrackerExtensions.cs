using System;
using System.Collections.Generic;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Provides extension methods for <see cref="QueryTracker"/>.
/// </summary>
public static class QueryTrackerExtensions
{
    /// <summary>
    /// Tracks multiple query executions sequentially.
    /// </summary>
    /// <param name="tracker">The <see cref="QueryTracker"/> instance.</param>
    /// <param name="sqlCommands">The collection of SQL commands to track.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tracker"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sqlCommands"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sqlCommands"/> contains a <see langword="null"/> element.
    /// </exception>
    public static void TrackBatch(this QueryTracker tracker, IEnumerable<string> sqlCommands)
    {
        ArgumentNullException.ThrowIfNull(tracker);
        ArgumentNullException.ThrowIfNull(sqlCommands);

        foreach (var sql in sqlCommands)
        {
            ArgumentNullException.ThrowIfNull(sql);
            tracker.TrackExecution(sql);
        }
    }

    /// <summary>
    /// Resets the tracker and returns the tracker instance for fluent usage.
    /// </summary>
    /// <param name="tracker">The <see cref="QueryTracker"/> instance.</param>
    /// <returns>The original tracker instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tracker"/> is <see langword="null"/>.
    /// </exception>
    public static QueryTracker ResetAndReturn(this QueryTracker tracker)
    {
        ArgumentNullException.ThrowIfNull(tracker);
        tracker.Reset();
        return tracker;
    }

    /// <summary>
    /// Tracks a query execution if the command text is not null or empty.
    /// </summary>
    /// <param name="tracker">The <see cref="QueryTracker"/> instance.</param>
    /// <param name="commandText">The SQL command text.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tracker"/> is <see langword="null"/>.
    /// </exception>
    public static void TrackExecutionSafe(this QueryTracker tracker, string? commandText)
    {
        ArgumentNullException.ThrowIfNull(tracker);
        if (!string.IsNullOrEmpty(commandText))
        {
            tracker.TrackExecution(commandText);
        }
    }
}
