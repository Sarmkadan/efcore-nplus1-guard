#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Accumulates statistics about executed SQL queries.
    /// </summary>
    public class QueryStatistics
    {
        // Internal representation of statistics per normalized SQL.
        private sealed class Stat
        {
            public int Count;
            public TimeSpan TotalDuration;
        }

        // Thread‑safe dictionary keyed by normalized SQL.
        private readonly ConcurrentDictionary<string, Stat> _stats = new();

        /// <summary>
        /// Records the execution of a SQL query.
        /// </summary>
        /// <param name="sql">The raw SQL text.</param>
        /// <param name="duration">The time the query took to execute.</param>
        public void Record(string sql, TimeSpan duration)
        {
            if (sql is null) throw new ArgumentNullException(nameof(sql));
            if (duration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));

            var key = NormalizeSql(sql);

            _stats.AddOrUpdate(
                key,
                // New entry
                _ => new Stat { Count = 1, TotalDuration = duration },
                // Existing entry – update atomically
                (_, existing) =>
                {
                    // Increment count and add duration in a thread‑safe way.
                    // Since Stat is a reference type, we can safely mutate its fields.
                    System.Threading.Interlocked.Increment(ref existing.Count);
                    // Adding TimeSpan is not atomic; use lock on the Stat instance.
                    lock (existing)
                    {
                        existing.TotalDuration += duration;
                    }
                    return existing;
                });
        }

        /// <summary>
        /// Total number of queries recorded (including duplicates).
        /// </summary>
        public int TotalQueries => _stats.Values.Sum(s => s.Count);

        /// <summary>
        /// Number of distinct (normalized) queries recorded.
        /// </summary>
        public int UniqueQueries => _stats.Count;

        /// <summary>
        /// Sum of durations of all recorded queries.
        /// </summary>
        public TimeSpan TotalDuration => _stats.Values.Aggregate(TimeSpan.Zero, (acc, s) => acc + s.TotalDuration);

        /// <summary>
        /// Returns the top <paramref name="n"/> queries ordered by execution count descending.
        /// </summary>
        /// <param name="n">Maximum number of entries to return.</param>
        /// <returns>A read‑only list of <see cref="QueryStatEntry"/>.</returns>
        public IReadOnlyList<QueryStatEntry> TopByCount(int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));

            var top = _stats
                .OrderByDescending(kvp => kvp.Value.Count)
                .Take(n)
                .Select(kvp => new QueryStatEntry(
                    sql: kvp.Key,
                    count: kvp.Value.Count,
                    totalDuration: kvp.Value.TotalDuration))
                .ToList()
                .AsReadOnly();

            return top;
        }

        /// <summary>
        /// Clears all collected statistics.
        /// </summary>
        public void Reset()
        {
            _stats.Clear();
        }

        /// <summary>
        /// Normalizes a SQL string for use as a dictionary key.
        /// The implementation collapses whitespace and upper‑cases the string.
        /// </summary>
        private static string NormalizeSql(string sql)
        {
            // Trim, collapse consecutive whitespace to a single space, and uppercase.
            var collapsed = Regex.Replace(sql.Trim(), @"\s+", " ");
            return collapsed.ToUpperInvariant();
        }

        /// <summary>
        /// Represents a single entry in the query statistics.
        /// </summary>
        public sealed class QueryStatEntry
        {
            public string Sql { get; }
            public int Count { get; }
            public TimeSpan TotalDuration { get; }
            public TimeSpan AvgDuration => Count == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(TotalDuration.Ticks / Count);

            public QueryStatEntry(string sql, int count, TimeSpan totalDuration)
            {
                Sql = sql ?? throw new ArgumentNullException(nameof(sql));
                Count = count;
                TotalDuration = totalDuration;
            }
        }
    }
}
