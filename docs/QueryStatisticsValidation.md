# QueryStatisticsValidation

A static utility class for validating query execution statistics to detect potential N+1 query issues in Entity Framework Core applications. Provides methods to check query validity, retrieve validation errors, and enforce validation constraints.

## API

### Validate

```csharp
public static IReadOnlyList<string> Validate()
```

Returns a read-only list of error messages describing detected N+1 query issues. The list is empty if no issues are found. This method does not throw exceptions but returns all validation failures as strings.

### IsValid

```csharp
public static bool IsValid { get; }
```

A static property indicating whether the current query statistics pass validation. Returns `true` if no N+1 issues are detected, `false` otherwise.

### EnsureValid

```csharp
public static void EnsureValid()
```

Throws an exception if query statistics contain N+1 issues. The exception message includes all validation errors. Use this method to enforce strict validation during query execution.

## Usage

### Example 1: Check Validity Before Proceeding

```csharp
var query = context.Blogs.Include(b => b.Posts);
var results = query.ToList();

if (!QueryStatisticsValidation.IsValid)
{
    var errors = QueryStatisticsValidation.Validate();
    // Log or handle N+1 issues
    foreach (var error in errors)
    {
        Console.WriteLine(error);
    }
}
```

### Example 2: Enforce Validation with Exception Handling

```csharp
try
{
    var query = context.Authors.Select(a => new { 
        Author = a, 
        Posts = a.Posts.ToList() 
    });
    var results = query.ToList();
    
    QueryStatisticsValidation.EnsureValid();
}
catch (InvalidOperationException ex)
{
    // Handle N+1 violation
    Console.WriteLine($"Query validation failed: {ex.Message}");
}
```

## Notes

- All members are static and operate on shared query statistics state. Concurrent access from multiple threads may lead to race conditions or inconsistent validation results.
- `Validate()` and `IsValid` do not modify internal state, but their results reflect the most recent query execution tracked by the guard interceptor.
- `EnsureValid()` throws `InvalidOperationException` when validation fails. The exception message contains all detected N+1 issues.
- Validation results are only meaningful after query execution. Calling these methods before any queries are executed may return empty results or default values.
