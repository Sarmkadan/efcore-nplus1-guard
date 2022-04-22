using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Provides useful extension methods for working with <see cref="NPlusOneIncident"/> instances.
/// Includes methods for comparison, filtering, analysis, and formatting of N+1 query incidents.
/// </summary>
public static class NPlusOneIncidentExtensions
{
    /// <summary>
    /// Compares two <see cref="NPlusOneIncident"/> instances by severity and count.
    /// Incidents with higher severity come first. For incidents with equal severity,
    /// those with higher counts come first.
    /// </summary>
    /// <param name="left">The first incident to compare.</param>
    /// <param name="right">The second incident to compare.</param>
    /// <returns>
    /// A signed integer that indicates the relative ordering of the two incidents:
    /// - Less than zero: <paramref name="left"/> should come before <paramref name="right"/>
    /// - Zero: <paramref name="left"/> and <paramref name="right"/> have equal ordering
    /// - Greater than zero: <paramref name="left"/> should come after <paramref name="right"/>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when either <paramref name="left"/> or <paramref name="right"/> is <see langword="null"/>.
    /// </exception>
    public static int CompareBySeverityAndCount(this NPlusOneIncident left, NPlusOneIncident right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var severityComparison = left.Severity.CompareTo(right.Severity);
        return severityComparison != 0
            ? -severityComparison // Higher severity first (descending)
            : -left.Count.CompareTo(right.Count); // Higher count first for same severity
    }

    /// <summary>
    /// Filters a collection of incidents to only those with the specified severity level.
    /// </summary>
    /// <param name="incidents">The collection of incidents to filter.</param>
    /// <param name="severity">The severity level to match.</param>
    /// <returns>An enumerable containing only incidents with the specified severity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> is <see langword="null"/>.</exception>
    public static IEnumerable<NPlusOneIncident> FilterBySeverity(
        this IEnumerable<NPlusOneIncident> incidents,
        NPlusOneSeverity severity)
    {
        ArgumentNullException.ThrowIfNull(incidents);

        return incidents.Where(i => i.Severity == severity);
    }

    /// <summary>
    /// Filters a collection of incidents to only those with severity levels at or above the specified threshold.
    /// </summary>
    /// <param name="incidents">The collection of incidents to filter.</param>
    /// <param name="minSeverity">The minimum severity level to include.</param>
    /// <returns>An enumerable containing incidents with severity at or above the threshold.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> is <see langword="null"/>.</exception>
    public static IEnumerable<NPlusOneIncident> FilterByMinSeverity(
        this IEnumerable<NPlusOneIncident> incidents,
        NPlusOneSeverity minSeverity)
    {
        ArgumentNullException.ThrowIfNull(incidents);

        return incidents.Where(i => i.Severity >= minSeverity);
    }

    /// <summary>
    /// Filters a collection of incidents to only those with count at or above the specified threshold.
    /// </summary>
    /// <param name="incidents">The collection of incidents to filter.</param>
    /// <param name="minCount">The minimum occurrence count to include.</param>
    /// <returns>An enumerable containing incidents with count at or above the threshold.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> is <see langword="null"/>.</exception>
    public static IEnumerable<NPlusOneIncident> FilterByMinCount(
        this IEnumerable<NPlusOneIncident> incidents,
        int minCount)
    {
        ArgumentNullException.ThrowIfNull(incidents);

        return incidents.Where(i => i.Count >= minCount);
    }

