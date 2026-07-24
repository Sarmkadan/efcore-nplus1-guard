using System;
using Xunit;
using EfCoreNPlusOneGuard;

namespace EfCoreNPlusOneGuard.Tests;

public class DuplicateQueryDetectorJsonExtensionsTests
{
    [Fact]
    public void ToJson_WithValidDetector_ReturnsNonNullString()
    {
        // Arrange
        var detector = new DuplicateQueryDetector();

        // Act
        var json = detector.ToJson();

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);
    }

    [Fact]
    public void ToJson_WithNullDetector_ThrowsArgumentNullException()
    {
        // Arrange
        DuplicateQueryDetector? detector = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => detector.ToJson());
    }

    [Fact]
    public void ToJson_WithIndentedTrue_ReturnsFormattedJson()
    {
        // Arrange
        var detector = new DuplicateQueryDetector();

        // Act
        var json = detector.ToJson(indented: true);

        // Assert
        Assert.Contains("\n", json);
        Assert.Contains("  ", json);
    }

    [Fact]
    public void FromJson_WithValidJson_ReturnsDetector()
    {
        // Arrange
        var detector = new DuplicateQueryDetector();
        var json = detector.ToJson();

        // Act
        var result = DuplicateQueryDetectorJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void FromJson_WithNullOrEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => DuplicateQueryDetectorJsonExtensions.FromJson(null));
        Assert.Throws<ArgumentException>(() => DuplicateQueryDetectorJsonExtensions.FromJson(string.Empty));
    }

    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndDetector()
    {
        // Arrange
        var detector = new DuplicateQueryDetector();
        var json = detector.ToJson();

        // Act
        var success = DuplicateQueryDetectorJsonExtensions.TryFromJson(json, out var result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
    }

    [Fact]
    public void TryFromJson_WithInvalidJson_ReturnsFalseAndNull()
    {
        // Act
        var success = DuplicateQueryDetectorJsonExtensions.TryFromJson("{not valid json}", out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_WithNullOrEmptyString_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => DuplicateQueryDetectorJsonExtensions.TryFromJson(null, out _));
        Assert.Throws<ArgumentException>(() => DuplicateQueryDetectorJsonExtensions.TryFromJson(string.Empty, out _));
    }
}
