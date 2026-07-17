## QueryFingerprintValidationResult

The QueryFingerprintValidationResult class represents the validation result of a query fingerprint. It contains information about the query fingerprint's validity, validation errors, and metadata used for fingerprinting.

The QueryFingerprintValidationResult class contains the following public members:
* CommandTextHash: a string representing the command text hash
* NormalizedSql: a string representing the normalized SQL
* CallSite: a string representing the call site
* IsValid: a boolean indicating whether the query fingerprint is valid
* ValidationErrors: a string array containing validation errors

Example usage:
```csharp
var result = new QueryFingerprintValidationResult
{
    CommandTextHash = "abc123",
    NormalizedSql = "SELECT * FROM Users WHERE Id = @__userId_0",
    CallSite = "MyApp.Services.UserService.GetUser(Int32 id)",
    IsValid = true,
    ValidationErrors = Array.Empty<string>()
};
```

## IncidentAggregator

The `IncidentAggregator` class collects N+1 incidents in-memory and groups them by query fingerprint. It provides thread-safe operations for adding incidents, retrieving counts by fingerprint, accessing all incidents, building summary reports, and clearing collected data.

Example usage:
```csharp
var aggregator = new IncidentAggregator();

// Add incidents
aggregator.Add(new NPlusOneIncident
{
    SqlQuery = "SELECT * FROM Orders WHERE CustomerId = @customerId",
    CallSite = "OrderService.GetOrdersForCustomer(Int32 customerId)",
    ExecutionCount = 10
});

aggregator.Add(new NPlusOneIncident
{
    SqlQuery = "SELECT * FROM Orders WHERE CustomerId = @customerId",
    CallSite = "OrderService.GetOrdersForCustomer(Int32 customerId)",
    ExecutionCount = 10
});

aggregator.Add(new NPlusOneIncident
{
    SqlQuery = "SELECT * FROM Products WHERE CategoryId = @categoryId",
    CallSite = "ProductService.GetProductsByCategory(Int32 categoryId)",
    ExecutionCount = 5
});

// Get counts by fingerprint
var counts = aggregator.CountsByFingerprint();
// {"SELECT * FROM Orders WHERE CustomerId = @customerId": 2, "SELECT * FROM Products WHERE CategoryId = @categoryId": 1}

// Get all incidents
var allIncidents = aggregator.All();

// Build summary text
var summary = aggregator.BuildSummaryText();
/*
N+1 Guard Summary (2026-07-18 12:34:56 UTC)

Total incidents: 3
Unique query fingerprints: 2
Total duplicate queries: 2

Top fingerprints by occurrence:
 2x: SELECT * FROM Orders WHERE CustomerId = @customerId
 1x: SELECT * FROM Products WHERE CategoryId = @categoryId
*/

// Clear all incidents
aggregator.Clear();
```

## QueryStatisticsValidation

The `QueryStatisticsValidation` static class provides validation helpers for `QueryStatistics` and its nested `QueryStatistics.QueryStatEntry` instances. It offers methods to validate query statistics, check validity, and ensure validity with appropriate exception throwing.

Example usage:

```csharp
using System;
using EfCoreNPlusOneGuard;


// Create query statistics with some entries
var stats = new QueryStatistics();
stats.Add("SELECT * FROM Users WHERE Id = @id", 10, TimeSpan.FromMilliseconds(150));
stats.Add("SELECT * FROM Orders WHERE CustomerId = @customerId", 5, TimeSpan.FromMilliseconds(80));

// Validate the statistics
var validationErrors = stats.Validate();
if (validationErrors.Count > 0)
{
    Console.WriteLine("Validation errors found:");
    foreach (var error in validationErrors)
    {
        Console.WriteLine($"- {error}");
    }
}
else
{
    Console.WriteLine("Query statistics are valid!");
}

// Check if valid
bool isValid = stats.IsValid();
Console.WriteLine($"Is valid: {isValid}");

// Ensure validity (throws if invalid)
try
{
    stats.EnsureValid();
    Console.WriteLine("Statistics validated successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}

// Validate individual entries
foreach (var entry in stats.TopByCount(int.MaxValue))
{
    bool entryValid = entry.IsValid();
    Console.WriteLine($"Entry '{entry.Sql}' is valid: {entryValid}");
    
    try
    {
        entry.EnsureValid();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Entry validation failed: {ex.Message}");
    }
}
```

## NPlusOneIncidentExtensions

`NPlusOneIncidentExtensions` provides a set of LINQ‑style extension methods for analysing collections of `NPlusOneIncident` objects.  
The methods enable filtering by severity or count, grouping by normalized query patterns, summarising totals, ordering by severity and count, and retrieving the most severe incidents, all without modifying the original collection.

**Example usage**

```csharp
using System;
using System.Collections.Generic;
using EfCoreNPlusOneGuard;

var incidents = new List<NPlusOneIncident>
{
    new NPlusOneIncident
    {
        SqlQuery = "SELECT * FROM Users",
        CallSite = "UserService.GetAll()",
        Severity = NPlusOneSeverity.High,
        Count = 5
    },
    new NPlusOneIncident
    {
        SqlQuery = "SELECT * FROM Orders",
        CallSite = "OrderService.GetAll()",
        Severity = NPlusOneSeverity.Medium,
        Count = 3
    },
    new NPlusOneIncident
    {
        SqlQuery = "SELECT * FROM Users",
        CallSite = "UserService.GetAll()",
        Severity = NPlusOneSeverity.High,
        Count = 2
    }
};

// Basic aggregations
int total = incidents.TotalCount();
int highTotal = incidents.TotalCountBySeverity(NPlusOneSeverity.High);
string summary = incidents.ToSummaryString();

// Filtering
var highSeverity = incidents.FilterBySeverity(NPlusOneSeverity.High);
var minCount = incidents.FilterByMinCount(4);

// Ordering and selection
var ordered = incidents.OrderBySeverityAndCount();
var mostSevere = incidents.GetMostSevere();
var topTwo = incidents.GetTopSevere(2);

// Predicates
bool hasHigh = incidents.HasHighSeverity();
bool anyLarge = incidents.AnyExceedsCount(4);

// Grouping by normalized query pattern
var groups = incidents.GroupByQueryPattern();

Console.WriteLine($"Total incidents: {total}");
Console.WriteLine(summary);
Console.WriteLine($"Most severe query: {mostSevere?.SqlQuery}");
Console.WriteLine($"Has high severity: {hasHigh}");
Console.WriteLine($"Any incident with count >= 4: {anyLarge}");
```

The example demonstrates how the extension methods can be combined to produce rich diagnostics and reports from a simple list of `NPlusOneIncident` objects.