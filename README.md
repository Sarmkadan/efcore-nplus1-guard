var options = new NPlusOneGuardOptions
{
    Threshold = 5,
    DetectionWindow = TimeSpan.FromSeconds(10),
    ThrowOnDetection = true,
    LogOnDetection = true,
    IgnoredQueryPatterns = new List<string> { "SELECT * FROM __EFMigrationsHistory" }
};

## QueryStatistics

The `QueryStatistics` class is used to track and analyze the queries executed by the application. It provides a way to record query statistics, such as the number of queries executed, the total duration of the queries, and the SQL queries themselves.

Here is an example of how to use `QueryStatistics`:
