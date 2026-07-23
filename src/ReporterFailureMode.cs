namespace EfCoreNPlusOneGuard;

/// <summary>
/// Controls how a file-based <see cref="IIncidentReporter"/> behaves once it has
/// exhausted its write retries and still cannot persist an incident, e.g. because the
/// target file is locked by a log shipper or antivirus, permissions were revoked, or
/// the disk is full.
/// </summary>
public enum ReporterFailureMode
{
    /// <summary>
    /// Swallow the failure without logging anything. The incident is dropped and only
    /// counted; use this when reporter availability is not important enough to warrant
    /// log noise.
    /// </summary>
    Silent,

    /// <summary>
    /// Swallow the failure but emit a rate-limited <see cref="Microsoft.Extensions.Logging.ILogger"/>
    /// warning (at most once per configured interval) instead of logging every dropped
    /// incident. This is the default: the host application keeps running and the operator
    /// still finds out that something is wrong.
    /// </summary>
    LogOnce,

    /// <summary>
    /// Propagate the write failure to the caller instead of degrading gracefully. Use this
    /// only when incident reporting is considered critical and the host application should
    /// fail loudly rather than silently drop incidents.
    /// </summary>
    Throw,
}
