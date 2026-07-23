using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EfCoreNPlusOneGuard;

public class NPlusOneIncidentExtensionsTests
{
    [Fact]
    public void CompareBySeverityAndCount_HighSeverityFirst()
    {
        // Arrange
        var highSeverity = new NPlusOneIncident { Severity = NPlusOneSeverity.High, Count = 1 };
        var lowSeverity = new NPlusOneIncident { Severity = NPlusOneSeverity.Low, Count = 2 };

        // Act
        var result = NPlusOneIncidentExtensions.CompareBySeverityAndCount(highSeverity, lowSeverity);

        // Assert
        Assert.True(result < 0);
    }

    [Fact]
    public void FilterBySeverity_ReturnsIncidentsWithMatchingSeverity()
    {
        // Arrange
        var incidents = new List<NPlusOneIncident>
        {
            new NPlusOneIncident { Severity = NPlusOneSeverity.High },
            new NPlusOneIncident { Severity = NPlusOneSeverity.Medium },
            new NPlusOneIncident { Severity = NPlusOneSeverity.High },
        };

        // Act
        var result = NPlusOneIncidentExtensions.FilterBySeverity(incidents, NPlusOneSeverity.High);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void GroupByQueryPattern_GroupsIncidentsByQueryPattern()
    {
        // Arrange
        var incidents = new List<NPlusOneIncident>
        {
            new NPlusOneIncident { SqlQuery = "SELECT * FROM Table1" },
            new NPlusOneIncident { SqlQuery = "SELECT * FROM Table1" },
            new NPlusOneIncident { SqlQuery = "SELECT * FROM Table2" },
        };

        // Act
        var result = NPlusOneIncidentExtensions.GroupByQueryPattern(incidents);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result["SELECT * FROM Table1"].Count);
        Assert.Equal(1, result["SELECT * FROM Table2"].Count);
    }

    [Fact]
    public void TotalCount_ReturnsTotalCountOfAllIncidents()
    {
        // Arrange
        var incidents = new List<NPlusOneIncident>
        {
            new NPlusOneIncident { Count = 1 },
            new NPlusOneIncident { Count = 2 },
            new NPlusOneIncident { Count = 3 },
        };

        // Act
        var result = NPlusOneIncidentExtensions.TotalCount(incidents);

        // Assert
        Assert.Equal(6, result);
    }

    [Fact]
    public void OrderBySeverityAndCount_ReturnsIncidentsInSortedOrder()
    {
        // Arrange
        var incidents = new List<NPlusOneIncident>
        {
            new NPlusOneIncident { Severity = NPlusOneSeverity.Low, Count = 1 },
            new NPlusOneIncident { Severity = NPlusOneSeverity.High, Count = 2 },
            new NPlusOneIncident { Severity = NPlusOneSeverity.Medium, Count = 3 },
        };

        // Act
        var result = NPlusOneIncidentExtensions.OrderBySeverityAndCount(incidents);

        // Assert
        Assert.Equal(NPlusOneSeverity.High, result.First().Severity);
        Assert.Equal(NPlusOneSeverity.Medium, result.ElementAt(1).Severity);
        Assert.Equal(NPlusOneSeverity.Low, result.ElementAt(2).Severity);
    }

    [Fact]
    public void GetMostSevere_ReturnsMostSevereIncident()
    {
        // Arrange
        var incidents = new List<NPlusOneIncident>
        {
            new NPlusOneIncident { Severity = NPlusOneSeverity.Low },
            new NPlusOneIncident { Severity = NPlusOneSeverity.High },
            new NPlusOneIncident { Severity = NPlusOneSeverity.Medium },
        };

        // Act
        var result = NPlusOneIncidentExtensions.GetMostSevere(incidents);

        // Assert
        Assert.Equal(NPlusOneSeverity.High, result.Severity);
    }

    [Fact]
    public void ToSummaryString_ReturnsSummaryString()
    {
        // Arrange
        var incidents = new List<NPlusOneIncident>
        {
            new NPlusOneIncident { Severity = NPlusOneSeverity.High, Count = 1 },
            new NPlusOneIncident { Severity = NPlusOneSeverity.Medium, Count = 2 },
            new NPlusOneIncident { Severity = NPlusOneSeverity.Low, Count = 3 },
        };

        // Act
        var result = NPlusOneIncidentExtensions.ToSummaryString(incidents);

        // Assert
        Assert.Contains("Total: 6", result);
        Assert.Contains("High: 1", result);
        Assert.Contains("Medium: 2", result);
        Assert.Contains("Low: 3", result);
    }
}
