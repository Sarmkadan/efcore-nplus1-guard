using System;
using System.Collections.Generic;
using EfCoreNPlusOneGuard;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests
{
    public class QueryStatisticsTests
    {
        [Fact]
        public void Record_NullSql_ThrowsArgumentNullException()
        {
            var stats = new QueryStatistics();

            Assert.Throws<ArgumentNullException>(() => stats.Record(null!, TimeSpan.FromMilliseconds(10)));
        }

        [Fact]
        public void Record_NegativeDuration_ThrowsArgumentOutOfRangeException()
        {
            var stats = new QueryStatistics();

            Assert.Throws<ArgumentOutOfRangeException>(() => stats.Record("SELECT 1", TimeSpan.FromMilliseconds(-1)));
        }

        [Fact]
        public void Record_ValidInput_UpdatesTotals()
        {
            var stats = new QueryStatistics();

            stats.Record("SELECT * FROM Users", TimeSpan.FromMilliseconds(100));
            stats.Record("SELECT * FROM Users", TimeSpan.FromMilliseconds(200));
            stats.Record("SELECT * FROM Orders", TimeSpan.FromMilliseconds(150));

            // Total queries should be 3
            Assert.Equal(3, stats.TotalQueries);
            // Unique queries should be 2 (after normalization)
            Assert.Equal(2, stats.UniqueQueries);
            // Total duration should be sum of all durations
            var expectedTotal = TimeSpan.FromMilliseconds(100 + 200 + 150);
            Assert.Equal(expectedTotal, stats.TotalDuration);
        }

        [Fact]
        public void TopByCount_ReturnsCorrectOrderingAndCounts()
        {
            var stats = new QueryStatistics();

            // Add multiple occurrences for two queries
            for (int i = 0; i < 5; i++)
                stats.Record("SELECT * FROM A", TimeSpan.FromMilliseconds(10));

            for (int i = 0; i < 3; i++)
                stats.Record("SELECT * FROM B", TimeSpan.FromMilliseconds(20));

            var top = stats.TopByCount(2);

            Assert.Equal(2, top.Count);
            Assert.Equal("SELECT * FROM A", top[0].Sql);
            Assert.Equal(5, top[0].Count);
            Assert.Equal(TimeSpan.FromMilliseconds(50), top[0].TotalDuration);

            Assert.Equal("SELECT * FROM B", top[1].Sql);
            Assert.Equal(3, top[1].Count);
            Assert.Equal(TimeSpan.FromMilliseconds(60), top[1].TotalDuration);
        }

        [Fact]
        public void TopByCount_NegativeN_ThrowsArgumentOutOfRangeException()
        {
            var stats = new QueryStatistics();

            Assert.Throws<ArgumentOutOfRangeException>(() => stats.TopByCount(-1));
        }

        [Fact]
        public void Reset_ClearsAllData()
        {
            var stats = new QueryStatistics();

            stats.Record("SELECT 1", TimeSpan.FromMilliseconds(5));
            stats.Record("SELECT 2", TimeSpan.FromMilliseconds(10));

            Assert.NotEqual(0, stats.TotalQueries);
            Assert.NotEqual(0, stats.UniqueQueries);

            stats.Reset();

            Assert.Equal(0, stats.TotalQueries);
            Assert.Equal(0, stats.UniqueQueries);
            Assert.Empty(stats.TopByCount(10));
        }

        [Fact]
        public void QueryStatEntry_Properties_WorkAsExpected()
        {
            var entry = new QueryStatistics.QueryStatEntry(
                sql: "SELECT * FROM Test",
                count: 4,
                totalDuration: TimeSpan.FromMilliseconds(40));

            // QueryStatEntry stores the raw SQL as provided; it does not normalize.
            Assert.Equal("SELECT * FROM Test", entry.Sql);
            Assert.Equal(4, entry.Count);
            Assert.Equal(TimeSpan.FromMilliseconds(40), entry.TotalDuration);
            Assert.Equal(TimeSpan.FromMilliseconds(10), entry.AvgDuration);
        }
    }
}
