# QueryTracker
The `QueryTracker` class monitors Entity Framework Core query executions to detect potential N+1 query problems. It records information about each executed query and, when a pattern indicative of an N+1 issue is observed, exposes the incident via the `Record` property. Consumers can reset the tracker to start a fresh monitoring session.

## API
### QueryTracker()
**Purpose**  
Initializes a new instance of the `QueryTracker` with no prior query history.

**Parameters**  
None.

**Return value**  
A new `QueryTracker` object.

**Exceptions**  
None.

### NPlusOneIncident? Record
**Purpose**  
Gets the most recent N+1 incident detected by the tracker, or `null` if no incident has been recorded since the last reset.

**Parameters**  
None.

**Return value**  
An `NPlusOneIncident` instance describing the detected incident, or `null`.

**Exceptions**  
None.

### void Reset()
**Purpose**  
Clears all stored query execution data and any recorded incident, allowing the tracker to be reused for a new monitoring period.

**Parameters**  
None.

**Return value**  
None.

**Exceptions**  
None.

### void TrackExecution()
**Purpose**  
Notifies the tracker that a query has just been executed. The method updates internal counters and evaluates whether the sequence of executions matches an N+1 pattern.

**Parameters**  
None.

**Return value**  
None.

**Exceptions**  
Throws `InvalidOperationException` if called before the tracker has been properly initialized (e.g., after the object has been disposed in a future version that implements `IDisposable`). In the current implementation, no exceptions are thrown under normal use.

## Usage
### Basic monitoring within a using block
```csharp
using var tracker = new QueryTracker();

// Simulate executing queries inside a loop
foreach (var blog in context.Blogs)
{
    // Execute a query that may cause N+1
    var posts = context.Posts.Where(p => p.BlogId == blog.Id).ToList();
    tracker.TrackExecution();
}

// Check for an incident after the loop
if (tracker.Record is NPlusOneIncident incident)
{
    Console.WriteLine($"N+1 detected: {incident.Message}");
}
else
{
    Console.WriteLine("No N+1 issue observed.");
}
```

### Resetting the tracker for multiple phases
```csharp
var tracker = new QueryTracker();

// Phase 1: Load authors
foreach (var author in context.Authors)
{
    _ = context.Books.Where(b => b.AuthorId == author.Id).ToList();
    tracker.TrackExecution();
}

if (tracker.Record != null)
{
    // Handle incident for phase 1
    HandleIncident(tracker.Record);
}

// Reset before starting phase 2
tracker.Reset();

// Phase 2: Load categories
foreach (var category in context.Categories)
{
    _ = context.Books.Where(b => b.CategoryId == category.Id).ToList();
    tracker.TrackExecution();
}

if (tracker.Record != null)
{
    // Handle incident for phase 2
    HandleIncident(tracker.Record);
}
```

## Notes
- The tracker is **not thread‑safe**; concurrent calls to `TrackExecution` from multiple threads may corrupt internal state and lead to inaccurate incident detection. If multi‑threaded monitoring is required, external synchronization (e.g., a lock) must be applied.
- `Record` reflects only the **most recent** incident; earlier incidents are overwritten when a new one is detected. To retain a history, consumers should inspect `Record` after each query batch and persist the value as needed.
- Calling `TrackExecution` without any preceding query execution (e.g., in a tight loop that does not actually invoke EF Core) will still increment the internal counter, potentially producing false positives. Ensure the method is invoked only after a genuine EF Core query has been materialized.
- The class currently does not implement `IDisposable`; the `Reset` method provides the means to reuse the instance without allocating a new object. Future versions may add disposal semantics, at which point calling `TrackExecution` after disposal would throw an `ObjectDisposedException`.
