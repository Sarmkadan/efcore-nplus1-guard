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
