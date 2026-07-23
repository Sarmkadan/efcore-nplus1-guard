using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using EfCoreNPlusOneGuard;

namespace EfCoreNPlusOneGuard.Tests
{
    public sealed class JsonIncidentReporterTests : IDisposable
    {
        private readonly string _tempFilePath;
        private readonly JsonIncidentReporter _reporter;

        public JsonIncidentReporterTests()
        {
            // Create a unique temporary file for each test run.
            _tempFilePath = Path.GetTempFileName();

            // Ensure the file starts empty.
            File.WriteAllText(_tempFilePath, string.Empty);

            // Use NullLogger to avoid needing a real logger.
            _reporter = new JsonIncidentReporter(
                filePath: _tempFilePath,
                options: null,
                logger: NullLogger.Instance,
                aggregator: null);
        }

        public void Dispose()
        {
            // Dispose the reporter (flushes any pending writes) and delete the temp file.
            _reporter.Dispose();
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        /// <summary>
        /// Creates a minimal, non‑null <see cref="NPlusOneIncident"/> instance.
        /// The concrete type may not have a public parameterless constructor, so we
        /// obtain an instance via deserialization of an empty JSON object.
        /// </summary>
        private static NPlusOneIncident CreateSampleIncident()
        {
            // "{}" will deserialize to an instance with default property values.
            var incident = JsonSerializer.Deserialize<NPlusOneIncident>("{}");
            // The deserialization should never return null for a reference type.
            return incident ?? throw new InvalidOperationException("Failed to create sample incident.");
        }

        [Fact]
        public void Serialize_ReturnsValidJson()
        {
            // Arrange
            var incident = CreateSampleIncident();

            // Act
            string json = JsonIncidentReporter.Serialize(incident);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(json));

            // The JSON should be deserializable back to the same type.
            var deserialized = JsonSerializer.Deserialize<NPlusOneIncident>(json);
            Assert.NotNull(deserialized);
        }

        [Fact]
        public void Report_WritesJsonLineToFile()
        {
            // Arrange
            var incident = CreateSampleIncident();
            string expectedLine = JsonIncidentReporter.Serialize(incident);

            // Act
            _reporter.Report(incident);
            // Dispose to ensure the write is flushed.
            _reporter.Dispose();

            // Assert
            string[] lines = File.ReadAllLines(_tempFilePath);
            Assert.Single(lines);
            Assert.Equal(expectedLine, lines[0]);
        }

        [Fact]
        public void Constructor_AllowsNullOptionsAndLogger()
        {
            // The constructor should accept null for optional parameters without throwing.
            var ex = Record.Exception(() =>
                new JsonIncidentReporter(
                    filePath: _tempFilePath,
                    options: null,
                    logger: null,
                    aggregator: null));

            Assert.Null(ex);
        }

        [Fact]
        public void Report_NullIncident_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _reporter.Report(null!));
        }
    }
}
