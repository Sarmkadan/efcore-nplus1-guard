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
        /// Validates a <see cref="QueryStatistics"/> instance and returns a validation result.
        /// </summary>
        /// <param name="value">The query statistics instance to validate.</param>
        /// <returns>A validation result indicating success or failure with error messages.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static QueryFingerprintValidationResult Validate(this QueryStatistics? value)
        {
            ArgumentNullException.ThrowIfNull(value);
            var errors = new List<string>();

            // QueryStatistics itself maintains valid internal state, so we only need to validate entries
            foreach (var entry in value.TopByCount(int.MaxValue))
            {
                errors.AddRange(entry.Validate().ValidationErrors);
            }

            return errors.Count == 0
                ? QueryFingerprintValidationResult.Success(string.Empty, string.Empty, string.Empty)
                : QueryFingerprintValidationResult.Failure(string.Empty, string.Empty, string.Empty, errors.ToArray());
        }

        /// <summary>
        /// Determines whether the specified <see cref="QueryStatistics"/> is valid.
        /// </summary>
        /// <param name="value">The query statistics instance to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool IsValid(this QueryStatistics? value) => value != null && Validate(value).IsValid;

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
        /// Validates a <see cref="QueryStatistics.QueryStatEntry"/> instance and returns a validation result.
        /// </summary>
        /// <param name="value">The query stat entry to validate.</param>
        /// <returns>A validation result indicating success or failure with error messages.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static QueryFingerprintValidationResult Validate(this QueryStatistics.QueryStatEntry? value)
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

            return errors.Count == 0
                ? QueryFingerprintValidationResult.Success(string.Empty, string.Empty, string.Empty)
                : QueryFingerprintValidationResult.Failure(string.Empty, string.Empty, string.Empty, errors.ToArray());
        }

        /// <summary>
        /// Determines whether the specified <see cref="QueryStatistics.QueryStatEntry"/> is valid.
        /// </summary>
        /// <param name="value">The query stat entry to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool IsValid(this QueryStatistics.QueryStatEntry? value) => value != null && Validate(value).IsValid;

        /// <summary>
        /// Ensures that the specified <see cref="QueryStatistics.QueryStatEntry"/> is valid, throwing an <see cref="ArgumentNullException"/> if it is null or an <see cref="ArgumentException"/> if it is invalid.
        /// </summary>
        /// <param name="value">The query stat entry to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when the entry is invalid.</exception>
        public static void EnsureValid(this QueryStatistics.QueryStatEntry? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var result = Validate(value);
            if (!result.IsValid)
            {
                throw new ArgumentException(
                    $"QueryStatEntry is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, result.ValidationErrors)}");
            }
        }
    }
}