// tests/NPlusOneIncidentJsonExtensionsJsonExtensionsTests.cs
using System;
using EfCoreNPlusOneGuard;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests;

public sealed class NPlusOneIncidentJsonExtensionsJsonExtensionsTests
{
    [Fact]
    public void ToJson_WithValidIncident_ReturnsNonEmptyJson()
    {
        // Arrange
        var incident = new NPlusOneIncident();

        // Act
        string json = NPlusOneIncidentJsonExtensionsJsonExtensions.ToJson(incident);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);
    }

    [Fact]
    public void ToJson_WithNullIncident_ThrowsArgumentNullException()
    {
        // Arrange
        NPlusOneIncident? incident = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NPlusOneIncidentJsonExtensionsJsonExtensions.ToJson(incident!));
    }

    [Fact]
    public void FromJson_WithValidJson_ReturnsIncident()
    {
        // Arrange
        var original = new NPlusOneIncident();
        string json = NPlusOneIncidentJsonExtensionsJsonExtensions.ToJson(original);

        // Act
        NPlusOneIncident? result = NPlusOneIncidentJsonExtensionsJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(result);
        // The round‑trip should produce an object of the same type.
        Assert.IsType<NPlusOneIncident>(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromJson_WithEmptyOrWhiteSpace_ReturnsNull(string input)
    {
        // Act
        NPlusOneIncident? result = NPlusOneIncidentJsonExtensionsJsonExtensions.FromJson(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_WithNullArgument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NPlusOneIncidentJsonExtensionsJsonExtensions.FromJson(null!));
    }

    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndIncident()
    {
        // Arrange
        var original = new NPlusOneIncident();
        string json = NPlusOneIncidentJsonExtensionsJsonExtensions.ToJson(original);

        // Act
        bool success = NPlusOneIncidentJsonExtensionsJsonExtensions.TryFromJson(json, out NPlusOneIncident? result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.IsType<NPlusOneIncident>(result);
    }

    [Fact]
    public void TryFromJson_WithInvalidJson_ReturnsFalseAndNull()
    {
        // Arrange
        const string invalidJson = "{ this is not valid json }";

        // Act
        bool success = NPlusOneIncidentJsonExtensionsJsonExtensions.TryFromJson(invalidJson, out NPlusOneIncident? result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_WithNullArgument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            NPlusOneIncidentJsonExtensionsJsonExtensions.TryFromJson(null!, out _));
    }
}
