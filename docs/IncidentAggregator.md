# IncidentAggregator

A utility for aggregating and analyzing N+1 query incidents detected during runtime. It collects incidents, groups them by fingerprint, and provides summary statistics and top offenders for diagnostics.

## API

### `public Summary GetScanSummary()`

Returns a summary of the current scan state, including total incident count, unique fingerprints, and top offenders.

- **Returns**: `Summary` – A record containing aggregated scan metrics.
- **Throws**: Never throws exceptions.

---

### `public void Add(NPlusOneIncident incident)`

Adds a new N+1 incident to the aggregator.

- **Parameters**:
  - `incident` – The incident to add.
- **Throws**: Never throws exceptions.

---

### `public IReadOnlyDictionary<string, int> CountsByFingerprint`

Gets a read-only dictionary mapping fingerprints to the number of incidents with that fingerprint.

- **Returns**: `IReadOnlyDictionary<string, int>` – Fingerprint counts.
- **Throws**: Never throws exceptions.

---

### `public IReadOnlyList<NPlusOneIncident> All`

Gets a read-only list of all incidents collected so far.

- **Returns**: `IReadOnlyList<NPlusOneIncident>` – All incidents.
- **Throws**: Never throws exceptions.

---
### `public string BuildSummaryText()`

Generates a human-readable summary of the current scan state.

- **Returns**: `string` – A formatted summary string.
- **Throws**: Never throws exceptions.

---
### `public IReadOnlyList<TopOffender> GetTopOffenders(int count = 5)`

Returns the top offenders by incident count.

- **Parameters**:
  - `count` – Maximum number of top offenders to return (default: 5).
- **Returns**: `IReadOnlyList<TopOffender>` – Sorted list of top offenders.
- **Throws**: Never throws exceptions.

---
### `public void Clear()`

Removes all collected incidents and resets the aggregator state.

- **Throws**: Never throws exceptions.

## Usage
