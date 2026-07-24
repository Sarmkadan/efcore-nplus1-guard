// Copyright (c) 2024
// SPDX-License-Identifier: MIT

using System;
using EfCoreNPlusOneGuard;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests;

public class CallSiteWhitelistJsonExtensionsTests
{
    [Fact]
    public void ToJson_NullArgument_ThrowsArgumentNullException()
    {
        CallSiteWhitelist? whitelist = null;
        Assert.Throws<ArgumentNullException>(() => whitelist!.ToJson());
    }

    [Fact]
    public void FromJson_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CallSiteWhitelistJsonExtensions.FromJson(null!));
        Assert.Throws<ArgumentException>(() => CallSiteWhitelistJsonExtensions.FromJson(string.Empty));
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalse()
    {
        var invalidJson = "{ this is not valid json }";
        var result = CallSiteWhitelistJsonExtensions.TryFromJson(invalidJson, out var whitelist);
        Assert.False(result);
        Assert.Null(whitelist);
    }

    [Fact]
    public void RoundTrip_EmptyWhitelist_ProducesEmptyArray()
    {
        var empty = new CallSiteWhitelist();
        var json = empty.ToJson(indented: true);
        Assert.Equal("[]", json.Trim());

        var deserialized = CallSiteWhitelistJsonExtensions.FromJson(json);
        Assert.NotNull(deserialized);
        var jsonAgain = deserialized!.ToJson();
        Assert.Equal(json, jsonAgain);
    }

    [Fact]
    public void RoundTrip_WithVariousEntries_PreservesData()
    {
        var whitelist = new CallSiteWhitelist();
        var expiry = DateTimeOffset.UtcNow.AddHours(1);

        whitelist.Add("Exact.Type", "ExactMethod", expiry);
        whitelist.AddPattern("Pattern.*", expiry);
        whitelist.AddFingerprint("abc123def456", expiry);

        var json = whitelist.ToJson(indented: false);
        var deserialized = CallSiteWhitelistJsonExtensions.FromJson(json);
        Assert.NotNull(deserialized);

        // Serialize again and compare – the converter should produce identical JSON.
        var jsonRoundTrip = deserialized!.ToJson(indented: false);
        Assert.Equal(json, jsonRoundTrip);
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndWhitelist()
    {
        var whitelist = new CallSiteWhitelist();
        whitelist.Add("Some.Type");
        var json = whitelist.ToJson();

        var success = CallSiteWhitelistJsonExtensions.TryFromJson(json, out var result);
        Assert.True(success);
        Assert.NotNull(result);

        // Verify round‑trip consistency.
        var json2 = result!.ToJson();
        Assert.Equal(json, json2);
    }

    [Fact]
    public void ToJson_Indented_ProducesReadableFormatting()
    {
        var whitelist = new CallSiteWhitelist();
        whitelist.AddPattern("MyPattern*");

        var json = whitelist.ToJson(indented: true);
        // Indented JSON should contain line breaks.
        Assert.Contains(Environment.NewLine, json);
        // Still a valid JSON array.
        var deserialized = CallSiteWhitelistJsonExtensions.FromJson(json);
        Assert.NotNull(deserialized);
    }
}
