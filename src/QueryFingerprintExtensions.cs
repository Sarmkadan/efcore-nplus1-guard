using System;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides extension methods for <see cref="QueryFingerprint"/>.
    /// </summary>
    public static class QueryFingerprintExtensions
    {
        /// <summary>
        /// Checks if two <see cref="QueryFingerprint"/> instances represent the same underlying SQL query,
        /// regardless of the call site.
        /// </summary>
        /// <param name="fingerprint">The first fingerprint.</param>
        /// <param name="other">The second fingerprint.</param>
        /// <returns><c>true</c> if both fingerprints have the same SQL hash; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fingerprint"/> or <paramref name="other"/> is null.</exception>
        public static bool IsEquivalentSql(this QueryFingerprint fingerprint, QueryFingerprint other)
        {
            ArgumentNullException.ThrowIfNull(fingerprint);
            ArgumentNullException.ThrowIfNull(other);

            return fingerprint.CommandTextHash == other.CommandTextHash;
        }

        /// <summary>
        /// Returns a formatted string representation of the fingerprint for logging purposes.
        /// </summary>
        /// <param name="fingerprint">The fingerprint.</param>
        /// <returns>A string containing the shortened hash and the call site.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fingerprint"/> is null.</exception>
        public static string ToLogString(this QueryFingerprint fingerprint)
        {
            ArgumentNullException.ThrowIfNull(fingerprint);

            // Using the first 8 characters of the hash for identification.
            return $"Query({fingerprint.CommandTextHash.Substring(0, Math.Min(8, fingerprint.CommandTextHash.Length))}) at {fingerprint.CallSite}";
        }
    }
}
