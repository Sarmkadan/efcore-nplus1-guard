using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>
    /// Runs <paramref name="scope"/> with an in-memory incident collector activated as the
    /// ambient <see cref="InMemoryIncidentReporter.Current"/> reporter, then fails with an
    /// <see cref="NPlusOneDetectedException"/> if any N+1 incident was reported while it ran.
    /// </summary>
    /// <remarks>
    /// For this to observe anything, <paramref name="context"/> (or whichever
    /// <see cref="DbContext"/> instances are exercised by <paramref name="scope"/>) must have been
    /// configured with
    /// <c>options.UseNPlusOneGuard(onDetected: incident => InMemoryIncidentReporter.Current?.Report(incident))</c>.
    /// The <paramref name="context"/> parameter itself is not queried directly - it exists so the
    /// assertion reads naturally against the context under test and so the compiler can infer intent
    /// at the call site.
    /// </remarks>
    /// <param name="context">The <see cref="DbContext"/> under test.</param>
    /// <param name="scope">The code to execute under N+1 observation, typically a repository or service call.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> or <paramref name="scope"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="NPlusOneDetectedException">Thrown when one or more N+1 incidents were detected while <paramref name="scope"/> ran.</exception>
    public static void AssertNoNPlusOne(this DbContext context, Action scope)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(scope);

        RunAndAssert(scope);
    }

    /// <summary>
    /// Runs <paramref name="scope"/> with an in-memory incident collector activated as the
    /// ambient <see cref="InMemoryIncidentReporter.Current"/> reporter, then fails with an
    /// <see cref="NPlusOneDetectedException"/> if any N+1 incident was reported while it ran.
    /// </summary>
    /// <remarks>
    /// For this to observe anything, whichever <see cref="DbContext"/> is resolved from
    /// <paramref name="serviceProvider"/> and exercised by <paramref name="scope"/> must have been
    /// configured with
    /// <c>options.UseNPlusOneGuard(onDetected: incident => InMemoryIncidentReporter.Current?.Report(incident))</c>.
    /// </remarks>
    /// <param name="serviceProvider">The service provider used to resolve dependencies for the test.</param>
    /// <param name="scope">The code to execute under N+1 observation, typically a repository or service call.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceProvider"/> or <paramref name="scope"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="NPlusOneDetectedException">Thrown when one or more N+1 incidents were detected while <paramref name="scope"/> ran.</exception>
    public static void AssertNoNPlusOne(this IServiceProvider serviceProvider, Action scope)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(scope);

        RunAndAssert(scope);
    }

    private static void RunAndAssert(Action scope)
    {
        var reporter = new InMemoryIncidentReporter();

        using (InMemoryIncidentReporter.Activate(reporter))
        {
            scope();
        }

        var offenders = reporter.Aggregator.GetTopOffenders(5);
        if (offenders.Count == 0)
        {
            return;
        }

        var allIncidents = reporter.Aggregator.All();
        var message = FormatFailureMessage(offenders, allIncidents);
        var representativeIncident = allIncidents[allIncidents.Count - 1];

        throw new NPlusOneDetectedException(representativeIncident, message);
    }

    private static string FormatFailureMessage(
        IReadOnlyList<IncidentAggregator.TopOffender> offenders,
        IReadOnlyList<NPlusOneIncident> allIncidents)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"N+1 query pattern(s) detected while asserting no N+1 ({offenders.Count} distinct offender(s)):");

        foreach (var offender in offenders)
        {
            var callSite = allIncidents
                .LastOrDefault(incident => (incident.SqlQuery ?? string.Empty) == offender.Fingerprint)
                ?.CallSite ?? "unknown";

            builder.AppendLine();
            builder.AppendLine($"  fingerprint: {offender.Fingerprint.GetHashCode():X8}");
            builder.AppendLine($"  count:       {offender.Count}");
            builder.AppendLine($"  sql:         {Truncate(offender.Fingerprint, 300)}");
            builder.AppendLine($"  call site:   {callSite}");
        }

        return builder.ToString();
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
