# QueryStatistics

`QueryStatistics` collects and exposes metrics about LINQ queries executed through a `DbContext` that is being monitored by the EF Core N+1 guard. It records each query’s SQL text, execution count, and cumulative duration, enabling detection of N+1 patterns and other query inefficiencies.

## API

### `public void Record(string sql, TimeSpan duration)`
Records an occurrence of a query with the given SQL text and execution duration. If the SQL text has been seen before, its counter is incremented and its total duration is increased; otherwise a new entry is created.

| Parameter   | Type      | Purpose                                      |
|-------------|-----------|----------------------------------------------|
| `sql`       | `string`  | The normalized SQL text of the executed query. |
| `duration`  | `TimeSpan`| The time taken to execute this occurrence.   |

**Returns:** nothing.

**Throws:** `ArgumentNullException` when `sql` is `null`.

---

### `public IReadOnlyList<QueryStatEntry> TopByCount { get; }`
Returns a read-only list of all recorded query entries, ordered by execution count descending (most frequently executed query first). The list is a snapshot created at the time of property access.

**Returns:** `IReadOnlyList<QueryStatEntry>` – the current query statistics sorted by count.

**Throws:** nothing.

---

### `public void Reset()`
Clears all recorded query statistics, resetting the internal state to empty. After calling this, `Count` returns zero and `TopByCount` returns an empty list.

**Returns:** nothing.

**Throws:** nothing.

---

### `public string Sql { get; }`
Gets the SQL text of the query represented by a single `QueryStatEntry`. This member is exposed on `QueryStatEntry`, not on `QueryStatistics` itself.

**Returns:** `string` – the normalized SQL text for this entry.

**Throws:** nothing.

---

### `public int Count { get; }`
Gets the number of times the query represented by a single `QueryStatEntry` has been executed. This member is exposed on `QueryStatEntry`, not on `QueryStatistics` itself.

On `QueryStatistics`, `Count` returns the total number of *distinct* queries recorded (i.e., the number of entries).

**Returns:** `int` – the execution count for the entry, or the distinct query count for the statistics collection.

**Throws:** nothing.

---

### `public TimeSpan TotalDuration { get; }`
Gets the cumulative execution time for the query represented by a single `QueryStatEntry`. This member is exposed on `QueryStatEntry`, not on `QueryStatistics` itself.

**Returns:** `TimeSpan` – the sum of all recorded durations for this query entry.

**Throws:** nothing.

---

### `QueryStatEntry`
A nested or associated type representing a single distinct query’s recorded statistics. It exposes the following members:

- `string Sql` – the normalized SQL text.
- `int Count` – the number of times this query was executed.
- `TimeSpan TotalDuration` – the cumulative execution time.

Instances are obtained through `TopByCount` or other enumeration of `QueryStatistics`.

## Usage

### Example 1: Basic recording and inspection

```csharp
var stats = new QueryStatistics();

stats.Record("SELECT * FROM Orders WHERE CustomerId = @p0", TimeSpan.FromMilliseconds(12));
stats.Record("SELECT * FROM Orders WHERE CustomerId = @p0", TimeSpan.FromMilliseconds(8));
stats.Record("SELECT * FROM Products", TimeSpan.FromMilliseconds(45));

Console.WriteLine($"Distinct queries: {stats.Count}"); // 2

foreach (var entry in stats.TopByCount)
{
    Console.WriteLine($"{entry.Sql} – Count: {entry.Count}, Total: {entry.TotalDuration.TotalMilliseconds} ms");
}
// Output:
// SELECT * FROM Orders WHERE CustomerId = @p0 – Count: 2, Total: 20 ms
// SELECT * FROM Products – Count: 1, Total: 45 ms
```

### Example 2: Detecting N+1 with reset

```csharp
var stats = new QueryStatistics();

// Simulate N+1: same query executed once per customer
foreach (var customer in customers)
{
    // ... EF Core executes the orders query for each customer
    stats.Record("SELECT * FROM Orders WHERE CustomerId = @p0", TimeSpan.FromMilliseconds(5));
}

var top = stats.TopByCount.First();
if (top.Count > 1)
{
    Console.WriteLine($"Potential N+1: '{top.Sql}' executed {top.Count} times.");
}

// Reset for next analysis window
stats.Reset();
Debug.Assert(stats.Count == 0);
```

## Notes

- **Snapshot semantics:** `TopByCount` creates a snapshot at access time. Subsequent calls to `Record` do not update a previously obtained list.
- **Thread safety:** `Record`, `Reset`, and `TopByCount` are not thread-safe. Concurrent calls from multiple threads must be externally synchronized.
- **SQL normalization:** The caller is responsible for normalizing SQL text (e.g., constantizing parameter values) before calling `Record`. The type performs exact string matching; semantically identical queries with different literal values are treated as distinct entries unless normalized beforehand.
- **Empty state:** After construction or `Reset`, `Count` returns zero and `TopByCount` returns an empty list (not null). Iterating over it is safe.
- **Duration accumulation:** `TotalDuration` on a `QueryStatEntry` is the sum of all `duration` arguments passed to `Record` for that SQL text. No averaging or other statistics are computed.
