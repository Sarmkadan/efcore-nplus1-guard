using System;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests
{
    public class DuplicateQueryDetectorTests
    {
        [Fact]
        public void Record_WithNullSql_ThrowsArgumentNullException()
        {
            // Arrange
            var detector = new DuplicateQueryDetector();
            string nullSql = null!;
            var parameters = "@p0";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => detector.Record(nullSql, parameters));
        }

        [Fact]
        public void Record_WithEmptySql_ThrowsArgumentException()
        {
            // Arrange
            var detector = new DuplicateQueryDetector();
            var emptySql = string.Empty;
            var parameters = "@p0";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => detector.Record(emptySql, parameters));
        }

        [Fact]
        public void Record_WithWhitespaceSql_ThrowsArgumentException()
        {
            // Arrange
            var detector = new DuplicateQueryDetector();
            var whitespaceSql = "   ";
            var parameters = "@p0";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => detector.Record(whitespaceSql, parameters));
        }

        [Fact]
        public void GetDuplicates_WithNoRecords_ReturnsEmptyList()
        {
            // Arrange
            var detector = new DuplicateQueryDetector();

            // Act
            var duplicates = detector.GetDuplicates();

            // Assert
            Assert.Empty(duplicates);
        }

        [Fact]
        public void GetDuplicates_WithSingleRecord_ReturnsEmptyList()
        {
            // Arrange
            var detector = new DuplicateQueryDetector();
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var parameters = "@p0";

            // Act
            detector.Record(sql, parameters);
            var duplicates = detector.GetDuplicates();

            // Assert
            Assert.Empty(duplicates);
        }

        [Fact]
        public void GetDuplicates_WithTwoRecordsSameQuery_ReturnsDuplicateGroup()
        {
            // Arrange
            var detector = new DuplicateQueryDetector(threshold: 2);
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var parameters = "@p0";

            // Act
            detector.Record(sql, parameters);
            detector.Record(sql, parameters);
            var duplicates = detector.GetDuplicates();

            // Assert
            Assert.Single(duplicates);
            var duplicate = Assert.Single(duplicates);
            Assert.Equal(sql, duplicate.Sql);
            Assert.Equal(parameters, duplicate.Parameters);
            Assert.Equal(2, duplicate.Count);
        }

        [Fact]
        public void GetDuplicates_WithThreeRecordsSameQuery_ReturnsDuplicateGroupWithCountThree()
        {
            // Arrange
            var detector = new DuplicateQueryDetector(threshold: 2);
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var parameters = "@p0";

            // Act
            detector.Record(sql, parameters);
            detector.Record(sql, parameters);
            detector.Record(sql, parameters);
            var duplicates = detector.GetDuplicates();

            // Assert
            Assert.Single(duplicates);
            var duplicate = Assert.Single(duplicates);
            Assert.Equal(sql, duplicate.Sql);
            Assert.Equal(parameters, duplicate.Parameters);
            Assert.Equal(3, duplicate.Count);
        }

        [Fact]
        public void GetDuplicates_WithRecordsBelowThreshold_ReturnsEmptyList()
        {
            // Arrange
            var detector = new DuplicateQueryDetector(threshold: 3);
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var parameters = "@p0";

            // Act
            detector.Record(sql, parameters);
            detector.Record(sql, parameters);  // Only 2 records, below threshold of 3
            var duplicates = detector.GetDuplicates();

            // Assert
            Assert.Empty(duplicates);
        }

        [Fact]
        public void GetDuplicates_WithMultipleDistinctQueries_ReturnsEmptyList()
        {
            // Arrange
            var detector = new DuplicateQueryDetector(threshold: 2);
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Orders WHERE UserId = @p0";
            var sql3 = "SELECT * FROM Products WHERE CategoryId = @p0";

            // Act
            detector.Record(sql1, "@p0");
            detector.Record(sql2, "@p0");
            detector.Record(sql3, "@p0");
            var duplicates = detector.GetDuplicates();

            // Assert
            Assert.Empty(duplicates);
        }

        [Fact]
        public void GetDuplicates_WithOneDuplicateAndOneDistinct_ReturnsOnlyDuplicate()
        {
            // Arrange
            var detector = new DuplicateQueryDetector(threshold: 2);
            var duplicateSql = "SELECT * FROM Users WHERE Id = @p0";
            var distinctSql = "SELECT * FROM Orders WHERE UserId = @p0";

            // Act
            detector.Record(duplicateSql, "@p0");
            detector.Record(duplicateSql, "@p0");
            detector.Record(distinctSql, "@p0");
            var duplicates = detector.GetDuplicates();

            // Assert
            Assert.Single(duplicates);
            var duplicate = Assert.Single(duplicates);
            Assert.Equal(duplicateSql, duplicate.Sql);
            Assert.Equal(2, duplicate.Count);
        }

        [Fact]
        public void GetDuplicates_WithMultipleDuplicateGroups_ReturnsAllGroups()
        {
            // Arrange
            var detector = new DuplicateQueryDetector(threshold: 2);
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Orders WHERE UserId = @p0";
            var sql3 = "SELECT * FROM Products WHERE CategoryId = @p0";

            // Act
            detector.Record(sql1, "@p0");
            detector.Record(sql1, "@p0");
            detector.Record(sql2, "@p1");
            detector.Record(sql2, "@p1");
            detector.Record(sql3, "@p2");
            detector.Record(sql3, "@p2");
            var duplicates = detector.GetDuplicates();

            // Assert
            Assert.Equal(3, duplicates.Count);
            Assert.Contains(duplicates, d => d.Sql == sql1 && d.Count == 2);
            Assert.Contains(duplicates, d => d.Sql == sql2 && d.Count == 2);
            Assert.Contains(duplicates, d => d.Sql == sql3 && d.Count == 2);
        }

        [Fact]
        public void GetDuplicates_WithDifferentParametersSameSql_DetectsAsDifferentQueries()
        {
            // Arrange
            var detector = new DuplicateQueryDetector(threshold: 2);
            var sql = "SELECT * FROM Users WHERE Status = @p0";

            // Act
            detector.Record(sql, "@p0=Active");
            detector.Record(sql, "@p0=Inactive");
            detector.Record(sql, "@p0=Active");
            var duplicates = detector.GetDuplicates();

            // Assert - Different parameters mean different queries, so the query with "Active" appears twice
            Assert.Single(duplicates);
            var duplicate = Assert.Single(duplicates);
            Assert.Equal(sql, duplicate.Sql);
            Assert.Equal("@p0=Active", duplicate.Parameters);
            Assert.Equal(2, duplicate.Count);
        }

        [Fact]
        public void GetDuplicates_AfterClear_ReturnsEmptyList()
        {
            // Arrange
            var detector = new DuplicateQueryDetector(threshold: 2);
            var sql = "SELECT * FROM Users WHERE Id = @p0";

            detector.Record(sql, "@p0");
            detector.Record(sql, "@p0");
            Assert.Single(detector.GetDuplicates());

            // Act
            detector.Clear();
            var duplicates = detector.GetDuplicates();

            // Assert
            Assert.Empty(duplicates);
        }

        [Fact]
        public void DuplicateQueryGroup_HasCorrectProperties()
        {
            // Arrange
            var detector = new DuplicateQueryDetector(threshold: 2);
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var parameters = "@p0=1";

            // Act
            detector.Record(sql, parameters);
            detector.Record(sql, parameters);
            var duplicates = detector.GetDuplicates();
            var duplicate = Assert.Single(duplicates);

            // Assert
            Assert.Equal(sql, duplicate.Sql);
            Assert.Equal(parameters, duplicate.Parameters);
            Assert.Equal(2, duplicate.Count);
        }

        [Fact]
        public void Constructor_WithDefaultThreshold_UsesTwoAsDefault()
        {
            // Arrange & Act
            var detector = new DuplicateQueryDetector();

            // Assert
            Assert.Equal(2, detector.GetThreshold());
        }

        [Fact]
        public void Constructor_WithCustomThreshold_UsesProvidedThreshold()
        {
            // Arrange & Act
            var detector = new DuplicateQueryDetector(threshold: 5);

            // Assert
            Assert.Equal(5, detector.GetThreshold());
        }

        [Fact]
        public void GetDuplicates_WithThresholdOne_FlagsAllQueries()
        {
            // Arrange
            var detector = new DuplicateQueryDetector(threshold: 1);
            var sql = "SELECT * FROM Users WHERE Id = @p0";

            // Act
            detector.Record(sql, "@p0");
            var duplicates = detector.GetDuplicates();

            // Assert - With threshold of 1, even a single execution is flagged
            Assert.Single(duplicates);
            var duplicate = Assert.Single(duplicates);
            Assert.Equal(sql, duplicate.Sql);
            Assert.Equal(1, duplicate.Count);
        }
    }
}
