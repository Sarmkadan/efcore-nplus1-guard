using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests
{
    /// <summary>
    /// Tests for IncidentAggregator.TopOffender ranking logic with focus on edge cases,
    /// ties, and deterministic ordering behavior.
    /// </summary>
    public class IncidentAggregatorTopOffenderTests
    {
        [Fact]
        public void GetTopOffenders_WithEmptyInput_ReturnsEmptyList()
        {
            // Arrange
            var aggregator = new IncidentAggregator();

            // Act
            var topOffenders = aggregator.GetTopOffenders(5);

            // Assert
            Assert.Empty(topOffenders);
        }

        [Fact]
        public void GetTopOffenders_WithSingleIncidentSource_ReturnsSingleTopOffenderWithCorrectCount()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
            var incident = new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Users WHERE Id = @p0",
                Count = 1,
                Severity = NPlusOneSeverity.Medium,
                StackTrace = "at UserRepository.GetUser()",
                CallSite = "UserRepository.GetUser"
            };

            aggregator.Add(incident);

            // Act
            var topOffenders = aggregator.GetTopOffenders(5);

            // Assert
            Assert.Single(topOffenders);
            var offender = topOffenders[0];
            Assert.Equal("SELECT * FROM Users WHERE Id = @p0", offender.Fingerprint);
            Assert.Equal(1, offender.Count); // Count represents number of incidents with this fingerprint
        }

        [Fact]
        public void GetTopOffenders_WithTwoOffendersWithTiedCount_ReturnsBothWithDeterministicOrder()
        {
            // Arrange
            var aggregator = new IncidentAggregator();

            // Create two incidents with the same fingerprint pattern (tied ranking by count)
            // The Count property on TopOffender represents the number of incidents with that fingerprint
            var incident1 = new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Users WHERE Id = @p0",
                Count = 1,
                Severity = NPlusOneSeverity.Medium,
                StackTrace = "at UserRepository.GetUser()",
                CallSite = "UserRepository.GetUser"
            };

            var incident2 = new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Orders WHERE UserId = @p0",
                Count = 1, // Same count - creates a tie in the ranking
                Severity = NPlusOneSeverity.High,
                StackTrace = "at OrderRepository.GetOrders()",
                CallSite = "OrderRepository.GetOrders"
            };

            aggregator.Add(incident1);
            aggregator.Add(incident2);

            // Act - Call multiple times to verify deterministic ordering
            var topOffenders1 = aggregator.GetTopOffenders(2);
            var topOffenders2 = aggregator.GetTopOffenders(2);
            var topOffenders3 = aggregator.GetTopOffenders(2);

            // Assert - Should return both offenders (tied on count of 1 each)
            Assert.Equal(2, topOffenders1.Count);
            Assert.Equal(2, topOffenders2.Count);
            Assert.Equal(2, topOffenders3.Count);

            // Assert - Both should have count of 1 (one incident each)
            Assert.Equal(1, topOffenders1[0].Count);
            Assert.Equal(1, topOffenders1[1].Count);

            // Assert - Deterministic ordering: should always return in the same order
            Assert.Equal(topOffenders1[0].Fingerprint, topOffenders2[0].Fingerprint);
            Assert.Equal(topOffenders1[1].Fingerprint, topOffenders2[1].Fingerprint);
            Assert.Equal(topOffenders1[0].Fingerprint, topOffenders3[0].Fingerprint);
            Assert.Equal(topOffenders1[1].Fingerprint, topOffenders3[1].Fingerprint);
        }

        [Fact]
        public void GetTopOffenders_WithMultipleOffendersWithTiedCounts_ReturnsDeterministicOrdering()
        {
            // Arrange
            var aggregator = new IncidentAggregator();

            // Create multiple incidents with the same count to test tie-breaking
            var incidents = new NPlusOneIncident[5];
            for (int i = 0; i < 5; i++)
            {
                incidents[i] = new NPlusOneIncident
                {
                    SqlQuery = $"SELECT * FROM Table{i}",
                    Count = 1, // All have the same count
                    Severity = NPlusOneSeverity.Medium,
                    StackTrace = $"at TestClass.TestMethod{i}()",
                    CallSite = $"TestClass.TestMethod{i}"
                };
                aggregator.Add(incidents[i]);
            }

            // Act - Request more offenders than available
            var topOffenders1 = aggregator.GetTopOffenders(10);
            var topOffenders2 = aggregator.GetTopOffenders(10);

            // Assert - Should return all 5 offenders
            Assert.Equal(5, topOffenders1.Count);
            Assert.Equal(5, topOffenders2.Count);

            // Assert - All should have count of 1
            foreach (var offender in topOffenders1)
            {
                Assert.Equal(1, offender.Count);
            }

            // Assert - Deterministic ordering across multiple calls
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(topOffenders1[i].Fingerprint, topOffenders2[i].Fingerprint);
            }
        }

        [Fact]
        public void GetTopOffenders_WithTiedCounts_OrdersByLastSeenDescending()
        {
            // Arrange
            var aggregator = new IncidentAggregator();

            var incident1 = new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Table1",
                Count = 1,
                Severity = NPlusOneSeverity.Medium,
                StackTrace = "at TestClass.TestMethod1()",
                CallSite = "TestClass.TestMethod1"
            };

            var incident2 = new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Table2",
                Count = 1, // Same count - creates a tie
                Severity = NPlusOneSeverity.High,
                StackTrace = "at TestClass.TestMethod2()",
                CallSite = "TestClass.TestMethod2"
            };

            // Add incident1 first
            aggregator.Add(incident1);

            // Wait a bit to ensure different timestamps
            System.Threading.Thread.Sleep(20);

            // Add incident2 (will have a later timestamp)
            aggregator.Add(incident2);

            // Act
            var topOffenders = aggregator.GetTopOffenders(2);

            // Assert - Both have same count
            Assert.Equal(1, topOffenders[0].Count);
            Assert.Equal(1, topOffenders[1].Count);

            // Assert - Should be ordered by LastSeen descending (most recent first)
            // incident2 was added after incident1, so it should come first
            Assert.Equal("SELECT * FROM Table2", topOffenders[0].Fingerprint);
            Assert.Equal("SELECT * FROM Table1", topOffenders[1].Fingerprint);
        }

        [Fact]
        public void GetTopOffenders_WithClearTieBreaking_ReturnsConsistentOrder()
        {
            // Arrange - Create a scenario where we have multiple offenders with the same count
            // and verify the ordering is deterministic based on internal dictionary ordering
            var aggregator = new IncidentAggregator();

            // Create incidents with same count
            var callSites = new[] { "ZebraRepository.GetData", "AlphaRepository.GetData", "BetaRepository.GetData", "GammaRepository.GetData" };

            foreach (var callSite in callSites)
            {
                var incident = new NPlusOneIncident
                {
                    SqlQuery = $"SELECT * FROM {callSite.Split('.')[0]}s",
                    Count = 1,
                    Severity = NPlusOneSeverity.Medium,
                    StackTrace = $"at {callSite}()",
                    CallSite = callSite
                };
                aggregator.Add(incident);
            }

            // Act - Get top offenders multiple times
            var offenders1 = aggregator.GetTopOffenders(10);
            var offenders2 = aggregator.GetTopOffenders(10);
            var offenders3 = aggregator.GetTopOffenders(10);

            // Assert - All should return the same order (deterministic)
            Assert.Equal(4, offenders1.Count);
            Assert.Equal(4, offenders2.Count);
            Assert.Equal(4, offenders3.Count);

            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(offenders1[i].Fingerprint, offenders2[i].Fingerprint);
                Assert.Equal(offenders1[i].Fingerprint, offenders3[i].Fingerprint);
            }

            // Assert - Should be ordered deterministically
            Assert.NotEmpty(offenders1[0].Fingerprint);
            Assert.NotEmpty(offenders1[1].Fingerprint);
            Assert.NotEmpty(offenders1[2].Fingerprint);
            Assert.NotEmpty(offenders1[3].Fingerprint);
        }

        [Fact]
        public void TopOffenderRecord_CountPropertyMatchesInput()
        {
            // Arrange
            var fingerprint = "SELECT * FROM Test";
            var count = 42;
            var lastSeen = DateTime.UtcNow.AddMinutes(-5);

            // Act
            var offender = new IncidentAggregator.TopOffender(fingerprint, count, lastSeen);

            // Assert
            Assert.Equal(count, offender.Count);
        }

        [Fact]
        public void GetTopOffenders_WithRequestLargerThanAvailable_ReturnsAllAvailable()
        {
            // Arrange
            var aggregator = new IncidentAggregator();

            // Add only 3 unique fingerprints
            for (int i = 0; i < 3; i++)
            {
                var incident = new NPlusOneIncident
                {
                    SqlQuery = $"SELECT * FROM Table{i}",
                    Count = 1,
                    Severity = NPlusOneSeverity.Medium,
                    StackTrace = $"at TestClass.TestMethod{i}()",
                    CallSite = $"TestClass.TestMethod{i}"
                };
                aggregator.Add(incident);
            }

            // Act - Request more than available
            var topOffenders = aggregator.GetTopOffenders(10);

            // Assert - Should return all 3, not throw or return more
            Assert.Equal(3, topOffenders.Count);
        }

        [Fact]
        public void GetTopOffenders_WithZeroRequested_ReturnsEmptyList()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
            var incident = new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Users",
                Count = 1,
                Severity = NPlusOneSeverity.Medium,
                StackTrace = "at TestClass.TestMethod()",
                CallSite = "TestClass.TestMethod"
            };
            aggregator.Add(incident);

            // Act
            var topOffenders = aggregator.GetTopOffenders(0);

            // Assert
            Assert.Empty(topOffenders);
        }

        [Fact]
        public void GetTopOffenders_WithSingleOffender_ReturnsListWithOneElement()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
            var incident = new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Users",
                Count = 1,
                Severity = NPlusOneSeverity.High,
                StackTrace = "at UserRepository.GetAll()",
                CallSite = "UserRepository.GetAll"
            };
            aggregator.Add(incident);

            // Act
            var topOffenders = aggregator.GetTopOffenders(5);

            // Assert
            Assert.Single(topOffenders);
            Assert.Equal(1, topOffenders[0].Count);
            Assert.Equal("SELECT * FROM Users", topOffenders[0].Fingerprint);
        }
    }
}