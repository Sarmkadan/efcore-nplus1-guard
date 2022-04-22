#nullable enable

using System;
using System.Collections.Generic;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides extension methods for <see cref="CallSiteWhitelist"/> to enhance whitelist management capabilities.
    /// </summary>
    public static class CallSiteWhitelistExtensions
    {
        /// <summary>
        /// Adds multiple exact type/method pairs to the whitelist in a single call.
        /// </summary>
        /// <param name="whitelist">The whitelist instance.</param>
        /// <param name="entries">Collection of type/method pairs to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="whitelist"/> or <paramref name="entries"/> is null.</exception>
        public static void AddRange(this CallSiteWhitelist whitelist, IEnumerable<(string TypeName, string? MethodName)> entries)
        {
            ArgumentNullException.ThrowIfNull(whitelist);
            ArgumentNullException.ThrowIfNull(entries);

            foreach (var (typeName, methodName) in entries)
            {
                whitelist.Add(typeName, methodName);
            }
        }

        /// <summary>
        /// Adds multiple wildcard patterns to the whitelist in a single call.
        /// </summary>
        /// <param name="whitelist">The whitelist instance.</param>
        /// <param name="patterns">Collection of wildcard patterns to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="whitelist"/> or <paramref name="patterns"/> is null.</exception>
        public static void AddPatterns(this CallSiteWhitelist whitelist, IEnumerable<string> patterns)
        {
            ArgumentNullException.ThrowIfNull(whitelist);
            ArgumentNullException.ThrowIfNull(patterns);

            foreach (var pattern in patterns)
            {
                whitelist.AddPattern(pattern);
            }
        }

        /// <summary>
        /// Gets the number of entries in the whitelist.
        /// </summary>
        /// <param name="whitelist">The whitelist instance.</param>
        /// <returns>The number of entries in the whitelist.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="whitelist"/> is null.</exception>
        public static int GetEntryCount(this CallSiteWhitelist whitelist)
        {
            ArgumentNullException.ThrowIfNull(whitelist);
            return whitelist.Count;
        }

        /// <summary>
        /// Determines whether the whitelist is empty (contains no entries).
        /// </summary>
        /// <param name="whitelist">The whitelist instance.</param>
        /// <returns>True if the whitelist is empty; otherwise false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="whitelist"/> is null.</exception>
        public static bool IsEmpty(this CallSiteWhitelist whitelist)
        {
            ArgumentNullException.ThrowIfNull(whitelist);
            return whitelist.Count == 0;
        }

        /// <summary>
        /// Removes all entries from the whitelist.
        /// </summary>
        /// <param name="whitelist">The whitelist instance.</param>
        /// <returns>The number of entries removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="whitelist"/> is null.</exception>
        public static int ClearAll(this CallSiteWhitelist whitelist)
        {
            ArgumentNullException.ThrowIfNull(whitelist);
            var count = whitelist.Count;
            whitelist.Clear();
            return count;
        }
    }
}