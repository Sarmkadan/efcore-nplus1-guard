using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Collects N+1 incidents in-memory and groups them by fingerprint.
/// Thread-safe implementation with bounded capacity and time-window filtering.
/// </summary>
public sealed class IncidentAggregator
{
    private readonly ConcurrentDictionary<string, List<NPlusOneIncident>> _incidentsByFingerprint = new();
    private readonly List<NPlusOneIncident> _allIncidents = [];

    // Tracks the most recent time a fingerprint was seen for LRU eviction.
    private readonly ConcurrentDictionary<string, DateTime> _lastSeenByFingerprint = new();

    // Configuration options
    private readonly int _maxTrackedFingerprints;
    private readonly TimeSpan? _timeWindow;

    // LRU cache tracking
    private readonly LinkedList<string> _lruList = new();
    private readonly object _lruLock = new();

    /// <summary>
    /// Represents a fingerprint offender with its occurrence count and the last time it was seen.
    /// </summary>
    /// <param name="Fingerprint">The query fingerprint.</param>
    /// <param name="Count">The number of times this fingerprint occurred.</param>
    /// <param name="LastSeen">The last time this fingerprint was seen in UTC.</param>
    public sealed record TopOffender(string Fingerprint, int Count, DateTime LastSeen)
    {
        /// <summary>
        /// Gets whether this offender is within the configured time window.
        /// </summary>
        /// <param name="timeWindow">The time window to check against. If null, always returns true.</param>
        /// <returns>True if the offender is within the time window; otherwise, false.</returns>
        public bool IsWithinTimeWindow(TimeSpan? timeWindow)
        {
            if (timeWindow is null)
            {
                return true;
            }

            var age = DateTime.UtcNow - LastSeen;
            return age <= timeWindow.Value;
        }
    }

    /// <summary>
    /// Represents a scan summary containing aggregated incident data.
    /// </summary>
    /// <param name="TotalQueries">Total number of queries tracked.</param>
    /// <param name="UniqueFingerprints">Number of unique query fingerprints.</param>
    /// <param name="TopOffenders">Top 5 fingerprint offenders by occurrence count.</param>
    public sealed record Summary(int TotalQueries, int UniqueFingerprints, IReadOnlyList<TopOffender> TopOffenders);

    /// <summary>
    /// Initializes a new instance of the <see cref="IncidentAggregator"/> class with default settings.
    /// </summary>
    public IncidentAggregator()
        : this(maxTrackedFingerprints: 10_000, timeWindow: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IncidentAggregator"/> class with custom settings.
    /// </summary>
    /// <param name="maxTrackedFingerprints">Maximum number of unique fingerprints to track before evicting old entries. Default is 10,000.</param>
    /// <param name="timeWindow">Optional time window for filtering incidents. If null, all incidents are tracked indefinitely.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if maxTrackedFingerprints is less than 1.</exception>
    public IncidentAggregator(int maxTrackedFingerprints = 10_000, TimeSpan? timeWindow = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTrackedFingerprints, 1);

        _maxTrackedFingerprints = maxTrackedFingerprints;
        _timeWindow = timeWindow;
    }

    /// <summary>
    /// Gets a summary of the collected incidents including total count, unique fingerprints,
    /// and top offenders. This data can be consumed by reporters to provide scan summaries.
    /// </summary>
    /// <param name="timeWindow">Optional time window to filter incidents. If null, uses the aggregator's configured time window.</param>
    /// <returns>A summary of the collected incidents.</returns>
    public Summary GetScanSummary(TimeSpan? timeWindow = null)
    {
        var effectiveTimeWindow = timeWindow ?? _timeWindow;
        var totalCount = _allIncidents.Count;
        var uniqueCount = _incidentsByFingerprint.Count;
        var offenders = GetTopOffenders(5, effectiveTimeWindow);

        return new Summary(
            TotalQueries: totalCount,
            UniqueFingerprints: uniqueCount,
            TopOffenders: offenders
        );
    }

