namespace EfCoreNPlusOneGuard
{
    public sealed class QueryFingerprintValidationResult
    {
        public string CommandTextHash { get; init; } = string.Empty;

        public string NormalizedSql { get; init; } = string.Empty;

        public string CallSite { get; init; } = string.Empty;

        public bool IsValid { get; init; }

        public string[] ValidationErrors { get; init; } = Array.Empty<string>();
    }
}