    /// <summary>
    /// Groups incidents by their SQL query pattern, attempting to normalize variations of the same query.
    /// Uses simple heuristics to identify similar queries that differ only in parameter values or whitespace.
    /// </summary>
    /// <param name="incidents">The collection of incidents to group.</param>
    /// <returns>A dictionary mapping normalized query patterns to lists of incidents.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> is <see langword="null"/>.</exception>
    public static IReadOnlyDictionary<string, IReadOnlyList<NPlusOneIncident>> GroupByQueryPattern(
        this IEnumerable<NPlusOneIncident> incidents)
    {
        ArgumentNullException.ThrowIfNull(incidents);

        var groups = new Dictionary<string, List<NPlusOneIncident>>(StringComparer.OrdinalIgnoreCase);

        foreach (var incident in incidents)
        {
            var normalizedQuery = NormalizeSqlQuery(incident.SqlQuery);
            if (!groups.TryGetValue(normalizedQuery, out var group))
            {
                group = [];
                groups[normalizedQuery] = group;
            }
            group.Add(incident);
        }

        return groups.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<NPlusOneIncident>)kvp.Value.AsReadOnly());
    }

    /// <summary>
    /// Gets the total count of all incidents across all severity levels.
    /// </summary>
    /// <param name="incidents">The collection of incidents to sum.</param>
    /// <returns>The sum of all incident counts.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> is <see langword="null"/>.</exception>
    public static int TotalCount(this IEnumerable<NPlusOneIncident> incidents)
    {
        ArgumentNullException.ThrowIfNull(incidents);

        return incidents.Sum(i => i.Count);
    }

    /// <summary>
    /// Gets the total count of incidents filtered by severity.
    /// </summary>
    /// <param name="incidents">The collection of incidents to filter and sum.</param>
    /// <param name="severity">The severity level to include in the total.</param>
    /// <returns>The sum of incident counts for the specified severity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> is <see langword="null"/>.</exception>
    public static int TotalCountBySeverity(
        this IEnumerable<NPlusOneIncident> incidents,
        NPlusOneSeverity severity)
    {
        ArgumentNullException.ThrowIfNull(incidents);

        return incidents.Where(i => i.Severity == severity).Sum(i => i.Count);
    }

    /// <summary>
    /// Gets a human-readable summary of the incidents including total count and breakdown by severity.
    /// </summary>
    /// <param name="incidents">The collection of incidents to summarize.</param>
    /// <param name="culture">The culture to use for formatting numbers. Defaults to invariant culture.</param>
    /// <returns>A formatted string summary of the incidents.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> is <see langword="null"/>.</exception>
    public static string ToSummaryString(
        this IEnumerable<NPlusOneIncident> incidents,
        CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(incidents);

        culture ??= CultureInfo.InvariantCulture;

        var total = incidents.TotalCount();
        var high = incidents.TotalCountBySeverity(NPlusOneSeverity.High);
        var medium = incidents.TotalCountBySeverity(NPlusOneSeverity.Medium);
        var low = incidents.TotalCountBySeverity(NPlusOneSeverity.Low);

        return string.Create(culture, $"Total: {total} (High: {high}, Medium: {medium}, Low: {low})");
    }

    /// <summary>
    /// Sorts a collection of incidents by severity (descending) and count (descending).
    /// </summary>
    /// <param name="incidents">The collection of incidents to sort.</param>
    /// <returns>An enumerable containing incidents in sorted order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> is <see langword="null"/>.</exception>
    public static IEnumerable<NPlusOneIncident> OrderBySeverityAndCount(
        this IEnumerable<NPlusOneIncident> incidents)
    {
        ArgumentNullException.ThrowIfNull(incidents);

        return incidents.OrderByDescending(i => i, Comparer<NPlusOneIncident>.Create(CompareBySeverityAndCount));
    }

    /// <summary>
    /// Gets the most severe incident from the collection.
    /// </summary>
    /// <param name="incidents">The collection of incidents to search.</param>
    /// <returns>The incident with the highest severity, or null if the collection is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> is <see langword="null"/>.</exception>
    public static NPlusOneIncident? GetMostSevere(this IEnumerable<NPlusOneIncident> incidents)
    {
        ArgumentNullException.ThrowIfNull(incidents);

        return incidents
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => i.Count)
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets the top N most severe incidents from the collection.
    /// </summary>
    /// <param name="incidents">The collection of incidents to search.</param>
    /// <param name="count">The maximum number of incidents to return.</param>
    /// <returns>A list containing the top N most severe incidents, or fewer if the collection has fewer items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    public static IReadOnlyList<NPlusOneIncident> GetTopSevere(
        this IEnumerable<NPlusOneIncident> incidents,
        int count)
    {
        ArgumentNullException.ThrowIfNull(incidents);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        return incidents
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => i.Count)
            .Take(count)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Determines whether any incident in the collection has high severity.
    /// </summary>
    /// <param name="incidents">The collection of incidents to check.</param>
    /// <returns>True if any incident has High severity; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> is <see langword="null"/>.</exception>
    public static bool HasHighSeverity(this IEnumerable<NPlusOneIncident> incidents)
    {
        ArgumentNullException.ThrowIfNull(incidents);

        return incidents.Any(i => i.Severity == NPlusOneSeverity.High);
    }

    /// <summary>
    /// Determines whether any incident in the collection exceeds the specified count threshold.
    /// </summary>
    /// <param name="incidents">The collection of incidents to check.</param>
    /// <param name="threshold">The minimum count threshold.</param>
    /// <returns>True if any incident has count at or above the threshold; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incidents"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="threshold"/> is negative.</exception>
    public static bool AnyExceedsCount(
        this IEnumerable<NPlusOneIncident> incidents,
        int threshold)
    {
        ArgumentNullException.ThrowIfNull(incidents);
        ArgumentOutOfRangeException.ThrowIfNegative(threshold);

        return incidents.Any(i => i.Count >= threshold);
    }

    private static string NormalizeSqlQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return string.Empty;
        }

        // Normalize whitespace and remove common variations
        var normalized = sql
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("  ", " ", StringComparison.Ordinal);

        // Trim and remove trailing semicolon
        normalized = normalized.Trim();
        if (normalized.EndsWith(";", StringComparison.Ordinal))
        {
            normalized = normalized[..^1].Trim();
        }

        return normalized;
    }
}