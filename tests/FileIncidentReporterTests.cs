using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EfCoreNPlusOneGuard;

public class FileIncidentReporterTests : IDisposable
{
    private readonly string _testFilePath;
    private FileIncidentReporter? _reporter;
    private bool _disposed;

    public FileIncidentReporterTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"FileIncidentReporterTests_{Guid.NewGuid()}.log");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        _disposed = true;
    }

    [Fact]
    public void Constructor_WithNullFilePath_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new FileIncidentReporter(null!));
        Assert.Equal("filePath", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyFilePath_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new FileIncidentReporter(string.Empty));
        Assert.Contains("filePath", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithValidFilePath_CreatesReporter()
    {
        // Arrange & Act
        _reporter = new FileIncidentReporter(_testFilePath);

        // Assert
        Assert.NotNull(_reporter);
    }

    [Fact]
    public void Constructor_WithAppendFalse_ConfiguresAppendMode()
    {
        // Arrange & Act
        _reporter = new FileIncidentReporter(_testFilePath, append: false);

        // Assert
        Assert.NotNull(_reporter);
    }

    [Fact]
    public async Task Report_WithNullIncident_ThrowsArgumentNullException()
    {
        // Arrange
        _reporter = new FileIncidentReporter(_testFilePath);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _reporter.ReportAsync(null!));
    }

    [Fact]
    public async Task Report_WithValidIncident_WritesToFile()
    {
        // Arrange
        var incident = new NPlusOneIncident
        {
            SqlQuery = "SELECT * FROM Users WHERE Id = 1",
            Count = 10,
            Severity = NPlusOneSeverity.High,
            StackTrace = "at MyApp.Services.UserService.GetUser(Int32 id) in UserService.cs:line 42",
            CallSite = "MyApp.Services.UserService.GetUser"
        };

        _reporter = new FileIncidentReporter(_testFilePath);

        // Act
        await _reporter.ReportAsync(incident);

        // Assert
        Assert.True(File.Exists(_testFilePath));
        var content = await File.ReadAllTextAsync(_testFilePath);
        Assert.Contains("SELECT * FROM Users WHERE Id = 1", content);
        Assert.Contains("10", content);
        Assert.Contains("High", content);
        Assert.Contains("MyApp.Services.UserService.GetUser", content);
    }

    [Fact]
    public async Task Report_WithIncidentWithoutCallSite_WritesToFile()
    {
        // Arrange
        var incident = new NPlusOneIncident
        {
            SqlQuery = "SELECT * FROM Products",
            Count = 5,
            Severity = NPlusOneSeverity.Medium,
            StackTrace = "at System.Data.SqlClient.SqlCommand.ExecuteReader()"
        };

        _reporter = new FileIncidentReporter(_testFilePath);

        // Act
        await _reporter.ReportAsync(incident);

        // Assert
        var content = await File.ReadAllTextAsync(_testFilePath);
        Assert.Contains("SELECT * FROM Products", content);
        Assert.Contains("5", content);
        Assert.Contains("Medium", content);
    }

    [Fact]
    public async Task ReportBatch_WithNullIncidents_ThrowsArgumentNullException()
    {
        // Arrange
        _reporter = new FileIncidentReporter(_testFilePath);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _reporter.ReportBatchAsync(null!));
    }

    [Fact]
    public async Task ReportBatch_WithEmptyCollection_DoesNotThrow()
    {
        // Arrange
        _reporter = new FileIncidentReporter(_testFilePath);
        var incidents = new List<NPlusOneIncident>();

        // Act
        await _reporter.ReportBatchAsync(incidents);

        // Assert
        Assert.False(File.Exists(_testFilePath));
    }

    [Fact]
    public async Task ReportBatch_WithValidIncidents_WritesAllToFile()
    {
        // Arrange
        var incidents = new List<NPlusOneIncident>
        {
            new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Orders",
                Count = 3,
                Severity = NPlusOneSeverity.Low,
                StackTrace = "at OrderService.GetOrders()"
            },
            new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Customers",
                Count = 7,
                Severity = NPlusOneSeverity.High,
                StackTrace = "at CustomerService.GetCustomers()",
                CallSite = "CustomerService.GetCustomers"
            }
        };

        _reporter = new FileIncidentReporter(_testFilePath);

        // Act
        await _reporter.ReportBatchAsync(incidents);

        // Assert
        Assert.True(File.Exists(_testFilePath));
        var content = await File.ReadAllTextAsync(_testFilePath);
        Assert.Contains("SELECT * FROM Orders", content);
        Assert.Contains("SELECT * FROM Customers", content);
        Assert.Contains("3", content);
        Assert.Contains("7", content);
    }

    [Fact]
    public async Task ReportBatch_WithNullIncidentInCollection_SkipsNullIncident()
    {
        // Arrange
        var incidents = new List<NPlusOneIncident>
        {
            new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Table1",
                Count = 1,
                Severity = NPlusOneSeverity.Low,
                StackTrace = "at Test.Method1()"
            },
            new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Table2",
                Count = 2,
                Severity = NPlusOneSeverity.Medium,
                StackTrace = "at Test.Method2()"
            }
        };

        _reporter = new FileIncidentReporter(_testFilePath);

        // Act
        await _reporter.ReportBatchAsync(incidents);

        // Assert
        var content = await File.ReadAllTextAsync(_testFilePath);
        Assert.Contains("SELECT * FROM Table1", content);
        Assert.Contains("SELECT * FROM Table2", content);
    }

    [Fact]
    public async Task Report_WithAppendTrue_AppendsToExistingFile()
    {
        // Arrange
        var firstIncident = new NPlusOneIncident
        {
            SqlQuery = "First query",
            Count = 1,
            Severity = NPlusOneSeverity.Low,
            StackTrace = "at Test.Method1()"
        };

        var secondIncident = new NPlusOneIncident
        {
            SqlQuery = "Second query",
            Count = 2,
            Severity = NPlusOneSeverity.Medium,
            StackTrace = "at Test.Method2()"
        };

        _reporter = new FileIncidentReporter(_testFilePath, append: true);

        // Act - First report
        await _reporter.ReportAsync(firstIncident);

        // Assert - First report
        Assert.True(File.Exists(_testFilePath));
        var firstContent = await File.ReadAllTextAsync(_testFilePath);
        Assert.Contains("First query", firstContent);

        // Act - Second report
        await _reporter.ReportAsync(secondIncident);

        // Assert - Second report
        Assert.True(File.Exists(_testFilePath));
        var secondContent = await File.ReadAllTextAsync(_testFilePath);
        Assert.Contains("First query", secondContent);
        Assert.Contains("Second query", secondContent);
    }

    [Fact]
    public async Task Report_WithAppendFalse_TruncatesFileOnFirstWrite()
    {
        // Arrange
        var firstIncident = new NPlusOneIncident
        {
            SqlQuery = "First query",
            Count = 1,
            Severity = NPlusOneSeverity.Low,
            StackTrace = "at Test.Method1()"
        };

        _reporter = new FileIncidentReporter(_testFilePath, append: false);

        // Act - First report
        await _reporter.ReportAsync(firstIncident);

        // Assert - First report - file should contain only the first incident
        Assert.True(File.Exists(_testFilePath));
        var content = await File.ReadAllTextAsync(_testFilePath);
        Assert.Contains("First query", content);
        // Count occurrences - should be exactly 1
        var lines = content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
    }

    [Fact]
    public async Task FormatIncident_WithAllProperties_ReturnsFormattedString()
    {
        // Arrange
        var incident = new NPlusOneIncident
        {
            SqlQuery = "SELECT * FROM TestTable WHERE Id = @id",
            Count = 42,
            Severity = NPlusOneSeverity.High,
            StackTrace = "at MyApp.Repository.GetData() in Repository.cs:line 25\n   at MyApp.Service.LoadData() in Service.cs:line 15",
            CallSite = "MyApp.Service.LoadData"
        };

        _reporter = new FileIncidentReporter(_testFilePath);

        // Use reflection to call protected method
        var formatMethod = typeof(FileIncidentReporter).GetMethod(
            "FormatIncident",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        var result = formatMethod?.Invoke(_reporter, new object[] { incident }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Matches(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}", result);
        Assert.Contains("SELECT * FROM TestTable WHERE Id = @id", result);
        Assert.Contains("42", result);
        Assert.Contains("High", result);
        Assert.Contains("MyApp.Repository.GetData()", result);
        Assert.Contains("CallSite: MyApp.Service.LoadData", result);
    }

    [Fact]
    public async Task Report_WithMinimalIncident_ReturnsFormattedString()
    {
        // Arrange
        var incident = new NPlusOneIncident
        {
            SqlQuery = "SELECT 1",
            Count = 1,
            Severity = NPlusOneSeverity.Low,
            StackTrace = "at System.Data.SqlClient.SqlCommand.ExecuteReader()"
        };

        _reporter = new FileIncidentReporter(_testFilePath);

        // Use reflection to call protected method
        var formatMethod = typeof(FileIncidentReporter).GetMethod(
            "FormatIncident",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        var result = formatMethod?.Invoke(_reporter, new object[] { incident }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("SELECT 1", result);
        Assert.Contains("1", result);
        Assert.Contains("Low", result);
        Assert.Contains("System.Data.SqlClient.SqlCommand.ExecuteReader()", result);
    }

    [Fact]
    public async Task Report_WithSyncReportMethod_CallsAsyncMethod()
    {
        // Arrange
        var incident = new NPlusOneIncident
        {
            SqlQuery = "Test query",
            Count = 1,
            Severity = NPlusOneSeverity.Low,
            StackTrace = "at Test.Method()"
        };

        _reporter = new FileIncidentReporter(_testFilePath);

        // Act
        _reporter.Report(incident);

        // Wait a bit for async operation to complete
        await Task.Delay(100);

        // Assert
        Assert.True(File.Exists(_testFilePath));
        var content = await File.ReadAllTextAsync(_testFilePath);
        Assert.Contains("Test query", content);
    }
}