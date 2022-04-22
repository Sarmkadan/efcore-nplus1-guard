namespace EfCoreNPlusOneGuard;

/// <summary>
/// Collects N+1 incidents in-memory and groups them by fingerprint.
/// Thread-safe implementation.
/// </summary>
public sealed class IncidentAggregator
{
    private readonly object _lock = new();
    private readonly Dictionary<string, List<NPlusOneIncident>> _incidentsByFingerprint = new();
    private readonly List<NPlusOneIncident> _allIncidents = [];

    /// <summary>
    /// Adds an incident to the aggregator.
    /// </summary>
    /// <param name="incident">The incident to add.</param>
    public void Add(NPlusOneIncident incident)
    {
        if (incident is null)
        {
            throw new ArgumentNullException(nameof(incident));
        }

        lock (_lock)
        {
            _allIncidents.Add(incident);

            var fingerprint = incident.SqlQuery ?? string.Empty;
            if (!_incidentsByFingerprint.TryGetValue(fingerprint, out var list))
            {
                list = [];
                _incidentsByFingerprint[fingerprint] = list;
            }

            list.Add(incident);
        }
    }

    /// <summary>
    /// Gets the count of incidents grouped by fingerprint.
    /// </summary>
    /// <returns>A read-only dictionary mapping fingerprints to incident counts.</returns>
    public IReadOnlyDictionary<string, int> CountsByFingerprint()
    {
        lock (_lock)
        {
            var result = new Dictionary<string, int>(_incidentsByFingerprint.Count);
            foreach (var kvp in _incidentsByFingerprint)
            {
                result[kvp.Key] = kvp.Value.Count;
            }

            return result;
        }
    }

    /// <summary>
    /// Gets all collected incidents.
    /// </summary>
    /// <returns>A read-only list of all incidents.</returns>
    public IReadOnlyList<NPlusOneIncident> All()
    {
        lock (_lock)
        {
            return _allIncidents.AsReadOnly();
        }
    }

    /// <summary>
    /// Builds a human-readable summary text of the collected incidents.
    /// </summary>
    /// <returns>A summary string.</returns>
    public string BuildSummaryText()
    {
        lock (_lock)
        {
            if (_allIncidents.Count == 0)
            {
                return "No N+1 incidents detected.";
            }

            var totalCount = _allIncidents.Count;
            var uniqueCount = _incidentsByFingerprint.Count;
            var groupedCount = _incidentsByFingerprint
                .Where(kvp => kvp.Value.Count > 1)
                .Sum(kvp => kvp.Value.Count);

            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"N+1 Guard Summary ({DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC})");
            builder.AppendLine();
            builder.AppendLine($"Total incidents: {totalCount}");
            builder.AppendLine($"Unique query fingerprints: {uniqueCount}");
            builder.AppendLine($"Total duplicate queries: {groupedCount}");
            builder.AppendLine();

            if (uniqueCount > 0)
            {
                builder.AppendLine("Top fingerprints by occurrence:");
                foreach (var kvp in _incidentsByFingerprint
                    .OrderByDescending(kvp => kvp.Value.Count)
                    .Take(10))
                {
                    builder.AppendLine($"  {kvp.Value.Count}x: {kvp.Key}");
                }
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// Clears all collected incidents.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _incidentsByFingerprint.Clear();
            _allIncidents.Clear();
        }
    }
}