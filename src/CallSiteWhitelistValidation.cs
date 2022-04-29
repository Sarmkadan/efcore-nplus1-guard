#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides validation helpers for <see cref="CallSiteWhitelist"/> instances.
    /// </summary>
    public static class CallSiteWhitelistValidation
    {
        /// <summary>
        /// Validates a <see cref="CallSiteWhitelist"/> instance and returns any problems found.
        /// </summary>
        /// <param name="value">The whitelist to validate.</param>
        /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this CallSiteWhitelist value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // No validation needed for Count, IsWhitelisted, Clear as they don't have invariants
            // ExactEntry and PatternEntry are private implementation details with no public validation surface

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether a <see cref="CallSiteWhitelist"/> instance is valid.
        /// </summary>
        /// <param name="value">The whitelist to check.</param>
        /// <returns>True if the whitelist is valid; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static bool IsValid(this CallSiteWhitelist value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return value.Validate().Count == 0;
        }

        /// <summary>
        /// Ensures that a <see cref="CallSiteWhitelist"/> instance is valid, throwing an exception if not.
        /// </summary>
        /// <param name="value">The whitelist to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the whitelist contains invalid entries.</exception>
        public static void EnsureValid(this CallSiteWhitelist value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();
            if (problems.Count == 0)
                return;

            throw new ArgumentException(
                $"CallSiteWhitelist is invalid. Problems: {string.Join(", ", problems)}. " +
                $"CallSiteWhitelist instances should not have any validation problems.",
                nameof(value)
            );
        }
    }
}