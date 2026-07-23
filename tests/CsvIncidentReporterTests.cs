using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests
{
    public class CsvIncidentReporterTests : IDisposable
    {
        private const string TestFilePath = "./test-incidents.csv";
        private readonly CsvIncidentReporter _reporter;

        public CsvIncidentReporterTests()
        {
            if (File.Exists(TestFilePath))
            {
                File.Delete(TestFilePath);
            }

            _reporter = new CsvIncidentReporter(TestFilePath, append: true);
        }

        public void Dispose()
        {
            if (File.Exists(TestFilePath))
            {
                File.Delete(TestFilePath);
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
            var reporter = new CsvIncidentReporter(TestFilePath, append: true);
            Assert.NotNull(reporter);
        }

        [Fact]
        public async Task ReportAsync_WithValidIncident_WritesToFile()
        {
            var incident = new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Users WHERE Id = @p0",
                Count = 5,
                Severity = NPlusOneSeverity.High,
                StackTrace = "at UserRepository.GetUsers()",
                CallSite = "UserRepository.GetUsers"
            };

            await _reporter.ReportAsync(incident, default);

            Assert.True(File.Exists(TestFilePath));
            var content = await File.ReadAllTextAsync(TestFilePath);
            Assert.Contains("SELECT * FROM Users WHERE Id = @p0", content);
            Assert.Contains("5", content);
            Assert.Contains("High", content);
            Assert.Contains("UserRepository.GetUsers", content);
        }

        [Fact]
        public async Task ReportAsync_WithNullIncident_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _reporter.ReportAsync(null!, default));
        }

        [Fact]
        public async Task ReportBatchAsync_WithValidIncidents_WritesMultipleLines()
        {
            var incidents = new List<NPlusOneIncident>
            {
                new NPlusOneIncident
                {
                    SqlQuery = "SELECT * FROM Users WHERE Id = @p0",
                    Count = 5,
                    Severity = NPlusOneSeverity.High,
                    StackTrace = "",
                    CallSite = "UserRepository.GetUsers"
                },
                new NPlusOneIncident
                {
                    SqlQuery = "SELECT * FROM Orders WHERE UserId = @p0",
                    Count = 10,
                    Severity = NPlusOneSeverity.Medium,
                    StackTrace = "",
                    CallSite = "OrderRepository.GetOrders"
                }
            };

            await _reporter.ReportBatchAsync(incidents, default);

            Assert.True(File.Exists(TestFilePath));
            var content = await File.ReadAllTextAsync(TestFilePath);
            Assert.Contains("SELECT * FROM Users WHERE Id = @p0", content);
            Assert.Contains("SELECT * FROM Orders WHERE UserId = @p0", content);
            Assert.Contains("5", content);
            Assert.Contains("10", content);
        }

        [Fact]
        public async Task ReportBatchAsync_WithNullIncidents_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _reporter.ReportBatchAsync(null!, default));
        }

        [Fact]
        public void ReportBatch_WithNullIncidents_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _reporter.ReportBatch(null!));
        }

        [Fact]
        public async Task ReportBatchAsync_WithEmptyCollection_DoesNotThrow()
        {
            await _reporter.ReportBatchAsync(new List<NPlusOneIncident>(), default);
        }

        [Fact]
        public async Task ReportBatchAsync_WithNullInCollection_SkipsNullIncident()
        {
            var incidents = new List<NPlusOneIncident?>
            {
                new NPlusOneIncident
                {
                    SqlQuery = "SELECT * FROM Users",
                    Count = 1,
                    Severity = NPlusOneSeverity.Low,
                    StackTrace = "",
                    CallSite = "TestClass.TestMethod"
                },
                null,
                new NPlusOneIncident
                {
                    SqlQuery = "SELECT * FROM Orders",
                    Count = 2,
                    Severity = NPlusOneSeverity.Medium,
                    StackTrace = "",
                    CallSite = "TestClass.TestMethod"
                }
            };

            await _reporter.ReportBatchAsync(incidents, default);

            Assert.True(File.Exists(TestFilePath));
            var content = await File.ReadAllTextAsync(TestFilePath);
            var lines = content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length);
        }

        [Fact]
        public async Task ReportAsync_WithIncidentWithNullSqlQuery_HandlesGracefully()
        {
            var incident = new NPlusOneIncident
            {
                SqlQuery = null!,
                Count = 1,
                Severity = NPlusOneSeverity.Low,
                StackTrace = "",
                CallSite = null
            };

            await _reporter.ReportAsync(incident, default);

            Assert.True(File.Exists(TestFilePath));
            var content = await File.ReadAllTextAsync(TestFilePath);
            Assert.Contains(",1,Low,0,", content);
        }

        [Fact]
        public async Task ReportAsync_WithAppendFalse_TruncatesFileFirst()
        {
            var incident1 = new NPlusOneIncident
            {
                SqlQuery = "First incident",
                Count = 1,
                Severity = NPlusOneSeverity.Low,
                StackTrace = "",
                CallSite = "Test.Test"
            };

            await _reporter.ReportAsync(incident1, default);

            var reporter2 = new CsvIncidentReporter(TestFilePath, append: false);
            var incident2 = new NPlusOneIncident
            {
                SqlQuery = "Second incident",
                Count = 2,
                Severity = NPlusOneSeverity.Medium,
                StackTrace = "",
                CallSite = "Test.Test"
            };

            await reporter2.ReportAsync(incident2, default);

            var finalContent = await File.ReadAllTextAsync(TestFilePath);
            Assert.DoesNotContain("First incident", finalContent);
            Assert.Contains("Second incident", finalContent);
        }
    }
}
