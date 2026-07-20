namespace EfCoreNPlusOneGuard;

/// <summary>
/// Represents the severity level of an N+1 incident.
/// </summary>
public enum NPlusOneSeverity
{
	/// <summary>
	/// Low severity - minor performance impact
	/// </summary>
	Low,

	/// <summary>
	/// Medium severity - noticeable performance impact
	/// </summary>
	Medium,

	/// <summary>
	/// High severity - significant performance impact
	/// </summary>
	High
}

/// <summary>
/// Represents a detected N+1 query incident containing details about the problematic SQL query,
/// its occurrence count, severity level, and stack trace for debugging.
/// </summary>
public class NPlusOneIncident
{
	/// <summary>
	/// Gets or sets the SQL query that triggered the N+1 incident.
	/// </summary>
	public string SqlQuery { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the number of times this N+1 pattern occurred.
	/// </summary>
	public int Count { get; set; }

	/// <summary>
	/// Gets or sets the severity level of this N+1 incident.
	/// </summary>
	public NPlusOneSeverity Severity { get; set; }

	/// <summary>
	/// Gets or sets the stack trace showing where the N+1 query was executed,
	/// used for debugging and identifying the source of the issue.
	/// </summary>
	public string StackTrace { get; set; } = string.Empty;

/// <summary>
/// Gets or sets the call site information (method name, file name, line number) for the N+1 query.
/// This is extracted from the stack trace and represents the first application frame outside of
/// EF Core, System, and this library's namespace.
/// </summary>
public string? CallSite { get; set; }
}
