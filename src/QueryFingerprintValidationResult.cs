namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Represents the validation result for a query fingerprint, containing the normalized SQL,
    /// command text hash, call site information, and any validation errors encountered.
    /// </summary>
    public sealed class QueryFingerprintValidationResult
    {
        /// <summary>
        /// Gets the hash of the command text for the query fingerprint.
        /// </summary>
        public string CommandTextHash { get; init; } = string.Empty;

        /// <summary>
        /// Gets the normalized SQL representation of the query.
        /// </summary>
        public string NormalizedSql { get; init; } = string.Empty;

        /// <summary>
        /// Gets the call site information where the query was executed.
        /// </summary>
        public string CallSite { get; init; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the query fingerprint is valid.
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// Gets an array of validation error messages if the query fingerprint is invalid.
        /// </summary>
        public string[] ValidationErrors { get; init; } = Array.Empty<string>();
    }
}