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

            // Use reflection to access the private _entries field
            var entriesField = typeof(CallSiteWhitelist).GetField("_entries",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (entriesField == null)
            {
                problems.Add("Could not access internal entries collection of CallSiteWhitelist.");
                return problems.AsReadOnly();
            }

            var entries = entriesField.GetValue(value) as System.Collections.IList;
            if (entries == null)
            {
                problems.Add("Internal entries collection is null.");
                return problems.AsReadOnly();
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    problems.Add($"Entry at index {i} is null.");
                    continue;
                }

                var entryType = entry.GetType();
                if (entryType.Name == "ExactEntry")
                {
                    ValidateExactEntry(entry, i, problems);
                }
                else if (entryType.Name == "PatternEntry")
                {
                    ValidatePatternEntry(entry, i, problems);
                }
                else
                {
                    problems.Add($"Entry at index {i} has unknown type '{entryType.Name}'.");
                }
            }

            return problems.AsReadOnly();
        }

        private static void ValidateExactEntry(object entry, int index, List<string> problems)
        {
            var typeName = entry.GetType().GetProperty("TypeName")?.GetValue(entry) as string;
            var methodName = entry.GetType().GetProperty("MethodName")?.GetValue(entry) as string;

            if (string.IsNullOrWhiteSpace(typeName))
            {
                problems.Add($"ExactEntry at index {index} has null or whitespace TypeName.");
            }

            // MethodName can be null, but if not null it should not be whitespace
            if (methodName != null && string.IsNullOrWhiteSpace(methodName))
            {
                problems.Add($"ExactEntry at index {index} has whitespace MethodName.");
            }
        }

        private static void ValidatePatternEntry(object entry, int index, List<string> problems)
        {
            var pattern = entry.GetType().GetProperty("Pattern")?.GetValue(entry) as string;
            var regex = entry.GetType().GetProperty("Regex")?.GetValue(entry);

            if (string.IsNullOrWhiteSpace(pattern))
            {
                problems.Add($"PatternEntry at index {index} has null or whitespace Pattern.");
            }

            if (regex == null)
            {
                problems.Add($"PatternEntry at index {index} has null Regex.");
            }
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
