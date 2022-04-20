var options = new NPlusOneGuardOptions { Threshold = 5, DetectionWindow = TimeSpan.FromSeconds(10), ThrowOnDetection = true, LogOnDetection = true, IgnoredQueryPatterns = new List<string> { "SELECT * FROM __EFMigrationsHistory" } };

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

## QueryStatistics

The `QueryStatistics` class is used to track and analyze the queries executed by the application. It provides a way to record query statistics, such as the number of queries executed, the total duration of the queries, and the SQL queries themselves.

Here is an example of how to use `QueryStatistics`: