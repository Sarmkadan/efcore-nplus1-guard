using Xunit;
using EfCoreNPlusOneGuard;
using System;

namespace EfCoreNPlusOneGuard;

public class DuplicateQueryDetectorExtensionsTests
{
    [Fact]
    public void Record_HappyPath_DoesNotThrow()
    {
        // Arrange
        var detector = new DuplicateQueryDetector();
        var sql = "SELECT * FROM table";

        // Act and Assert
        detector.Record(sql);
    }

    [Fact]
    public void Record_NullDetector_ThrowsArgumentNullException()
    {
        // Arrange
        DuplicateQueryDetector? detector = null;
        var sql = "SELECT * FROM table";

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => detector.Record(sql));
    }

    [Fact]
    public void Record_NullSql_ThrowsArgumentException()
    {
        // Arrange
        var detector = new DuplicateQueryDetector();
        string? sql = null;

        // Act and Assert
        Assert.Throws<ArgumentException>(() => detector.Record(sql));
    }

    [Fact]
    public void HasDuplicates_HappyPath_ReturnsTrue()
    {
        // Arrange
        var detector = new DuplicateQueryDetector();
        var sql = "SELECT * FROM table";
        detector.Record(sql);
        detector.Record(sql);

        // Act and Assert
        Assert.True(detector.HasDuplicates());
    }

    [Fact]
    public void HasDuplicates_NoDuplicates_ReturnsFalse()
    {
        // Arrange
        var detector = new DuplicateQueryDetector();
        var sql = "SELECT * FROM table";
        detector.Record(sql);

        // Act and Assert
        Assert.False(detector.HasDuplicates());
    }

    [Fact]
    public void GetTotalDuplicateCount_HappyPath_ReturnsCorrectCount()
    {
        // Arrange
        var detector = new DuplicateQueryDetector();
        var sql = "SELECT * FROM table";
        detector.Record(sql);
        detector.Record(sql);

        // Act and Assert
        Assert.Equal(1, detector.GetTotalDuplicateCount());
    }

    [Fact]
    public void GetTotalDuplicateCount_NoDuplicates_ReturnsZero()
    {
        // Arrange
        var detector = new DuplicateQueryDetector();
        var sql = "SELECT * FROM table";
        detector.Record(sql);

        // Act and Assert
        Assert.Equal(0, detector.GetTotalDuplicateCount());
    }

    [Fact]
    public void GetTotalDuplicateCount_NullDetector_ThrowsArgumentNullException()
    {
        // Arrange
        DuplicateQueryDetector? detector = null;

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => detector.GetTotalDuplicateCount());
    }
}
