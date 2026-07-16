# EfCoreNPlusOneGuard

Detects EF Core N+1 query patterns at runtime. One line in your `DbContext` setup:

```csharp
optionsBuilder.UseNPlusOneGuard();
```

Or with options and a detection callback:

```csharp
optionsBuilder.UseNPlusOneGuard(
    o =>
    {
        o.Threshold = 5;
        o.DetectionWindow = TimeSpan.FromSeconds(10);
        o.ThrowOnDetection = true;
        o.LogOnDetection = true;
        o.IgnoredQueryPatterns.Add("__EFMigrationsHistory");
    },
    incident => Console.WriteLine($"N+1: {incident.SqlQuery} x{incident.Count}"));
```

## Architecture

How the interceptor -> fingerprint -> sliding-window pipeline works, why it is built
that way, and where to extend it: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## NPlusOneIncident

The `NPlusOneIncident` class represents a detected N+1 query pattern in Entity Framework Core applications. It contains detailed information about problematic SQL queries including the query text, occurrence count, severity level, and stack trace for debugging purposes.

Here is an example of how to create and use an `NPlusOneIncident`:

```csharp
var incident = new NPlusOneIncident
{
    SqlQuery = "SELECT * FROM Products WHERE CategoryId = @__categoryId_0",
    Count = 42,
    Severity = NPlusOneSeverity.High,
    StackTrace = "   at MyApp.Services.ProductService.GetProductsByCategory(Int32 categoryId)\n"
               + "   at MyApp.Controllers.ProductsController.GetProducts(Int32 id)\n"
               + "   at lambda_method1(Closure , Object , Object[] )"
};

// Write to HTML report
var writer = new HtmlIncidentReportWriter();
writer.WriteToFile(new[] { incident }, "nplus1-report.html");
```

## DuplicateQueryDetector

The `DuplicateQueryDetector` class monitors Entity Framework Core queries and identifies duplicate SQL queries that are executed repeatedly within a detection window. It helps detect N+1 query patterns and other query duplication issues by tracking query text, parameters, and occurrence counts.

Here is an example of how to use `DuplicateQueryDetector`:

```csharp
// Create a detector with a 10-second detection window
var detector = new DuplicateQueryDetector(TimeSpan.FromSeconds(10));

// Record queries as they execute
foreach (var query in executedQueries)
{
    detector.Record(query.Sql, query.Parameters);
}

// Check for duplicates after processing queries
var duplicates = detector.GetDuplicates();

if (duplicates.Any())
{
    Console.WriteLine($"Found {duplicates.Count} duplicate query groups:");
    foreach (var group in duplicates)
    {
        Console.WriteLine($"  - {group.Count} occurrences of: {group.Sql}");
        if (group.Parameters != null)
        {
            Console.WriteLine($"    Parameters: {group.Parameters}");
        }
    }
}

// Clear the detector for the next detection window
detector.Clear();
```

## QueryStatistics

The `QueryStatistics` class is used to track and analyze the queries executed by the application. It provides a way to record query statistics, such as the number of queries executed, the total duration of the queries, and the SQL queries themselves.

Here is an example of how to use `QueryStatistics`: