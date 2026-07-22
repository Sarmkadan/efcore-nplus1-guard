#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Immutable representation of a query fingerprint.
    /// </summary>
    public sealed class QueryFingerprint : IEquatable<QueryFingerprint>
    {
        /// <summary>
        /// SHA256 hash of the normalized SQL.
        /// </summary>
        public string CommandTextHash { get; }

        /// <summary>
        /// Normalized SQL string (literals/parameters removed, whitespace collapsed, lower‑cased).
        /// </summary>
        public string NormalizedSql { get; }

        /// <summary>
        /// Call site information (e.g. stack trace or method name).
        /// </summary>
        public string CallSite { get; }

        private QueryFingerprint(string commandTextHash, string normalizedSql, string callSite)
        {
            ArgumentNullException.ThrowIfNull(commandTextHash);
            ArgumentNullException.ThrowIfNull(normalizedSql);
            ArgumentNullException.ThrowIfNull(callSite);

            CommandTextHash = commandTextHash;
            NormalizedSql = normalizedSql;
            CallSite = callSite;
        }

        /// <summary>
        /// Creates a new <see cref="QueryFingerprint"/> from raw command text and call site.
        /// </summary>
        /// <param name="commandText">The raw SQL command text.</param>
        /// <param name="callSite">The call site information.</param>
        /// <returns>A new <see cref="QueryFingerprint"/> instance.</returns>
        public static QueryFingerprint Create(string commandText, string callSite)
        {
            ArgumentNullException.ThrowIfNull(commandText);
            ArgumentNullException.ThrowIfNull(callSite);

            var normalized = NormalizeSql(commandText);
            var hash = ComputeSha256Hash(normalized);

            return new QueryFingerprint(hash, normalized, callSite);
        }

        /// <summary>
        /// Normalizes SQL by removing literals/parameters, collapsing whitespace, and converting to lower case.
        /// Uses Regex for the primary normalization which is both efficient and maintainable.
        /// </summary>
        private static string NormalizeSql(string sql)
        {
            // Replace parameter placeholders (@p0, :p0, ?0, etc.) with a single placeholder.
            var paramPattern = @"(@\w+|:\w+|\?\d*|\d+|'[^']*')";
            var withoutParams = Regex.Replace(sql, paramPattern, "?");

            // Collapse all whitespace sequences into a single space.
            var collapsed = Regex.Replace(withoutParams, @"\s+", " ").Trim();

            // Convert to lower case for case‑insensitive comparison.
            return collapsed.ToLowerInvariant();
        }

        /// <summary>
        /// Computes the SHA256 hash of the given input string.
        /// Uses efficient span-based byte conversion to minimize allocations.
        /// </summary>
        private static string ComputeSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();

            // Use span-based APIs for efficient processing
            ReadOnlySpan<char> inputSpan = input.AsSpan();
            int byteCount = Encoding.UTF8.GetByteCount(inputSpan);
            byte[] bytes = new byte[byteCount];
            Encoding.UTF8.GetBytes(inputSpan, bytes);

            var hashBytes = sha256.ComputeHash(bytes);
            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Strips compiler-generated frames from the call site string.
        /// </summary>
        private static string StripCompilerFrames(string callSite)
        {
            var frames = callSite.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var strippedFrames = frames.Where(frame => !frame.Contains("d__"));
            return string.Join(Environment.NewLine, strippedFrames);
        }

        public override bool Equals(object? obj) => Equals(obj as QueryFingerprint);

        public bool Equals(QueryFingerprint? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return CommandTextHash == other.CommandTextHash && NormalizedSql == other.NormalizedSql && CallSite == StripCompilerFrames(other.CallSite);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CommandTextHash, NormalizedSql, CallSite);
        }

        public static bool operator ==(QueryFingerprint? left, QueryFingerprint? right) =>
            EqualityComparer<QueryFingerprint>.Default.Equals(left, right);

        public static bool operator !=(QueryFingerprint? left, QueryFingerprint? right) =>
            !(left == right);
    }
}
