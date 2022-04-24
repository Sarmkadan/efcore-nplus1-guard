using System;
using System.Collections.Generic;
using System.Linq;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Extension methods for <see cref="DuplicateQueryDetector"/>.
    /// </summary>
    public static class DuplicateQueryDetectorExtensions
    {
        /// <summary>
        /// Records a query execution without parameters.
        /// </summary>
        /// <param name="detector">The duplicate query detector.</param>
        /// <param name="sql">The SQL query.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="detector"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="sql"/> is null or empty.</exception>
        public static void Record(this DuplicateQueryDetector detector, string sql)
        {
            ArgumentNullException.ThrowIfNull(detector);
            ArgumentException.ThrowIfNullOrEmpty(sql);

            detector.Record(sql, null);
        }

        /// <summary>
        /// Checks if there are any duplicate queries detected.
        /// </summary>
        /// <param name="detector">The duplicate query detector.</param>
        /// <returns>True if any duplicates are detected; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="detector"/> is null.</exception>
        public static bool HasDuplicates(this DuplicateQueryDetector detector)
        {
            ArgumentNullException.ThrowIfNull(detector);

            return detector.GetDuplicates().Count > 0;
        }

        /// <summary>
        /// Gets the total number of duplicate query executions.
        /// </summary>
        /// <param name="detector">The duplicate query detector.</param>
        /// <returns>The total number of duplicate query executions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="detector"/> is null.</exception>
        public static int GetTotalDuplicateCount(this DuplicateQueryDetector detector)
        {
            ArgumentNullException.ThrowIfNull(detector);

            return detector.GetDuplicates().Sum(d => d.Count);
        }
    }
}
