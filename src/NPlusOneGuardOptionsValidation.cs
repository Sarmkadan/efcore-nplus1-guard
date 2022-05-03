#nullable enable

using System;
using System.Collections.Generic;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides validation helpers for <see cref="NPlusOneGuardOptions"/> instances.
    /// </summary>
    public static class NPlusOneGuardOptionsValidation
    {
        /// <summary>
        /// Validates a <see cref="NPlusOneGuardOptions"/> instance and returns any problems found.
        /// </summary>
        /// <param name="value">The options to validate.</param>
        /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this NPlusOneGuardOptions value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            if (value.Threshold <= 0)
                problems.Add("Threshold must be greater than zero.");

            if (value.DetectionWindow <= TimeSpan.Zero)
                problems.Add("DetectionWindow must be positive.");

            if (value.IgnoredQueryPatterns != null)
            {
                for (int i = 0; i < value.IgnoredQueryPatterns.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(value.IgnoredQueryPatterns[i]))
                    {
                        problems.Add($"Ignored query pattern at index {i} cannot be null or whitespace.");
                    }
                }
            }

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether a <see cref="NPlusOneGuardOptions"/> instance is valid.
        /// </summary>
        /// <param name="value">The options to check.</param>
        /// <returns>True if the options are valid; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static bool IsValid(this NPlusOneGuardOptions value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return value.Validate().Count == 0;
        }

        /// <summary>
        /// Ensures that a <see cref="NPlusOneGuardOptions"/> instance is valid, throwing an exception if not.
        /// </summary>
        /// <param name="value">The options to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the options contain invalid values.</exception>
        public static void EnsureValid(this NPlusOneGuardOptions value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();
            if (problems.Count > 0)
            {
                throw new ArgumentException(
                    $"NPlusOneGuardOptions is invalid. Problems: {string.Join(", ", problems)}.",
                    nameof(value)
                );
            }
        }
    }
}
