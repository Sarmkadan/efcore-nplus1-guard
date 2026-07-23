using System;
using System.Text.Json;
using EfCoreNPlusOneGuard;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests;

public class QueryTrackerJsonExtensionsTests
{
    private static QueryTracker CreateTracker()
    {
        // QueryTracker requires NPlusOneGuardOptions; use defaults.
        var options = new NPlusOneGuardOptions();
        return new QueryTracker(options);
    }

    [Fact]
    public void ToJson_NullTracker_ThrowsArgumentNullException()
    {
        QueryTracker? tracker = null;
        Assert.Throws<ArgumentNullException>(() => tracker!.ToJson());
    }

    [Fact]
    public void ToJson_DefaultIndentation_ReturnsCompactJson()
    {
        var tracker = CreateTracker();
        string json = tracker.ToJson();

        Assert.False(string.IsNullOrWhiteSpace(json));
        // Compact JSON should not contain line breaks.
        Assert.DoesNotContain('\n', json);
    }

    [Fact]
    public void ToJson_Indented_ReturnsPrettyJson()
    {
        var tracker = CreateTracker();
        string json = tracker.ToJson(indented: true);

        Assert.False(string.IsNullOrWhiteSpace(json));
        // Indented JSON should contain line breaks.
        Assert.Contains('\n', json);
    }

    [Fact]
    public void FromJson_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => QueryTrackerJsonExtensions.FromJson(null!));
    }

    [Fact]
    public void FromJson_WhiteSpaceInput_ReturnsNull()
    {
        QueryTracker? result = QueryTrackerJsonExtensions.FromJson("   ");
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsJsonException()
    {
        const string invalidJson = "{ this is not valid json }";
        Assert.Throws<JsonException>(() => QueryTrackerJsonExtensions.FromJson(invalidJson));
    }

    [Fact]
    public void FromJson_ValidJson_RoundTripsSuccessfully()
    {
        var original = CreateTracker();
        string json = original.ToJson();

        QueryTracker? deserialized = QueryTrackerJsonExtensions.FromJson(json);
        Assert.NotNull(deserialized);
    }

    [Fact]
    public void TryFromJson_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => QueryTrackerJsonExtensions.TryFromJson(null!, out _));
    }

    [Fact]
    public void TryFromJson_WhiteSpaceInput_ReturnsFalse()
    {
        bool success = QueryTrackerJsonExtensions.TryFromJson("   ", out QueryTracker? value);
        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalse()
    {
        const string invalidJson = "[ not a valid json }";
        bool success = QueryTrackerJsonExtensions.TryFromJson(invalidJson, out QueryTracker? value);
        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndValue()
    {
        var tracker = CreateTracker();
        string json = tracker.ToJson();

        bool success = QueryTrackerJsonExtensions.TryFromJson(json, out QueryTracker? value);
        Assert.True(success);
        Assert.NotNull(value);
    }
}
