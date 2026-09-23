#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using EfCoreNPlusOneGuard;

namespace EfCoreNPlusOneGuard.Tests
{
    public class QueryStatisticsValidationTests
    {
        [Fact]
        public void Validate_EmptyStatistics_ReturnsValid()
        {
            var stats = new QueryStatistics();
            var result = stats.Validate();
            Assert.True(result.IsValid);
            Assert.Empty(result.ValidationErrors);
        }

        [Fact]
        public void Validate_EntryWithNegativeCount_IsInvalid()
        {
            var entry = new QueryStatistics.QueryStatEntry("SELECT 1", -1, TimeSpan.FromSeconds(1));
            var result = entry.Validate();
            Assert.False(result.IsValid);
            Assert.Contains("Count cannot be negative.", result.ValidationErrors);
        }

        [Fact]
        public void Validate_EntryWithZeroCount_IsValid()
        {
            var entry = new QueryStatistics.QueryStatEntry("SELECT 1", 0, TimeSpan.Zero);
            var result = entry.Validate();
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_EntryWithNullSql_IsInvalid()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new QueryStatistics.QueryStatEntry(null!, 1, TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public void Validate_EntryWithEmptySql_IsInvalid()
        {
            var entry = new QueryStatistics.QueryStatEntry(string.Empty, 1, TimeSpan.FromSeconds(1));
            var result = entry.Validate();
            Assert.False(result.IsValid);
            Assert.Contains("Sql cannot be null or whitespace.", result.ValidationErrors);
        }

        [Fact]
        public void Validate_EntryWithWhitespaceSql_IsInvalid()
        {
            var entry = new QueryStatistics.QueryStatEntry("   ", 1, TimeSpan.FromSeconds(1));
            var result = entry.Validate();
            Assert.False(result.IsValid);
            Assert.Contains("Sql cannot be null or whitespace.", result.ValidationErrors);
        }

        [Fact]
        public void Validate_DuplicateEntriesAreNotPossible_InQueryStatistics()
        {
            var stats = new QueryStatistics();
            stats.Record("SELECT 1", TimeSpan.FromSeconds(1));
            stats.Record("SELECT 1", TimeSpan.FromSeconds(1));

            var entries = stats.TopByCount(int.MaxValue);

            Assert.Single(entries);
            Assert.Equal(2, entries[0].Count);
        }
    }
}