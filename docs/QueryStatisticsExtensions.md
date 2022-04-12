# QueryStatisticsExtensions

`QueryStatisticsExtensions` provides static convenience methods for analyzing and formatting query execution statistics collected by the EF Core N+1 guard. It exposes precomputed rankings and threshold-based filters over the recorded query entries, along with a human-readable summary formatter, enabling developers to quickly identify problematic query patterns without manually sorting or aggregating the raw statistics.

## API

### TopByDuration

```csharp
public static IReadOnlyList<QueryStatistics.QueryStatEntry> TopByDuration { get; }
```

Returns a read-only list of `QueryStatEntry` instances ordered by total execution duration in descending order. The list is computed from the current snapshot of collected statistics. This property is useful for identifying the single most expensive queries by cumulative time spent.

**Return value:** An `IReadOnlyList<QueryStatistics.QueryStatEntry>` where entries are sorted from highest total duration to lowest. May be empty if no queries have been recorded.

**Throws:** No exceptions are thrown under normal operation. Accessing this property while the underlying statistics collection is being mutated by another thread may yield a snapshot that is slightly stale but structurally consistent.

### TopByAvgDuration

```csharp
public static IReadOnlyList<QueryStatistics.QueryStatEntry> TopByAvgDuration { get; }
```

Returns a read-only list of `QueryStatEntry` instances ordered by average execution duration per invocation in descending order. This highlights queries that are individually slow, regardless of how many times they were called.

**Return value:** An `IReadOnlyList<QueryStatistics.QueryStatEntry>` sorted from highest average duration to lowest. May be empty if no queries have been recorded.

**Throws:** No exceptions are thrown under normal operation. The same thread-safety considerations as `TopByDuration` apply.

### WhereAvgDurationExceeds

```csharp
public static IReadOnlyList<QueryStatistics.QueryStatEntry> WhereAvgDurationExceeds(TimeSpan threshold)
```

Filters the collected query statistics to only those entries whose average execution duration exceeds the specified threshold.

**Parameters:**
- `threshold` (`TimeSpan`): The minimum average duration a query must have to be included in the result. A value of `TimeSpan.Zero` returns all entries with a non-zero average duration.

**Return value:** An `IReadOnlyList<QueryStatistics.QueryStatEntry>` containing entries with average duration greater than the threshold, ordered by average duration descending. Returns an empty list if no entries meet the criterion.

**Throws:**
- `ArgumentOutOfRangeException`: Thrown when `threshold` is negative.

### ToSummaryString

```csharp
public static string ToSummaryString(IReadOnlyList<QueryStatistics.QueryStatEntry> entries)
```

Formats a collection of query statistic entries into a multi-line human-readable summary string suitable for logging or diagnostic output. The summary includes per-query details such as invocation count, total duration, and average duration.

**Parameters:**
- `entries` (`IReadOnlyList<QueryStatistics.QueryStatEntry>`): The entries to format. Can be the result of `TopByDuration`, `TopByAvgDuration`, `WhereAvgDurationExceeds`, or any custom-filtered list.

**Return value:** A `string` containing a formatted summary. Returns a message indicating no entries when the input list is empty or null.

**Throws:** No exceptions are thrown. A null `entries` argument is handled gracefully and results in an empty-summary message.

## Usage

### Example 1: Logging the top 5 slowest queries by total duration

```csharp
using var guard = new NPlusOneGuard(dbContext);
guard.StartMonitoring();

// ... execute application logic that triggers database queries ...

guard.StopMonitoring();

var topQueries = QueryStatisticsExtensions.TopByDuration
    .Take(5)
    .ToList();

string summary = QueryStatisticsExtensions.ToSummaryString(topQueries);
Console.WriteLine("Top 5 queries by total duration:");
Console.WriteLine(summary);
```

### Example 2: Alerting on queries exceeding an average duration threshold

```csharp
using var guard = new NPlusOneGuard(dbContext);
guard.StartMonitoring();

// ... execute a critical code path ...

guard.StopMonitoring();

var slowQueries = QueryStatisticsExtensions.WhereAvgDurationExceeds(
    TimeSpan.FromMilliseconds(100));

if (slowQueries.Count > 0)
{
    string alertSummary = QueryStatisticsExtensions.ToSummaryString(slowQueries);
    logger.LogWarning(
        "Queries with average duration exceeding 100ms detected:\n{Summary}",
        alertSummary);
}
```

## Notes

- All properties and methods operate on a snapshot of the statistics taken at the moment of access. They do not lock or block concurrent writers, so results reflect a point-in-time view that may not include the very latest entries if collection is ongoing.
- The ordering within returned lists is deterministic for a given snapshot. `TopByDuration` and `TopByAvgDuration` sort descending; `WhereAvgDurationExceeds` sorts descending by average duration.
- `ToSummaryString` handles null input by returning a string indicating no entries are available, avoiding `NullReferenceException` in diagnostic logging paths.
- The returned `IReadOnlyList<T>` instances are not live views; subsequent changes to the underlying statistics collection are not reflected in previously obtained lists.
- Negative `TimeSpan` values passed to `WhereAvgDurationExceeds` are rejected with `ArgumentOutOfRangeException` because a negative duration threshold is semantically invalid.
