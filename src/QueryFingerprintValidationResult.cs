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
		public string CommandTextHash { get; } = string.Empty;

		/// <summary>
		/// Gets the normalized SQL representation of the query.
		/// </summary>
		public string NormalizedSql { get; } = string.Empty;

		/// <summary>
		/// Gets the call site information where the query was executed.
		/// </summary>
		public string CallSite { get; } = string.Empty;

		/// <summary>
		/// Gets a value indicating whether the query fingerprint is valid.
		/// This is derived from <see cref="ValidationErrors"/> being empty.
		/// </summary>
		public bool IsValid => ValidationErrors.Count == 0;

		/// <summary>
		/// Gets an array of validation error messages if the query fingerprint is invalid.
		/// If this array is empty, the result is valid.
		/// </summary>
		public IReadOnlyList<string> ValidationErrors { get; } = Array.Empty<string>();

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryFingerprintValidationResult"/> class for successful validation.
		/// </summary>
		/// <param name="commandTextHash">The hash of the command text.</param>
		/// <param name="normalizedSql">The normalized SQL representation.</param>
		/// <param name="callSite">The call site information.</param>
		public QueryFingerprintValidationResult(string commandTextHash, string normalizedSql, string callSite)
		{
			CommandTextHash = commandTextHash ?? throw new ArgumentNullException(nameof(commandTextHash));
			NormalizedSql = normalizedSql ?? throw new ArgumentNullException(nameof(normalizedSql));
			CallSite = callSite ?? throw new ArgumentNullException(nameof(callSite));
		}

		/// <summary>
		/// Parameterless constructor for serialization.
		/// </summary>
		private QueryFingerprintValidationResult()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryFingerprintValidationResult"/> class for failed validation.
		/// </summary>
		/// <param name="commandTextHash">The hash of the command text.</param>
		/// <param name="normalizedSql">The normalized SQL representation.</param>
		/// <param name="callSite">The call site information.</param>
		/// <param name="validationErrors">The validation error messages.</param>
		public QueryFingerprintValidationResult(string commandTextHash, string normalizedSql, string callSite, IReadOnlyList<string> validationErrors)
			: this(commandTextHash, normalizedSql, callSite)
		{
			ValidationErrors = validationErrors ?? throw new ArgumentNullException(nameof(validationErrors));
		}

		/// <summary>
		/// Creates a successful validation result.
		/// </summary>
		/// <param name="commandTextHash">The hash of the command text.</param>
		/// <param name="normalizedSql">The normalized SQL representation.</param>
		/// <param name="callSite">The call site information.</param>
		/// <returns>A successful validation result.</returns>
		public static QueryFingerprintValidationResult Success(string commandTextHash, string normalizedSql, string callSite)
		=> new(commandTextHash, normalizedSql, callSite);

		/// <summary>
		/// Creates a failed validation result.
		/// </summary>
		/// <param name="commandTextHash">The hash of the command text.</param>
		/// <param name="normalizedSql">The normalized SQL representation.</param>
		/// <param name="callSite">The call site information.</param>
		/// <param name="validationErrors">The validation error messages.</param>
		/// <returns>A failed validation result.</returns>
		public static QueryFingerprintValidationResult Failure(string commandTextHash, string normalizedSql, string callSite, params string[] validationErrors)
		=> new(commandTextHash, normalizedSql, callSite, validationErrors);

	}
}