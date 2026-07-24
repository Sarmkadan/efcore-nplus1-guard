#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// A whitelist of call‑site signatures for which the N+1 guard should be ignored.
    /// Supports exact type/method entries, glob-style wildcard patterns, and query
    /// fingerprint-hash entries, each with an optional expiry date so suppressions do
    /// not silently live forever.
    /// </summary>
    public sealed class CallSiteWhitelist
    {
        private abstract class Entry
        {
            /// <summary>UTC instant after which this entry is no longer honored, if any.</summary>
            public DateTimeOffset? ExpiresAtUtc { get; init; }

            /// <summary>Whether this entry has ever produced a match via <see cref="IsWhitelisted"/> or <see cref="IsFingerprintWhitelisted"/>.</summary>
            public bool Matched { get; set; }

            public bool IsExpired(DateTimeOffset now) => ExpiresAtUtc is { } expiry && now >= expiry;

            /// <summary>A human-readable label used for diagnostics/logging.</summary>
            public abstract string Describe();
        }

        private sealed class ExactEntry : Entry
        {
            public string TypeName { get; }
            public string? MethodName { get; }

            public ExactEntry(string typeName, string? methodName)
            {
                TypeName = typeName;
                MethodName = methodName;
            }

            public override string Describe() =>
                MethodName is null ? $"exact:{TypeName}" : $"exact:{TypeName}.{MethodName}";
        }

        private sealed class PatternEntry : Entry
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

            public override string Describe() => $"pattern:{Pattern}";
        }

        private sealed class FingerprintEntry : Entry
        {
            public string FingerprintHash { get; }

            public FingerprintEntry(string fingerprintHash) => FingerprintHash = fingerprintHash;

            public override string Describe() => $"fingerprint:{FingerprintHash}";
        }

        private readonly List<Entry> _entries = new();

        /// <summary>
        /// Adds an exact type/method pair to the whitelist.
        /// </summary>
        /// <param name="typeName">Fully qualified type name.</param>
        /// <param name="methodName">Optional method name. If null, any method of the type is whitelisted.</param>
        /// <param name="expiresAtUtc">Optional UTC instant after which this entry stops matching.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="typeName"/> is null or whitespace.</exception>
        public void Add(string typeName, string? methodName = null, DateTimeOffset? expiresAtUtc = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(typeName);

            _entries.Add(new ExactEntry(typeName, methodName) { ExpiresAtUtc = expiresAtUtc });
        }

        /// <summary>
        /// Adds a wildcard pattern to the whitelist. Supports '*' as a wildcard, e.g.
        /// <c>MyApp.Services.ReportService.*</c>.
        /// </summary>
        /// <param name="wildcardPattern">Pattern to match type names.</param>
        /// <param name="expiresAtUtc">Optional UTC instant after which this entry stops matching.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="wildcardPattern"/> is null or whitespace.</exception>
        public void AddPattern(string wildcardPattern, DateTimeOffset? expiresAtUtc = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(wildcardPattern);

            _entries.Add(new PatternEntry(wildcardPattern) { ExpiresAtUtc = expiresAtUtc });
        }

        /// <summary>
        /// Adds a query-fingerprint hash entry to the whitelist. Fingerprint hashes are stable
        /// across line-number shifts, unlike call-site strings, and are matched via
        /// <see cref="IsFingerprintWhitelisted"/> against <see cref="QueryFingerprint.CommandTextHash"/>.
        /// </summary>
        /// <param name="fingerprintHash">The fingerprint hash to whitelist.</param>
        /// <param name="expiresAtUtc">Optional UTC instant after which this entry stops matching.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="fingerprintHash"/> is null or whitespace.</exception>
        public void AddFingerprint(string fingerprintHash, DateTimeOffset? expiresAtUtc = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(fingerprintHash);

            _entries.Add(new FingerprintEntry(fingerprintHash) { ExpiresAtUtc = expiresAtUtc });
        }

        /// <summary>
        /// Determines whether any frame in the provided stack trace matches a non-expired
        /// exact or pattern whitelist entry. Matching entries are marked as matched for
        /// dead-suppression diagnostics.
        /// </summary>
        /// <param name="stackTrace">The stack trace string.</param>
        /// <returns>True if a match is found; otherwise false.</returns>
        public bool IsWhitelisted(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return false;

            var now = DateTimeOffset.UtcNow;

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

                foreach (var entry in _entries)
                {
                    if (entry.IsExpired(now))
                        continue;

                    switch (entry)
                    {
                        case ExactEntry exact when string.Equals(exact.TypeName, typeName, StringComparison.Ordinal)
                            && (exact.MethodName == null || string.Equals(exact.MethodName, methodName, StringComparison.Ordinal)):
                            exact.Matched = true;
                            return true;
                        case PatternEntry pattern when pattern.Regex.IsMatch(typeName):
                            pattern.Matched = true;
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the given query fingerprint hash matches a non-expired
        /// fingerprint whitelist entry. Matching entries are marked as matched for
        /// dead-suppression diagnostics.
        /// </summary>
        /// <param name="fingerprintHash">The fingerprint hash to check, typically <see cref="QueryFingerprint.CommandTextHash"/>.</param>
        /// <returns>True if a fingerprint entry matches; otherwise false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="fingerprintHash"/> is null or whitespace.</exception>
        public bool IsFingerprintWhitelisted(string fingerprintHash)
        {
            ArgumentException.ThrowIfNullOrEmpty(fingerprintHash);

            var now = DateTimeOffset.UtcNow;

            foreach (var entry in _entries)
            {
                if (entry is FingerprintEntry fp && !fp.IsExpired(now) &&
                    string.Equals(fp.FingerprintHash, fingerprintHash, StringComparison.Ordinal))
                {
                    fp.Matched = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns descriptions of entries that are already expired as of <paramref name="asOfUtc"/>.
        /// Intended for startup diagnostics so dead suppressions can be cleaned up.
        /// </summary>
        /// <param name="asOfUtc">The UTC instant to evaluate expiry against.</param>
        /// <returns>A read-only list of human-readable entry descriptions.</returns>
        public IReadOnlyList<string> GetExpiredEntries(DateTimeOffset asOfUtc) =>
            _entries.Where(e => e.IsExpired(asOfUtc)).Select(e => e.Describe()).ToList();

        /// <summary>
        /// Returns descriptions of entries that have never produced a match since this
        /// instance was created (or last reloaded). Intended for startup diagnostics so
        /// dead suppressions can be cleaned up.
        /// </summary>
        /// <returns>A read-only list of human-readable entry descriptions.</returns>
        public IReadOnlyList<string> GetNeverMatchedEntries() =>
            _entries.Where(e => !e.Matched).Select(e => e.Describe()).ToList();

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
