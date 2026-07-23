using EfCoreNPlusOneGuard;
using System.Text.Json;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests;

public class NPlusOneIncidentJsonExtensionsTests
{
    [Fact]
    public void ToJson_HappyPath_ReturnsJsonString()
    {
        // Arrange
        var incident = new NPlusOneIncident();

        // Act
        var json = NPlusOneIncidentJsonExtensions.ToJson(incident);

        // Assert
        Assert.NotEmpty(json);
    }

    [Fact]
    public void ToJson_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => NPlusOneIncidentJsonExtensions.ToJson(null));
    }

    [Fact]
    public void FromJson_HappyPath_ReturnsNPlusOneIncident()
    {
        // Arrange
        var incident = new NPlusOneIncident();
        var json = NPlusOneIncidentJsonExtensions.ToJson(incident);

        // Act
        var deserializedIncident = NPlusOneIncidentJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserializedIncident);
    }

    [Fact]
    public void FromJson_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => NPlusOneIncidentJsonExtensions.FromJson(null));
    }

    [Fact]
    public void FromJson_EmptyJson_ReturnsNull()
    {
        // Act
        var deserializedIncident = NPlusOneIncidentJsonExtensions.FromJson(string.Empty);

        // Assert
        Assert.Null(deserializedIncident);
    }

    [Fact]
    public void TryFromJson_HappyPath_ReturnsTrueAndDeserializedIncident()
    {
        // Arrange
        var incident = new NPlusOneIncident();
        var json = NPlusOneIncidentJsonExtensions.ToJson(incident);

        // Act
        var success = NPlusOneIncidentJsonExtensions.TryFromJson(json, out var deserializedIncident);

        // Assert
        Assert.True(success);
        Assert.NotNull(deserializedIncident);
    }

    [Fact]
    public void TryFromJson_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => NPlusOneIncidentJsonExtensions.TryFromJson(null, out _));
    }

    [Fact]
    public void TryFromJson_EmptyJson_ReturnsFalseAndNull()
    {
        // Act
        var success = NPlusOneIncidentJsonExtensions.TryFromJson(string.Empty, out var deserializedIncident);

        // Assert
        Assert.False(success);
        Assert.Null(deserializedIncident);
    }
}