    /// <summary>
    /// Adds an incident to the aggregator.
    /// </summary>
    /// <param name="incident">The incident to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if incident is null.</exception>
    public void Add(NPlusOneIncident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        var fingerprint = incident.SqlQuery ?? string.Empty;
        var now = DateTime.UtcNow;

        lock (_lruLock)
        {
            // Update LRU tracking
            _lruList.Remove(fingerprint);
            _lruList.AddLast(fingerprint);

            // Evict if we've exceeded capacity
            if (_incidentsByFingerprint.Count >= _maxTrackedFingerprints && _incidentsByFingerprint.TryGetValue(fingerprint, out _))
            {
                // Fingerprint already exists, no eviction needed
            }
            else if (_incidentsByFingerprint.Count >= _maxTrackedFingerprints)
            {
                // Evict the least recently used fingerprint
                var lruFingerprint = _lruList.First?.Value;
                if (lruFingerprint != null && _incidentsByFingerprint.TryRemove(lruFingerprint, out var removedList))
                {
                    _lastSeenByFingerprint.TryRemove(lruFingerprint, out _);
                    _lruList.RemoveFirst();
                }
            }
        }

        // Add to the global list
        _allIncidents.Add(incident);

        // Add to the fingerprint-specific list
        var added = _incidentsByFingerprint.AddOrUpdate(
            fingerprint,
            [incident],
            (_, existingList) =>
            {
                existingList.Add(incident);
                return existingList;
            });

        // Update the last-seen timestamp
        _lastSeenByFingerprint[fingerprint] = now;
    }

    /// <summary>
    /// Gets the count of incidents grouped by fingerprint.
    /// </summary>
    /// <returns>A read‑only dictionary mapping fingerprints to incident counts.</returns>
    public IReadOnlyDictionary<string, int> CountsByFingerprint()
    {
        var result = new Dictionary<string, int>(_incidentsByFingerprint.Count);
        foreach (var kvp in _incidentsByFingerprint)
        {
            result[kvp.Key] = kvp.Value.Count;
        }

        return result;
    }

    /// <summary>
    /// Gets all collected incidents.
    /// </summary>
    /// <returns>A read‑only list of all incidents.</returns>
    public IReadOnlyList<NPlusOneIncident> All()
    {
        return _allIncidents.AsReadOnly();
    }

    /// <summary>
    /// Builds a human‑readable summary text of the collected incidents.
    /// </summary>
    /// <param name="timeWindow">Optional time window to filter incidents. If null, uses the aggregator's configured time window.</param>
    /// <returns>A summary string.</returns>
    public string BuildSummaryText(TimeSpan? timeWindow = null)
    {
        var effectiveTimeWindow = timeWindow ?? _timeWindow;

        if (_allIncidents.Count == 0)
        {
            return "No N+1 incidents detected.";
        }

        var totalCount = _allIncidents.Count;
        var uniqueCount = _incidentsByFingerprint.Count;
        var groupedCount = _incidentsByFingerprint
            .Where(kvp => kvp.Value.Count > 1)
            .Sum(kvp => kvp.Value.Count);

        var builder = new StringBuilder();
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
                builder.AppendLine($" {kvp.Value.Count}x: {kvp.Key}");
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns the top <paramref name="n"/> fingerprints with the highest total execution counts,
    /// ordered descending. Each entry includes the fingerprint, its count, and the last‑seen timestamp.
    /// </summary>
    /// <param name="n">The maximum number of offenders to return.</param>
    /// <param name="timeWindow">Optional time window to filter incidents. If null, all incidents are considered.</param>
    /// <returns>A read‑only list of <see cref="TopOffender"/> records.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if n is less than 0.</exception>
    public IReadOnlyList<TopOffender> GetTopOffenders(int n, TimeSpan? timeWindow = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        var effectiveTimeWindow = timeWindow ?? _timeWindow;

        var offenders = _incidentsByFingerprint
            .Select(kvp => new TopOffender(
                Fingerprint: kvp.Key,
                Count: kvp.Value.Count,
                LastSeen: _lastSeenByFingerprint.TryGetValue(kvp.Key, out var dt) ? dt : DateTime.MinValue))
            .Where(o => !effectiveTimeWindow.HasValue || o.IsWithinTimeWindow(effectiveTimeWindow))
            .OrderByDescending(o => o.Count)
            .ThenByDescending(o => o.LastSeen)
            .Take(n)
            .ToList();

        return offenders;
    }

    /// <summary>
    /// Clears all collected incidents.
    /// </summary>
    public void Clear()
    {
        lock (_lruLock)
        {
            _incidentsByFingerprint.Clear();
            _allIncidents.Clear();
            _lastSeenByFingerprint.Clear();
            _lruList.Clear();
        }
    }
}
