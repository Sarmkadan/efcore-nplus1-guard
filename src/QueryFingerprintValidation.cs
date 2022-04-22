#nullable enable
using System;
using System.Collections.Generic;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides validation helpers for <see cref="QueryFingerprint"/> instances.
    /// </summary>
    public static class QueryFingerprintValidation
    {
        /// <summary>
        /// Validates a <see cref="QueryFingerprint"/> instance and returns a list of human-readable problems.
        /// </summary>
        /// <param name="value">The fingerprint to validate.</param>
        /// <returns>An empty list if valid; otherwise, a list of validation error messages.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static IReadOnlyList<string> Validate(this QueryFingerprint? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(value.CommandTextHash))
            {
                errors.Add("CommandTextHash cannot be null or whitespace.");
            }
            else if (value.CommandTextHash.Length != 64)
            {
                errors.Add("CommandTextHash must be a 64-character SHA256 hash.");
            }

            if (string.IsNullOrWhiteSpace(value.NormalizedSql))
            {
                errors.Add("NormalizedSql cannot be null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(value.CallSite))
            {
                errors.Add("CallSite cannot be null or whitespace.");
            }

            return errors;
        }

        /// <summary>
        /// Determines whether the specified <see cref="QueryFingerprint"/> is valid.
        /// </summary>
        /// <param name="value">The fingerprint to check.</param>
        /// <returns>True if valid; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool IsValid(this QueryFingerprint? value) => Validate(value).Count == 0;

        /// <summary>
        /// Ensures that the specified <see cref="QueryFingerprint"/> is valid, throwing an <see cref="ArgumentNullException"/> if it is null or an <see cref="ArgumentException"/> if it is invalid.
        /// </summary>
        /// <param name="value">The fingerprint to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when the fingerprint is invalid.</exception>
        public static void EnsureValid(this QueryFingerprint? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var errors = Validate(value);
            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    $"QueryFingerprint is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
            }
        }
    }
}