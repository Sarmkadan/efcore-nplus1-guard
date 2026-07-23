using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests
{
    public class CsvIncidentReporterTests : IDisposable
    {
        private readonly string _tempFilePath;
        private readonly CsvIncidentReporter _reporter;

        public CsvIncidentReporterTests()
        {
            // Create a unique temporary file for each test run.
            _tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv");
            _reporter = new CsvIncidentReporter(_tempFilePath, append: true);
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        [Fact]
        public void Constructor_WithNullFilePath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CsvIncidentReporter(null!, append: true));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            using var reporter = new CsvIncidentReporter(_tempFilePath, append: false);
            Assert.NotNull(reporter);
        }

        [Fact]
        public void Report_WithNullIncident_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _reporter.Report(null!));
        }

        [Fact]
        public void Report_WithValidIncident_WritesCsvLine()
        {
            var incident = new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Users WHERE Id = @p0",
                Count = 3,
                Severity = NPlusOneSeverity.High,
                CallSite = "UserRepository.GetUsers"
            };

            _reporter.Report(incident);

            Assert.True(File.Exists(_tempFilePath));
            var content = File.ReadAllText(_tempFilePath);
            // Header + one data line
            var lines = content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length);
            Assert.Contains("SELECT * FROM Users WHERE Id = @p0", content);
            Assert.Contains("3", content);
            Assert.Contains("High", content);
            Assert.Contains("UserRepository.GetUsers", content);
        }

        [Fact]
        public void ReportBatch_WithNullIncidents_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _reporter.ReportBatch(null!));
        }

        [Fact]
        public void ReportBatch_WithEmptyCollection_DoesNotThrowAndWritesOnlyHeader()
        {
            _reporter.ReportBatch(new List<NPlusOneIncident>());

            Assert.True(File.Exists(_tempFilePath));
            var content = File.ReadAllText(_tempFilePath);
            var lines = content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            // Only header line should be present
            Assert.Single(lines);
        }

        [Fact]
        public void ReportBatch_WithValidIncidents_WritesMultipleLines()
        {
            var incidents = new List<NPlusOneIncident>
            {
                new NPlusOneIncident
                {
                    SqlQuery = "SELECT * FROM Users WHERE Id = @p0",
                    Count = 5,
                    Severity = NPlusOneSeverity.High,
                    CallSite = "UserRepository.GetUsers"
                },
                new NPlusOneIncident
                {
                    SqlQuery = "SELECT * FROM Orders WHERE UserId = @p0",
                    Count = 10,
                    Severity = NPlusOneSeverity.Medium,
                    CallSite = "OrderRepository.GetOrders"
                }
            };

            _reporter.ReportBatch(incidents);

            Assert.True(File.Exists(_tempFilePath));
            var content = File.ReadAllText(_tempFilePath);
            var lines = content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            // Header + two data lines
            Assert.Equal(3, lines.Length);
            Assert.Contains("SELECT * FROM Users WHERE Id = @p0", content);
            Assert.Contains("SELECT * FROM Orders WHERE UserId = @p0", content);
            Assert.Contains("5", content);
            Assert.Contains("10", content);
        }

        [Fact]
        public void ReportBatch_WithNullInCollection_SkipsNullIncident()
        {
            var incidents = new List<NPlusOneIncident?>
            {
                new NPlusOneIncident
                {
                    SqlQuery = "SELECT * FROM Products",
                    Count = 1,
                    Severity = NPlusOneSeverity.Low,
                    CallSite = "ProductRepo.Get"
                },
                null,
                new NPlusOneIncident
                {
                    SqlQuery = "SELECT * FROM Categories",
                    Count = 2,
                    Severity = NPlusOneSeverity.Medium,
                    CallSite = "CategoryRepo.Get"
                }
            };

            // Cast to non‑nullable list for the reporter (it will ignore nulls internally)
            _reporter.ReportBatch(incidents as List<NPlusOneIncident> ?? new List<NPlusOneIncident>());

            Assert.True(File.Exists(_tempFilePath));
            var content = File.ReadAllText(_tempFilePath);
            var lines = content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            // Header + two valid incidents
            Assert.Equal(3, lines.Length);
            Assert.Contains("SELECT * FROM Products", content);
            Assert.Contains("SELECT * FROM Categories", content);
        }

        [Fact]
        public void Report_WithIncidentHavingNullFields_HandlesGracefully()
        {
            var incident = new NPlusOneIncident
            {
                SqlQuery = null!,
                Count = 1,
                Severity = NPlusOneSeverity.Low,
                CallSite = null
            };

            _reporter.Report(incident);

            Assert.True(File.Exists(_tempFilePath));
            var content = File.ReadAllText(_tempFilePath);
            // Expect empty fields for null values (two commas in a row)
            Assert.Contains(",,Low,0,", content);
        }

        [Fact]
        public void Report_WithAppendFalse_TruncatesFileOnFirstWrite()
        {
            // First reporter writes a line with append = true
            var firstReporter = new CsvIncidentReporter(_tempFilePath, append: true);
            var firstIncident = new NPlusOneIncident
            {
                SqlQuery = "First incident",
                Count = 1,
                Severity = NPlusOneSeverity.Low,
                CallSite = "Test.First"
            };
            firstReporter.Report(firstIncident);

            // Second reporter uses append = false, which should truncate before writing
            var secondReporter = new CsvIncidentReporter(_tempFilePath, append: false);
            var secondIncident = new NPlusOneIncident
            {
                SqlQuery = "Second incident",
                Count = 2,
                Severity = NPlusOneSeverity.Medium,
                CallSite = "Test.Second"
            };
            secondReporter.Report(secondIncident);

            var finalContent = File.ReadAllText(_tempFilePath);
            // Header + second incident only
            var lines = finalContent.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length);
            Assert.DoesNotContain("First incident", finalContent);
            Assert.Contains("Second incident", finalContent);
        }
    }
}
