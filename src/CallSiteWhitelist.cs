#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// A whitelist of call‑site signatures for which the N+1 guard should be ignored.
    /// </summary>
    public sealed class CallSiteWhitelist
    {
        private sealed class ExactEntry
        {
            public string TypeName { get; }
            public string? MethodName { get; }

            public ExactEntry(string typeName, string? methodName)
            {
                TypeName = typeName;
                MethodName = methodName;
            }
        }

        private sealed class PatternEntry
        {
            public string Pattern { get; }
            public Regex Regex { get; }

            public PatternEntry(string pattern)
            {
                Pattern = pattern;
                // Escape regex meta characters except '*', then replace '*' with '.*'
                var escaped = Regex.Escape(pattern).Replace(@"\*", ".*");
                Regex = new Regex($"^{escaped}$", RegexOptions.Compiled);
            }
        }

        private readonly List<object> _entries = new();

        /// <summary>
        /// Adds an exact type/method pair to the whitelist.
        /// </summary>
        /// <param name="typeName">Fully qualified type name.</param>
        /// <param name="methodName">Optional method name. If null, any method of the type is whitelisted.</param>
        public void Add(string typeName, string? methodName = null)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("Type name cannot be null or whitespace.", nameof(typeName));

            _entries.Add(new ExactEntry(typeName, methodName));
        }

        /// <summary>
        /// Adds a wildcard pattern to the whitelist. Supports '*' as a wildcard.
        /// </summary>
        /// <param name="wildcardPattern">Pattern to match type names.</param>
        public void AddPattern(string wildcardPattern)
        {
            if (string.IsNullOrWhiteSpace(wildcardPattern))
                throw new ArgumentException("Pattern cannot be null or whitespace.", nameof(wildcardPattern));

            _entries.Add(new PatternEntry(wildcardPattern));
        }

        /// <summary>
        /// Determines whether any frame in the provided stack trace matches a whitelist entry.
        /// </summary>
        /// <param name="stackTrace">The stack trace string.</param>
        /// <returns>True if a match is found; otherwise false.</returns>
        public bool IsWhitelisted(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return false;

            foreach (var line in stackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                // Typical stack trace line: "at Namespace.Type.Method (at file:line)"
                // Extract the part after "at " and before the first '(' or space.
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("at "))
                    continue;

                var afterAt = trimmed.Substring(3).Trim();
                var endIndex = afterAt.IndexOf('(');
                if (endIndex < 0)
                    endIndex = afterAt.IndexOf(' ');
                if (endIndex < 0)
                    endIndex = afterAt.Length;

                var fullMethod = afterAt.Substring(0, endIndex).Trim();

                // Split into type and method
                var lastDot = fullMethod.LastIndexOf('.');
                if (lastDot < 0)
                    continue; // cannot parse

                var typeName = fullMethod.Substring(0, lastDot);
                var methodName = fullMethod.Substring(lastDot + 1);

                // Check exact entries
                foreach (var entry in _entries)
                {
                    if (entry is ExactEntry exact)
                    {
                        if (string.Equals(exact.TypeName, typeName, StringComparison.Ordinal))
                        {
                            if (exact.MethodName == null || string.Equals(exact.MethodName, methodName, StringComparison.Ordinal))
                                return true;
                        }
                    }
                    else if (entry is PatternEntry pattern)
                    {
                        if (pattern.Regex.IsMatch(typeName))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the number of entries in the whitelist.
        /// </summary>
        public int Count => _entries.Count;

        /// <summary>
        /// Clears all entries from the whitelist.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
        }
    }
}
