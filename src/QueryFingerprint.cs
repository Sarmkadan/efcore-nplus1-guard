#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
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

        [JsonConstructor]
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
        /// Performs a single, allocation-conscious pass over the input span instead of chaining several
        /// <see cref="Regex"/> replacements (which each allocate an intermediate string and pay regex
        /// engine overhead), keeping this on the per-query hot path cheap.
        /// </summary>
        private static string NormalizeSql(string sql)
        {
            ReadOnlySpan<char> span = sql.AsSpan();
            var sb = new StringBuilder(span.Length);
            var lastAppendedWasSpace = true; // suppress leading whitespace

            for (var i = 0; i < span.Length; i++)
            {
                var c = span[i];

                if (c == '\'')
                {
                    // Consume a '...'-delimited string literal (matches the previous '[^']*' regex).
                    var j = i + 1;
                    while (j < span.Length && span[j] != '\'')
                    {
                        j++;
                    }

                    if (j < span.Length)
                    {
                        sb.Append('?');
                        lastAppendedWasSpace = false;
                        i = j;
                        continue;
                    }

                    // No closing quote found; fall through and copy the character verbatim.
                    sb.Append(c);
                    lastAppendedWasSpace = false;
                    continue;
                }

                if ((c == '@' || c == ':') && i + 1 < span.Length && IsWordChar(span[i + 1]))
                {
                    var j = i + 1;
                    while (j < span.Length && IsWordChar(span[j]))
                    {
                        j++;
                    }

                    sb.Append('?');
                    lastAppendedWasSpace = false;
                    i = j - 1;
                    continue;
                }

                if (c == '?')
                {
                    var j = i + 1;
                    while (j < span.Length && char.IsAsciiDigit(span[j]))
                    {
                        j++;
                    }

                    sb.Append('?');
                    lastAppendedWasSpace = false;
                    i = j - 1;
                    continue;
                }

                if (char.IsAsciiDigit(c))
                {
                    var j = i + 1;
                    while (j < span.Length && char.IsAsciiDigit(span[j]))
                    {
                        j++;
                    }

                    sb.Append('?');
                    lastAppendedWasSpace = false;
                    i = j - 1;
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    if (!lastAppendedWasSpace)
                    {
                        sb.Append(' ');
                        lastAppendedWasSpace = true;
                    }

                    continue;
                }

                sb.Append(char.ToLowerInvariant(c));
                lastAppendedWasSpace = false;
            }

            // Trim a single trailing collapsed space, if any.
            if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
            {
                sb.Length--;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Determines whether a character is a "word" character (letter, digit, or underscore) for the
        /// purposes of matching <c>@name</c> / <c>:name</c> parameter placeholders.
        /// </summary>
        /// <param name="c">The character to test.</param>
        /// <returns><see langword="true"/> if the character is a word character; otherwise <see langword="false"/>.</returns>
        private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

        /// <summary>
        /// Computes the SHA256 hash of the given input string.
        /// Uses <see cref="SHA256.HashData(ReadOnlySpan{byte})"/> and <see cref="Convert.ToHexStringLower(ReadOnlySpan{byte})"/>
        /// to avoid the disposable hash algorithm instance and the manual hex-encoding loop, minimizing
        /// allocations on the per-query hot path.
        /// </summary>
        private static string ComputeSha256Hash(string input)
        {
            ReadOnlySpan<char> inputSpan = input.AsSpan();
            var byteCount = Encoding.UTF8.GetByteCount(inputSpan);

            Span<byte> bytes = byteCount <= 512 ? stackalloc byte[byteCount] : new byte[byteCount];
            Encoding.UTF8.GetBytes(inputSpan, bytes);

            Span<byte> hashBytes = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(bytes, hashBytes);

            return Convert.ToHexStringLower(hashBytes);
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
