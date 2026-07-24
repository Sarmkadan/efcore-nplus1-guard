using System;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests;

public class QueryStatisticsValidationTests
{
    [Fact]
    public void Validate_QueryStatistics_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        QueryStatistics? stats = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => QueryStatisticsValidation.Validate(stats));
    }

    [Fact]
    public void Validate_QueryStatistics_WithValidEntries_ReturnsSuccessResult()
    {
        // Arrange
        var stats = new QueryStatistics();
        stats.Record("SELECT * FROM Users", TimeSpan.FromMilliseconds(10));
        stats.Record("SELECT * FROM Orders", TimeSpan.FromMilliseconds(25));

        // Act
        var result = QueryStatisticsValidation.Validate(stats);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.ValidationErrors);
    }

    [Fact]
    public void Validate_QueryStatistics_WithInvalidEntries_ReturnsFailureResult()
    {
        // Arrange
        var stats = new QueryStatistics();
        stats.Record("   ", TimeSpan.FromMilliseconds(5));

        // Act
        var result = QueryStatisticsValidation.Validate(stats);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, error => error.Contains("Sql cannot be null or whitespace"));
    }

    [Fact]
    public void IsValid_QueryStatistics_WithNullValue_ReturnsFalse()
    {
        // Arrange
        QueryStatistics? stats = null;

        // Act & Assert
        Assert.False(QueryStatisticsValidation.IsValid(stats));
    }

    [Fact]
    public void IsValid_QueryStatistics_WithValidEntries_ReturnsTrue()
    {
        // Arrange
        var stats = new QueryStatistics();
        stats.Record("SELECT * FROM Users", TimeSpan.FromMilliseconds(10));

        // Act & Assert
        Assert.True(QueryStatisticsValidation.IsValid(stats));
    }

    [Fact]
    public void EnsureValid_QueryStatistics_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        QueryStatistics? stats = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => QueryStatisticsValidation.EnsureValid(stats));
    }

    [Fact]
    public void Validate_QueryStatEntry_WithValidValues_ReturnsSuccessResult()
    {
        // Arrange
        var entry = new QueryStatistics.QueryStatEntry("SELECT * FROM Users", 5, TimeSpan.FromMilliseconds(10));

        // Act
        var result = QueryStatisticsValidation.Validate(entry);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.ValidationErrors);
    }

    [Fact]
    public void Validate_QueryStatEntry_WithWhitespaceSql_ReturnsFailureResult()
    {
        // Arrange
        var entry = new QueryStatistics.QueryStatEntry("   ", 5, TimeSpan.FromMilliseconds(10));

        // Act
        var result = QueryStatisticsValidation.Validate(entry);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.ValidationErrors);
        Assert.Contains("Sql cannot be null or whitespace", result.ValidationErrors[0]);
    }

    [Fact]
    public void Validate_QueryStatEntry_WithNegativeCount_ReturnsFailureResult()
    {
        // Arrange
        var entry = new QueryStatistics.QueryStatEntry("SELECT * FROM Users", -1, TimeSpan.FromMilliseconds(10));

        // Act
        var result = QueryStatisticsValidation.Validate(entry);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.ValidationErrors);
        Assert.Contains("Count cannot be negative", result.ValidationErrors[0]);
    }

    [Fact]
    public void EnsureValid_QueryStatEntry_WithInvalidSql_ThrowsArgumentException()
    {
        // Arrange
        var entry = new QueryStatistics.QueryStatEntry("   ", 5, TimeSpan.FromMilliseconds(10));

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => QueryStatisticsValidation.EnsureValid(entry));
        Assert.Contains("Sql cannot be null or whitespace", exception.Message);
    }
}