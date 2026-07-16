namespace EfCoreNPlusOneGuard;

/// <summary>
/// A sink for detected N+1 incidents. Implementations decide where the incident goes
/// (text log, JSON Lines file, custom telemetry, etc.). Implementations must be safe to
/// call from the <c>onDetected</c> callback, i.e. from whatever thread executed the query.
/// </summary>
public interface IIncidentReporter
{
    /// <summary>
    /// Reports a single detected incident.
    /// </summary>
    /// <param name="incident">The incident to report.</param>
    void Report(NPlusOneIncident incident);
}
