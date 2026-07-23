using System;
using System.Collections.Generic;
using EfCoreNPlusOneGuard;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests;

public class NPlusOneGuardOptionsExtensionsTests
{
    private static NPlusOneGuardOptions CreateOptions(
        IEnumerable<string>? ignored = null,
        int threshold = 10,
        TimeSpan? detectionWindow = null,
        bool throwOnDetection = false,
        bool logOnDetection = true)
    {
        return new NPlusOneGuardOptions
        {
            IgnoredQueryPatterns = new List<string>(ignored ?? Array.Empty<string>()),
            Threshold = threshold,
            DetectionWindow = detectionWindow ?? TimeSpan.FromMinutes(5),
            ThrowOnDetection = throwOnDetection,
            LogOnDetection = logOnDetection
        };
    }

    [Fact]
    public void IsQueryIgnored_ReturnsTrue_WhenPatternMatches()
    {
        var options = CreateOptions(ignored: new[] { "SELECT * FROM Users", "orders" });

        Assert.True(options.IsQueryIgnored("select * from users where id = 1"));
        Assert.True(options.IsQueryIgnored("GetOrdersByCustomer"));
    }

    [Fact]
    public void IsQueryIgnored_ReturnsFalse_WhenNoPatternMatches()
    {
        var options = CreateOptions(ignored: new[] { "SELECT * FROM Users" });

        Assert.False(options.IsQueryIgnored("SELECT * FROM Products"));
    }

    [Fact]
    public void IsQueryIgnored_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => NPlusOneGuardOptionsExtensions.IsQueryIgnored(null!, "any"));
    }

    [Fact]
    public void IsQueryIgnored_ThrowsArgumentException_WhenQueryPatternIsNullOrEmpty()
    {
        var options = CreateOptions();

        Assert.Throws<ArgumentException>(() => options.IsQueryIgnored(null!));
        Assert.Throws<ArgumentException>(() => options.IsQueryIgnored(string.Empty));
    }

    [Fact]
    public void GetThresholdString_ReturnsFormattedString()
    {
        var options = CreateOptions(threshold: 12345);

        var result = options.GetThresholdString();

        Assert.Equal("12,345", result); // invariant culture uses comma as thousands separator
    }

    [Fact]
    public void GetThresholdString_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => NPlusOneGuardOptionsExtensions.GetThresholdString(null!));
    }

    [Fact]
    public void GetDetectionWindowString_ReturnsGeneralFormat()
    {
        var window = new TimeSpan(1, 2, 3, 4, 500);
        var options = CreateOptions(detectionWindow: window);

        var result = options.GetDetectionWindowString();

        // "g" format for TimeSpan: d.hh:mm:ss.fffffff (fraction trimmed)
        var expected = window.ToString("g", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetDetectionWindowString_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => NPlusOneGuardOptionsExtensions.GetDetectionWindowString(null!));
    }

    [Fact]
    public void IsThrowOnDetectionEnabled_ReturnsConfiguredValue()
    {
        var options = CreateOptions(throwOnDetection: true);
        Assert.True(options.IsThrowOnDetectionEnabled());

        options = CreateOptions(throwOnDetection: false);
        Assert.False(options.IsThrowOnDetectionEnabled());
    }

    [Fact]
    public void IsLogOnDetectionEnabled_ReturnsConfiguredValue()
    {
        var options = CreateOptions(logOnDetection: true);
        Assert.True(options.IsLogOnDetectionEnabled());

        options = CreateOptions(logOnDetection: false);
        Assert.False(options.IsLogOnDetectionEnabled());
    }

    [Fact]
    public void IsLogOnDetectionEnabled_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => NPlusOneGuardOptionsExtensions.IsLogOnDetectionEnabled(null!));
    }

    [Fact]
    public void IsThrowOnDetectionEnabled_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => NPlusOneGuardOptionsExtensions.IsThrowOnDetectionEnabled(null!));
    }
}
