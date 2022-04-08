#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Extension methods for <see cref="QueryStatistics"/> that provide additional functionality
    /// for analyzing and formatting query statistics.
    /// </summary>
    public static class QueryStatisticsExtensions
    {
        /// <summary>
        /// Gets the top queries ordered by total duration (execution time) descending.
        /// </summary>
        /// <param name="statistics">The query statistics instance.</param>
        /// <param name="n">Maximum number of entries to return.</param>
        /// <returns>A read-only list of <see cref="QueryStatistics.QueryStatEntry"/> ordered by total duration.</returns>
        public static IReadOnlyList<QueryStatistics.QueryStatEntry> TopByDuration(this QueryStatistics statistics, int n)
        {
            if (statistics is null) throw new ArgumentNullException(nameof(statistics));
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));

            return statistics.TopByCount(n)
                .OrderByDescending(entry => entry.TotalDuration)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets the top queries ordered by average duration (execution time per execution) descending.
        /// </summary>
        /// <param name="statistics">The query statistics instance.</param>
        /// <param name="n">Maximum number of entries to return.</param>
        /// <returns>A read-only list of <see cref="QueryStatistics.QueryStatEntry"/> ordered by average duration.</returns>
        public static IReadOnlyList<QueryStatistics.QueryStatEntry> TopByAvgDuration(this QueryStatistics statistics, int n)
        {
            if (statistics is null) throw new ArgumentNullException(nameof(statistics));
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));

            return statistics.TopByCount(n)
                .OrderByDescending(entry => entry.AvgDuration)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets all queries that exceed a specified average duration threshold.
        /// </summary>
        /// <param name="statistics">The query statistics instance.</param>
        /// <param name="threshold">The minimum average duration threshold.</param>
        /// <returns>A read-only list of <see cref="QueryStatistics.QueryStatEntry"/> that exceed the threshold.</returns>
        public static IReadOnlyList<QueryStatistics.QueryStatEntry> WhereAvgDurationExceeds(this QueryStatistics statistics, TimeSpan threshold)
        {
            if (statistics is null) throw new ArgumentNullException(nameof(statistics));

            return statistics.TopByCount(int.MaxValue)
                .Where(entry => entry.AvgDuration > threshold)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets a formatted string representation of the query statistics summary.
        /// </summary>
        /// <param name="statistics">The query statistics instance.</param>
        /// <param name="includeTopQueries">Whether to include top queries in the summary.</param>
        /// <returns>A formatted string containing statistics summary.</returns>
        public static string ToSummaryString(this QueryStatistics statistics, bool includeTopQueries = true)
        {
            if (statistics is null) throw new ArgumentNullException(nameof(statistics));

            var lines = new List<string>
            {
                $"Query Statistics Summary:",
                $"-------------------------",
                $"Total Queries: {statistics.TotalQueries}",
                $"Unique Queries: {statistics.UniqueQueries}",
                $"Total Duration: {statistics.TotalDuration.TotalMilliseconds:F2} ms",
                $"Average Duration: {(statistics.TotalQueries > 0 ? statistics.TotalDuration.TotalMilliseconds / statistics.TotalQueries : 0):F2} ms"
            };

            if (includeTopQueries && statistics.TotalQueries > 0)
            {
                lines.Add("");
                lines.Add("Top 5 Queries by Count:");
                lines.Add("------------------------");

                var topByCount = statistics.TopByCount(5);
                foreach (var entry in topByCount)
                {
                    lines.Add($"  {entry.Count,6} executions - {entry.TotalDuration.TotalMilliseconds,8:F2} ms total - {entry.AvgDuration.TotalMilliseconds,8:F2} ms avg");
                    lines.Add($"    {entry.Sql.Substring(0, Math.Min(entry.Sql.Length, 80))}...");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}