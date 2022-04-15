using System;
using System.Collections.Generic;
using System.Linq;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// A detector for duplicate queries.
    /// </summary>
    /// <remarks>
    /// This class tracks the frequency of queries and returns groups of queries that have been executed more than a specified threshold.
    /// </remarks>
    public class DuplicateQueryDetector
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateQueryDetector"/> class.
        /// </summary>
        /// <param name="threshold">The minimum number of times a query must be executed to be considered a duplicate.</param>
        public DuplicateQueryDetector(int threshold = 2)
        {
            _threshold = threshold;
        }

        private readonly int _threshold;

        /// <summary>
        /// A dictionary mapping query keys to their execution counts.
        /// </summary>
        private readonly Dictionary<(string sql, string? parameters), int> _queryCounts = new();

        /// <summary>
        /// Records a query execution.
        /// </summary>
        /// <param name="sql">The SQL query.</param>
        /// <param name="parameters">The query parameters.</param>
        public void Record(string sql, string? parameters)
        {
            var key = (sql, parameters);
            if (_queryCounts.TryGetValue(key, out int count))
            {
                _queryCounts[key] = count + 1;
            }
            else
            {
                _queryCounts[key] = 1;
            }
        }

        /// <summary>
        /// Gets the groups of duplicate queries.
        /// </summary>
        /// <returns>A list of duplicate query groups.</returns>
        public IReadOnlyList<DuplicateQueryGroup> GetDuplicates()
        {
            return _queryCounts
                .Where(kvp => kvp.Value >= _threshold)
                .Select(kvp => new DuplicateQueryGroup
                {
                    Sql = kvp.Key.sql,
                    Parameters = kvp.Key.parameters,
                    Count = kvp.Value
                })
                .ToList();
        }

        /// <summary>
        /// Clears the query execution counts.
        /// </summary>
        public void Clear()
        {
            _queryCounts.Clear();
        }

        /// <summary>
        /// A group of duplicate queries.
        /// </summary>
        public class DuplicateQueryGroup
        {
            /// <summary>
            /// Gets the SQL query.
            /// </summary>
            public string Sql { get; set; } = string.Empty;

            /// <summary>
            /// Gets the query parameters.
            /// </summary>
            public string? Parameters { get; set; }

            /// <summary>
            /// Gets the number of times the query has been executed.
            /// </summary>
            public int Count { get; set; }
        }
    }
}
