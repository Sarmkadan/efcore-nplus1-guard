#nullable enable
using System;
using System.Collections.Generic;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides validation helpers for <see cref="QueryStatistics"/> and its nested <see cref="QueryStatistics.QueryStatEntry"/> instances.
    /// </summary>
    public static class QueryStatisticsValidation
    {
        /// <summary>
        /// Validates a <see cref="QueryStatistics"/> instance.
        /// </summary>
        /// <param name="value">The query statistics instance to validate.</param>
        /// <returns>An empty list as <see cref="QueryStatistics"/> maintains valid internal state.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static IReadOnlyList<string> Validate(this QueryStatistics? value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new List<string>();
        }

        /// <summary>
        /// Determines whether the specified <see cref="QueryStatistics"/> is valid.
        /// </summary>
        /// <param name="value">The query statistics instance to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool IsValid(this QueryStatistics? value) => value != null;

        /// <summary>
        /// Ensures that the specified <see cref="QueryStatistics"/> is valid, throwing an <see cref="ArgumentNullException"/> if it is null.
        /// </summary>
        /// <param name="value">The query statistics instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static void EnsureValid(this QueryStatistics? value)
        {
            ArgumentNullException.ThrowIfNull(value);
        }

        /// <summary>
        /// Validates a <see cref="QueryStatistics.QueryStatEntry"/> instance and returns a list of human-readable problems.
        /// </summary>
        /// <param name="value">The query stat entry to validate.</param>
        /// <returns>An empty list if valid; otherwise, a list of validation error messages.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static IReadOnlyList<string> Validate(this QueryStatistics.QueryStatEntry? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(value.Sql))
            {
                errors.Add("Sql cannot be null or whitespace.");
            }
            if (value.Count < 0)
            {
                errors.Add("Count cannot be negative.");
            }
            if (value.TotalDuration < TimeSpan.Zero)
            {
                errors.Add("TotalDuration cannot be negative.");
            }
            return errors;
        }

        /// <summary>
        /// Determines whether the specified <see cref="QueryStatistics.QueryStatEntry"/> is valid.
        /// </summary>
        /// <param name="value">The query stat entry to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool IsValid(this QueryStatistics.QueryStatEntry? value) => Validate(value).Count == 0;

        /// <summary>
        /// Ensures that the specified <see cref="QueryStatistics.QueryStatEntry"/> is valid, throwing an <see cref="ArgumentNullException"/> if it is null or an <see cref="ArgumentException"/> if it is invalid.
        /// </summary>
        /// <param name="value">The query stat entry to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when the entry is invalid.</exception>
        public static void EnsureValid(this QueryStatistics.QueryStatEntry? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = Validate(value);
            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    $"QueryStatEntry is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
            }
        }
    }
}
