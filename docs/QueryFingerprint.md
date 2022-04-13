# QueryFingerprint

The `QueryFingerprint` struct provides a unique identification mechanism for Entity Framework Core queries, enabling the detection and analysis of repetitive query patterns associated with N+1 problems. By normalizing SQL command text and correlating it with specific application call sites, it allows monitoring systems to group similar queries even when parameter values differ.

## API

### `string CommandTextHash`
Gets the hash value representing the normalized SQL command text. This property is used for efficient comparison of queries that share identical structures.

### `string NormalizedSql`
Gets the normalized version of the SQL command text. This string has been processed to remove variable parameter values, making it consistent across multiple executions of the same query logic.

### `string CallSite`
Gets the identifier or location in the application code where the query originated. This helps differentiate between identical queries executed from different parts of the application.

### `static QueryFingerprint Create(string sql, string callSite)`
Creates a new instance of `QueryFingerprint`.
*   **Parameters:** 
    *   `sql`: The raw SQL command text to normalize.
    *   `callSite`: A string representing the origin of the query execution.
*   **Returns:** A new `QueryFingerprint` instance configured with the computed hash and normalized SQL.

### `override bool Equals(object? obj)`
Determines whether the specified object is equal to the current `QueryFingerprint`.
*   **Parameters:** `obj`: The object to compare with the current instance.
*   **Returns:** `true` if the specified object is a `QueryFingerprint` and has the same `CommandTextHash` and `CallSite`; otherwise, `false`.

### `bool Equals(QueryFingerprint other)`
Determines whether the specified `QueryFingerprint` is equal to the current instance.
*   **Parameters:** `other`: The `QueryFingerprint` to compare with the current instance.
*   **Returns:** `true` if the properties `CommandTextHash` and `CallSite` match; otherwise, `false`.

### `override int GetHashCode()`
Returns the hash code for the current `QueryFingerprint`.
*   **Returns:** A 32-bit signed integer hash code derived from the `CommandTextHash` and `CallSite` properties.

## Usage

### Identifying Repeated Queries
```csharp
var sql = "SELECT [Id], [Name] FROM [Products] WHERE [CategoryId] = @p0";
var callSite = "ProductService.GetProductsByCategory";

var fingerprint1 = QueryFingerprint.Create(sql, callSite);
var fingerprint2 = QueryFingerprint.Create(sql, callSite);

if (fingerprint1.Equals(fingerprint2))
{
    // Both fingerprints represent the same query from the same location
}
```

### Storing in a Dictionary
```csharp
var queryCounts = new Dictionary<QueryFingerprint, int>();
var fingerprint = QueryFingerprint.Create(sql, callSite);

if (queryCounts.TryGetValue(fingerprint, out var count))
{
    queryCounts[fingerprint] = count + 1;
}
else
{
    queryCounts[fingerprint] = 1;
}
```

## Notes

*   **Immutability:** `QueryFingerprint` is designed to be immutable, ensuring that its identity remains stable once created.
*   **Thread Safety:** Instances of `QueryFingerprint` are thread-safe because they are immutable; they can be safely shared across multiple threads without synchronization.
*   **Normalization:** The effectiveness of the fingerprint depends on the quality of SQL normalization. If the normalization logic changes, `CommandTextHash` values may change, rendering previously stored fingerprints incompatible.
*   **Edge Cases:** If `NormalizedSql` or `CallSite` are null or empty strings, the `Create` method handles them gracefully, though such fingerprints may be less effective at distinguishing unique queries. Always ensure valid inputs are provided to `Create` for reliable identification.
