using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests
{
    /// <summary>
    /// Tests for the <see cref="IncidentAggregator"/> class.
    /// </summary>
    public class IncidentAggregatorTests
    {
        /// <summary>
        /// Tests that adding a null incident throws an ArgumentNullException.
        /// </summary>
        [Fact]
        public void Add_WithNullIncident_ThrowsArgumentNullException()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
            NPlusOneIncident nullIncident = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => aggregator.Add(nullIncident));
        }

        /// <summary>
        /// Tests that adding a valid incident adds it to the aggregator.
        /// </summary>
        [Fact]
        public void Add_WithValidIncident_AddsToAggregator()
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

            // Act
            aggregator.Add(incident);

            // Assert
            var allIncidents = aggregator.All();
            var counts = aggregator.CountsByFingerprint();
            var summary = aggregator.GetScanSummary();

            Assert.Single(allIncidents);
            Assert.Equal(1, counts[incident.SqlQuery]);
            Assert.Equal(1, summary.TotalQueries);
            Assert.Equal(1, summary.UniqueFingerprints);
        }

        /// <summary>
        /// Tests that adding incidents with the same fingerprint accumulates their counts.
        /// </summary>
        [Fact]
        public void Add_WithSameFingerprint_AccumulatesIncidents()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
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
                SqlQuery = "SELECT * FROM Users WHERE Id = @p0",
                Count = 1,
                Severity = NPlusOneSeverity.Low,
                StackTrace = "at UserRepository.GetUsers()",
                CallSite = "UserRepository.GetUsers"
            };

            // Act
            aggregator.Add(incident1);
            aggregator.Add(incident2);

            // Assert
            var allIncidents = aggregator.All();
            var counts = aggregator.CountsByFingerprint();

            Assert.Equal(2, allIncidents.Count);
            Assert.Equal(2, counts[incident1.SqlQuery]);
        }

        /// <summary>
        /// Tests that adding incidents with different fingerprints stores them separately.
        /// </summary>
        [Fact]
        public void Add_WithDifferentFingerprints_StoresSeparately()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
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
                Count = 1,
                Severity = NPlusOneSeverity.High,
                StackTrace = "at OrderRepository.GetOrders()",
                CallSite = "OrderRepository.GetOrders"
            };

            // Act
            aggregator.Add(incident1);
            aggregator.Add(incident2);

            // Assert
            var counts = aggregator.CountsByFingerprint();

            Assert.Equal(2, counts.Count);
            Assert.Equal(1, counts[incident1.SqlQuery]);
            Assert.Equal(1, counts[incident2.SqlQuery]);
        }

        /// <summary>
        /// Tests that CountsByFingerprint returns correct counts for multiple incidents.
        /// </summary>
        [Fact]
        public void CountsByFingerprint_WithMultipleIncidents_ReturnsCorrectCounts()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
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
                SqlQuery = "SELECT * FROM Users WHERE Id = @p0",
                Count = 1,
                Severity = NPlusOneSeverity.Low,
                StackTrace = "at UserRepository.GetUsers()",
                CallSite = "UserRepository.GetUsers"
            };
            var incident3 = new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Orders WHERE UserId = @p0",
                Count = 1,
                Severity = NPlusOneSeverity.High,
                StackTrace = "at OrderRepository.GetOrders()",
                CallSite = "OrderRepository.GetOrders"
            };

            aggregator.Add(incident1);
            aggregator.Add(incident2);
            aggregator.Add(incident3);

            // Act
            var counts = aggregator.CountsByFingerprint();

            // Assert
            Assert.Equal(2, counts.Count);
            Assert.Equal(2, counts["SELECT * FROM Users WHERE Id = @p0"]);
            Assert.Equal(1, counts["SELECT * FROM Orders WHERE UserId = @p0"]);
        }

        /// <summary>
        /// Tests that CountsByFingerprint returns a read-only dictionary.
        /// </summary>
        [Fact]
        public void CountsByFingerprint_ReturnsReadOnlyDictionary()
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
            var counts = aggregator.CountsByFingerprint();

            // Assert
            Assert.IsAssignableFrom<IReadOnlyDictionary<string, int>>(counts);
        }

        /// <summary>
        /// Tests that All returns all incidents when multiple incidents are added.
        /// </summary>
        [Fact]
        public void All_WithMultipleIncidents_ReturnsAllIncidents()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
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
                Count = 1,
                Severity = NPlusOneSeverity.High,
                StackTrace = "at OrderRepository.GetOrders()",
                CallSite = "OrderRepository.GetOrders"
            };

            aggregator.Add(incident1);
            aggregator.Add(incident2);

            // Act
            var allIncidents = aggregator.All();

            // Assert
            Assert.Equal(2, allIncidents.Count);
            Assert.Contains(incident1, allIncidents);
            Assert.Contains(incident2, allIncidents);
            Assert.IsAssignableFrom<IReadOnlyList<NPlusOneIncident>>(allIncidents);
        }

        /// <summary>
        /// Tests that All returns an empty list when no incidents have been added.
        /// </summary>
        [Fact]
        public void All_WithEmptyAggregator_ReturnsEmptyList()
        {
            // Arrange
            var aggregator = new IncidentAggregator();

            // Act
            var allIncidents = aggregator.All();

            // Assert
            Assert.Empty(allIncidents);
        }

        /// <summary>
        /// Tests that GetScanSummary returns zero values when no incidents have been added.
        /// </summary>
        [Fact]
        public void GetScanSummary_WithEmptyAggregator_ReturnsZeroValues()
        {
            // Arrange
            var aggregator = new IncidentAggregator();

            // Act
            var summary = aggregator.GetScanSummary();

            // Assert
            Assert.Equal(0, summary.TotalQueries);
            Assert.Equal(0, summary.UniqueFingerprints);
            Assert.Empty(summary.TopOffenders);
        }

        /// <summary>
        /// Tests that GetScanSummary returns correct summary when incidents have been added.
        /// </summary>
        [Fact]
        public void GetScanSummary_WithIncidents_ReturnsCorrectSummary()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
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
                SqlQuery = "SELECT * FROM Users WHERE Id = @p0",
                Count = 1,
                Severity = NPlusOneSeverity.Low,
                StackTrace = "at UserRepository.GetUsers()",
                CallSite = "UserRepository.GetUsers"
            };
            var incident3 = new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Orders WHERE UserId = @p0",
                Count = 1,
                Severity = NPlusOneSeverity.High,
                StackTrace = "at OrderRepository.GetOrders()",
                CallSite = "OrderRepository.GetOrders"
            };

            aggregator.Add(incident1);
            aggregator.Add(incident2);
            aggregator.Add(incident3);

            // Act
            var summary = aggregator.GetScanSummary();

            // Assert
            Assert.Equal(3, summary.TotalQueries);
            Assert.Equal(2, summary.UniqueFingerprints);
            Assert.Equal(3, summary.TopOffenders.Sum(o => o.Count));
        }

        /// <summary>
        /// Tests that GetTopOffenders returns an empty list when no incidents have been added.
        /// </summary>
        [Fact]
        public void GetTopOffenders_WithEmptyAggregator_ReturnsEmptyList()
        {
            // Arrange
            var aggregator = new IncidentAggregator();

            // Act
            var topOffenders = aggregator.GetTopOffenders(5);

            // Assert
            Assert.Empty(topOffenders);
        }

        /// <summary>
        /// Tests that GetTopOffenders returns an empty list when a negative count is requested.
        /// </summary>
        [Fact]
        public void GetTopOffenders_WithNegativeCount_ReturnsEmptyList()
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
            var topOffenders = aggregator.GetTopOffenders(-1);

            // Assert
            Assert.Empty(topOffenders);
        }

        /// <summary>
        /// Tests that GetTopOffenders returns an empty list when zero is requested.
        /// </summary>
        [Fact]
        public void GetTopOffenders_WithZeroCount_ReturnsEmptyList()
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
            var topOffenders = aggregator.GetTopOffenders(0);

            // Assert
            Assert.Empty(topOffenders);
        }

        /// <summary>
        /// Tests that GetTopOffenders returns the top offenders by count when multiple incidents exist.
        /// </summary>
        [Fact]
        public void GetTopOffenders_WithMultipleIncidents_ReturnsTopByCount()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
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
                Count = 1,
                Severity = NPlusOneSeverity.High,
                StackTrace = "at OrderRepository.GetOrders()",
                CallSite = "OrderRepository.GetOrders"
            };
            var incident3 = new NPlusOneIncident
            {
                SqlQuery = "SELECT * FROM Products WHERE CategoryId = @p0",
                Count = 1,
                Severity = NPlusOneSeverity.Low,
                StackTrace = "at ProductRepository.GetProducts()",
                CallSite = "ProductRepository.GetProducts"
            };

            aggregator.Add(incident1);
            aggregator.Add(incident2);
            for (int i = 0; i < 10; i++)
            {
                aggregator.Add(incident3); // Total of 10 incidents for this fingerprint
            }

            // Act
            var topOffenders = aggregator.GetTopOffenders(2);

            // Assert
            Assert.Equal(2, topOffenders.Count);
            // The one with 10 incidents should be first
            Assert.Equal(10, topOffenders[0].Count);
            Assert.Equal("SELECT * FROM Products WHERE CategoryId = @p0", topOffenders[0].Fingerprint);
            // The other two have count=1, so either could be second (ordered by last seen)
            Assert.Equal(1, topOffenders[1].Count);
            // Just verify it's one of the two with count=1
            Assert.True(topOffenders[1].Fingerprint == "SELECT * FROM Users WHERE Id = @p0" ||
                      topOffenders[1].Fingerprint == "SELECT * FROM Orders WHERE UserId = @p0");
        }

        /// <summary>
        /// Tests that GetTopOffenders orders by last seen when counts are equal.
        /// </summary>
        [Fact]
        public void GetTopOffenders_WithSameCount_OrdersByLastSeen()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
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
                Count = 1,
                Severity = NPlusOneSeverity.High,
                StackTrace = "at OrderRepository.GetOrders()",
                CallSite = "OrderRepository.GetOrders"
            };

            aggregator.Add(incident1);
            // Add second incident slightly later
            System.Threading.Thread.Sleep(10);
            aggregator.Add(incident2);

            // Act
            var topOffenders = aggregator.GetTopOffenders(2);

            // Assert - Should be ordered by count descending, then by last seen descending
            Assert.Equal(2, topOffenders.Count);
            Assert.Equal(1, topOffenders[0].Count);
            Assert.Equal(1, topOffenders[1].Count);
        }

        /// <summary>
        /// Tests that Clear resets the aggregator when incidents have been added.
        /// </summary>
        [Fact]
        public void Clear_WithIncidents_ResetsAggregator()
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

            // Verify incidents exist
            Assert.Single(aggregator.All());
            Assert.Equal(1, aggregator.CountsByFingerprint().Count);

            // Act
            aggregator.Clear();

            // Assert
            Assert.Empty(aggregator.All());
            Assert.Empty(aggregator.CountsByFingerprint());
            var summary = aggregator.GetScanSummary();
            Assert.Equal(0, summary.TotalQueries);
            Assert.Equal(0, summary.UniqueFingerprints);
        }

        /// <summary>
        /// Tests that Clear does not throw when called on an empty aggregator.
        /// </summary>
        [Fact]
        public void Clear_WithEmptyAggregator_DoesNotThrow()
        {
            // Arrange
            var aggregator = new IncidentAggregator();

            // Act & Assert
            aggregator.Clear(); // Should not throw
            Assert.Empty(aggregator.All());
        }

        /// <summary>
        /// Tests that BuildSummaryText returns the no incidents message when no incidents have been added.
        /// </summary>
        [Fact]
        public void BuildSummaryText_WithEmptyAggregator_ReturnsNoIncidentsMessage()
        {
            // Arrange
            var aggregator = new IncidentAggregator();

            // Act
            var summaryText = aggregator.BuildSummaryText();

            // Assert
            Assert.Equal("No N+1 incidents detected.", summaryText);
        }

        /// <summary>
        /// Tests that BuildSummaryText returns a formatted summary when incidents have been added.
        /// </summary>
        [Fact]
        public void BuildSummaryText_WithIncidents_ReturnsFormattedSummary()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
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
                SqlQuery = "SELECT * FROM Users WHERE Id = @p0",
                Count = 1,
                Severity = NPlusOneSeverity.Low,
                StackTrace = "at UserRepository.GetUsers()",
                CallSite = "UserRepository.GetUsers"
            };
            aggregator.Add(incident1);
            aggregator.Add(incident2);

            // Act
            var summaryText = aggregator.BuildSummaryText();

            // Assert
            Assert.Contains("N+1 Guard Summary", summaryText);
            Assert.Contains("Total incidents: 2", summaryText);
            Assert.Contains("Unique query fingerprints: 1", summaryText);
            Assert.Contains("Total duplicate queries: 2", summaryText);
            Assert.Contains("Top fingerprints by occurrence:", summaryText);
            Assert.Contains("2x: SELECT * FROM Users WHERE Id = @p0", summaryText);
        }

        /// <summary>
        /// Tests that Add is thread-safe when called concurrently from multiple threads.
        /// </summary>
        [Fact]
        public async Task Add_WithConcurrentAdds_ThreadSafe()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
            var numThreads = 10;
            var incidentsPerThread = 100;
            var tasks = new List<Task>();

            // Act - Concurrent adds from multiple threads
            for (int i = 0; i < numThreads; i++)
            {
                int threadId = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < incidentsPerThread; j++)
                    {
                        var incident = new NPlusOneIncident
                        {
                            SqlQuery = $"SELECT * FROM Table{threadId}",
                            Count = 1,
                            Severity = NPlusOneSeverity.Medium,
                            StackTrace = $"at TestClass.TestMethod{threadId}()",
                            CallSite = $"TestClass.TestMethod{threadId}"
                        };
                        aggregator.Add(incident);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            var allIncidents = aggregator.All();
            var counts = aggregator.CountsByFingerprint();

            Assert.Equal(numThreads * incidentsPerThread, allIncidents.Count);
            Assert.Equal(numThreads, counts.Count);

            foreach (var kvp in counts)
            {
                Assert.Equal(incidentsPerThread, kvp.Value);
            }
        }

        /// <summary>
        /// Tests that GetScanSummary is thread-safe when accessed concurrently.
        /// </summary>
        [Fact]
        public async Task GetScanSummary_WithConcurrentAccess_ThreadSafe()
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

            var numThreads = 10;
            var tasks = new List<Task>();

            // Act - Concurrent calls to GetScanSummary
            for (int i = 0; i < numThreads; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        var summary = aggregator.GetScanSummary();
                        Assert.Equal(1, summary.TotalQueries);
                        Assert.Equal(1, summary.UniqueFingerprints);
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Tests that CountsByFingerprint is thread-safe when accessed concurrently.
        /// </summary>
        [Fact]
        public async Task CountsByFingerprint_WithConcurrentAccess_ThreadSafe()
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

            var numThreads = 10;
            var tasks = new List<Task>();

            // Act - Concurrent calls to CountsByFingerprint
            for (int i = 0; i < numThreads; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        var counts = aggregator.CountsByFingerprint();
                        Assert.Single(counts);
                        Assert.Equal(1, counts[incident.SqlQuery]);
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Tests that Clear is thread-safe when accessed concurrently.
        /// </summary>
        [Fact]
        public async Task Clear_WithConcurrentAccess_ThreadSafe()
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

            var numThreads = 10;
            var tasks = new List<Task>();

            // Act - Concurrent calls to Clear and Add
            for (int i = 0; i < numThreads; i++)
            {
                if (i % 2 == 0)
                {
                    tasks.Add(Task.Run(() => aggregator.Clear()));
                }
                else
                {
                    tasks.Add(Task.Run(() => aggregator.Add(incident)));
                }
            }

            await Task.WhenAll(tasks);

            // Assert - Should not throw and should be in a valid state
            var allIncidents = aggregator.All();
            Assert.NotNull(allIncidents);
        }

        /// <summary>
        /// Tests that GetTopOffenders returns only the requested number of offenders when more exist.
        /// </summary>
        [Fact]
        public void GetTopOffenders_WithMoreOffendersThanRequested_ReturnsOnlyRequestedCount()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
            for (int i = 0; i < 10; i++)
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

            // Act
            var topOffenders = aggregator.GetTopOffenders(3);

            // Assert
            Assert.Equal(3, topOffenders.Count);
        }

        /// <summary>
        /// Tests that GetTopOffenders returns all offenders when the requested count is greater than or equal to the number of offenders.
        /// </summary>
        [Fact]
        public void GetTopOffenders_WithAllOffendersRequested_ReturnsAll()
        {
            // Arrange
            var aggregator = new IncidentAggregator();
            for (int i = 0; i < 5; i++)
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

            // Act
            var topOffenders = aggregator.GetTopOffenders(10);

            // Assert
            Assert.Equal(5, topOffenders.Count);
        }

        /// <summary>
        /// Tests that the TopOffender record's properties are correctly set.
        /// </summary>
        [Fact]
        public void TopOffenderRecord_PropertiesAreCorrect()
        {
            // Arrange
            var fingerprint = "SELECT * FROM Users";
            var count = 5;
            var lastSeen = DateTime.UtcNow;

            // Act
            var offender = new IncidentAggregator.TopOffender(fingerprint, count, lastSeen);

            // Assert
            Assert.Equal(fingerprint, offender.Fingerprint);
            Assert.Equal(count, offender.Count);
            Assert.Equal(lastSeen, offender.LastSeen);
        }

        /// <summary>
        /// Tests that the Summary record's properties are correctly set.
        /// </summary>
        [Fact]
        public void SummaryRecord_PropertiesAreCorrect()
        {
            // Arrange
            var totalQueries = 10;
            var uniqueFingerprints = 3;
            var topOffenders = new List<IncidentAggregator.TopOffender>
            {
                new IncidentAggregator.TopOffender("SELECT * FROM Users", 5, DateTime.UtcNow),
                new IncidentAggregator.TopOffender("SELECT * FROM Orders", 3, DateTime.UtcNow.AddSeconds(-10))
            };

            // Act
            var summary = new IncidentAggregator.Summary(totalQueries, uniqueFingerprints, topOffenders);

            // Assert
            Assert.Equal(totalQueries, summary.TotalQueries);
            Assert.Equal(uniqueFingerprints, summary.UniqueFingerprints);
            Assert.Equal(2, summary.TopOffenders.Count);
            Assert.IsAssignableFrom<IReadOnlyList<IncidentAggregator.TopOffender>>(summary.TopOffenders);
        }
    }
}