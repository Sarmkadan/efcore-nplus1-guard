# DuplicateQueryDetector

The `DuplicateQueryDetector` class is designed to monitor, track, and aggregate SQL query executions within an Entity Framework Core application. It provides functionality to identify identical queries executed multiple times, facilitating the detection and diagnosis of N+1 query performance anti-patterns.

## API

### Constructor
#### `DuplicateQueryDetector()`
Initializes a new instance of the `DuplicateQueryDetector` class with an empty state.

### Methods

#### `void Record(string sql, string? parameters)`
Records an executed query and its parameters.
- `sql`: The SQL command string.
- `parameters`: A string representation of the parameters used in the query.

#### `IReadOnlyList<DuplicateQueryGroup> GetDuplicates()`
Analyzes the recorded queries and returns a list of groups containing queries that were executed multiple times.

#### `void Clear()`
Resets the internal state of the detector, removing all recorded queries and resetting the count.

### Properties

#### `string Sql`
*Get-only.* Returns the SQL associated with the recorded context, if applicable.

#### `string? Parameters`
*Get-only.* Returns the parameters associated with the recorded context, if applicable.

#### `int Count`
*Get-only.* Returns the total number of queries recorded by this instance.

## Usage

### Tracking Queries in a Service
```csharp
var detector = new DuplicateQueryDetector();

// Simulate query execution
detector.Record("SELECT * FROM Users", "id=1");
detector.Record("SELECT * FROM Users", "id=1"); // Duplicate

var duplicates = detector.GetDuplicates();
foreach (var group in duplicates)
{
    Console.WriteLine($"Query {group.Sql} executed {group.Count} times.");
}
```

### Resetting the Detector
```csharp
var detector = new DuplicateQueryDetector();

// ... perform operations ...

// Clear data before processing a new batch or request
detector.Clear();

// Assert that the detector is empty
Debug.Assert(detector.Count == 0);
```

## Notes

### Thread Safety
`DuplicateQueryDetector` is not thread-safe. If queries are recorded from multiple threads simultaneously, access to `Record` and `Clear` must be externally synchronized. It is recommended to scope instances of this class to a single request or a single unit of work.

### Memory Consumption
Recording a high volume of unique queries may lead to increased memory usage. In high-throughput environments, ensure that `Clear()` is called periodically, or instantiate a new `DuplicateQueryDetector` per scope to prevent memory leaks and uncontrolled growth of the underlying storage.
