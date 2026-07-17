# NPlusOneIncidentExtensions

Provides LINQ-style extension methods for analyzing, filtering, and summarizing collections of `NPlusOneIncident` objects detected during EF Core query execution. These extensions enable programmatic inspection of N+1 query patterns, severity-based triage, and report generation without requiring direct access to the detector internals.

## API

### CompareBySeverityAndCount
```csharp
public static int CompareBySeverityAndCount(NPlusOneIncident? x, NPlusOneIncident? y)
```
Compares two incidents by severity (descending) then by occurrence count (descending). Returns a negative value if `x` is more severe than `y`, positive if less severe, zero if equal. Throws `ArgumentNullException` if either argument is null. Suitable for use with `Array.Sort` or `OrderBy`.

### FilterBySeverity
```csharp
public static IEnumerable<NPlusOneIncident> FilterBySeverity(this IEnumerable<NPlusOneIncident> incidents, NPlusOneSeverity severity)
```
Returns incidents matching the exact `severity` level. Uses deferred execution. Throws `ArgumentNullException` if `incidents` is null.

### FilterByMinSeverity
```csharp
public static IEnumerable<NPlusOneIncident> FilterByMinSeverity(this IEnumerable<NPlusOneIncident> incidents, NPlusOneSeverity minSeverity)
```
Returns incidents with severity greater than or equal to `minSeverity`. Uses deferred execution. Throws `ArgumentNullException` if `incidents` is null.

### FilterByMinCount
```csharp
public static IEnumerable<NPlusOneIncident> FilterByMinCount(this IEnumerable<NPlusOneIncident> incidents, int minCount)
```
Returns incidents where `Count >= minCount`. Uses deferred execution. Throws `ArgumentNullException` if `incidents` is null. Throws `ArgumentOutOfRangeException` if `minCount < 0`.

### GroupByQueryPattern
```csharp
public static IReadOnlyDictionary<string, IReadOnlyList<NPlusOneIncident>> GroupByQueryPattern(this IEnumerable<NPlusOneIncident> incidents)
```
Groups incidents by their `QueryPattern` property, returning a read-only dictionary mapping pattern strings to read-only lists. Materializes the input sequence immediately. Throws `ArgumentNullException` if `incidents` is null. Throws `ArgumentException` if any incident has a null or empty `QueryPattern`.

### TotalCount
```csharp
public static int TotalCount(this IEnumerable<NPlusOneIncident> incidents)
```
Returns the sum of `Count` across all incidents. Materializes the input sequence. Throws `ArgumentNullException` if `incidents` is null. Returns 0 for empty sequences.

### TotalCountBySeverity
```csharp
public static int TotalCountBySeverity(this IEnumerable<NPlusOneIncident> incidents, NPlusOneSeverity severity)
```
Returns the sum of `Count` for incidents matching `severity`. Materializes the input sequence. Throws `ArgumentNullException` if `incidents` is null. Returns 0 if no incidents match.

### ToSummaryString
```csharp
public static string ToSummaryString(this IEnumerable<NPlusOneIncident> incidents)
```
Produces a human-readable multi-line summary including total incident count, total query count, breakdown by severity, and top 5 patterns by count. Materializes the input sequence. Throws `ArgumentNullException` if `incidents` is null. Returns "No N+1 incidents detected." for empty sequences.

### OrderBySeverityAndCount
```csharp
public static IEnumerable<NPlusOneIncident> OrderBySeverityAndCount(this IEnumerable<NPlusOneIncident> incidents)
```
Returns incidents ordered by severity (descending) then by count (descending). Uses deferred execution. Throws `ArgumentNullException` if `incidents` is null.

### GetMostSevere
```csharp
public static NPlusOneIncident? GetMostSevere(this IEnumerable<NPlusOneIncident> incidents)
```
Returns the single most severe incident (by severity then count), or null if the sequence is empty. Materializes the input sequence. Throws `ArgumentNullException` if `incidents` is null.

### GetTopSevere
```csharp
public static IReadOnlyList<NPlusOneIncident> GetTopSevere(this IEnumerable<NPlusOneIncident> incidents, int count)
```
Returns the top `count` most severe incidents as a read-only list. Materializes the input sequence. Throws `ArgumentNullException` if `incidents` is null. Throws `ArgumentOutOfRangeException` if `count < 0`. Returns all incidents if `count` exceeds sequence length.

### HasHighSeverity
```csharp
public static bool HasHighSeverity(this IEnumerable<NPlusOneIncident> incidents)
```
Returns true if any incident has severity `High` or `Critical`. Uses short-circuiting enumeration. Throws `ArgumentNullException` if `incidents` is null.

### AnyExceedsCount
```csharp
public static bool AnyExceedsCount(this IEnumerable<NPlusOneIncident> incidents, int threshold)
```
Returns true if any incident has `Count > threshold`. Uses short-circuiting enumeration. Throws `ArgumentNullException` if `incidents` is null. Throws `ArgumentOutOfRangeException` if `threshold < 0`.

## Usage

### Example 1: CI Gate with Severity Threshold
```csharp
var incidents = detector.GetIncidents();

if (incidents.HasHighSeverity())
{
    var summary = incidents.ToSummaryString();
    logger.LogError("N+1 queries detected:\n{Summary}", summary);
    Environment.ExitCode = 1;
}
else if (incidents.AnyExceedsCount(10))
{
    var top = incidents.GetTopSevere(3);
    logger.LogWarning("High-volume N+1 patterns (top 3): {Patterns}",
        string.Join(", ", top.Select(i => $"{i.QueryPattern} (x{i.Count})")));
}
```

### Example 2: Detailed Report Generation
```csharp
var incidents = detector.GetIncidents().ToList();

var report = new StringBuilder();
report.AppendLine($"Total N+1 incidents: {incidents.Count}");
report.AppendLine($"Total extra queries: {incidents.TotalCount()}");

foreach (var (pattern, group) in incidents.GroupByQueryPattern())
{
    var patternCount = group.TotalCount();
    var worst = group.GetMostSevere()!;
    report.AppendLine($"  {pattern}: {patternCount} queries (worst: {worst.Severity}, x{worst.Count})");
}

if (incidents.FilterByMinSeverity(NPlusOneSeverity.High).Any())
{
    report.AppendLine("\n⚠ High/Critical severity incidents present - review required");
}

File.WriteAllText("nplus1-report.txt", report.ToString());
```

## Notes

- All extension methods validate the `incidents` parameter and throw `ArgumentNullException` if null. Methods accepting `count` or `threshold` parameters validate non-negativity.
- Methods documented as "materializes the input sequence" enumerate the source immediately (e.g., `ToList()` internally). Methods documented as "uses deferred execution" return iterators that enumerate on demand.
- `GroupByQueryPattern` throws `ArgumentException` if any incident has a null or empty `QueryPattern`, which should not occur for incidents produced by the built-in detector but may arise from manually constructed instances.
- The type is stateless and thread-safe. Concurrent calls on the same or different sequences are safe provided the underlying `IEnumerable<NPlusOneIncident>` source is thread-safe or not shared across threads.
- `GetMostSevere` and `GetTopSevere` use `CompareBySeverityAndCount` semantics: `Critical > High > Medium > Low`, then by `Count` descending.
- `ToSummaryString` is intended for logging and diagnostics; its format is not guaranteed stable across versions.
