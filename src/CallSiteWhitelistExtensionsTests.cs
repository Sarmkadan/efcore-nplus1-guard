// tests/CallSiteWhitelistExtensionsTests.cs
using System;
using System.Collections.Generic;
using EfCoreNPlusOneGuard;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests
{
    public class CallSiteWhitelistExtensionsTests
    {
        [Fact]
        public void AddRange_HappyPath_AddsAllEntries()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();
            var entries = new List<(string TypeName, string? MethodName)>
            {
                ("Namespace.TypeA", null),
                ("Namespace.TypeB", "MethodB")
            };

            // Act
            whitelist.AddRange(entries);

            // Assert
            Assert.Equal(entries.Count, whitelist.GetEntryCount());
        }

        [Fact]
        public void AddRange_NullWhitelist_ThrowsArgumentNullException()
        {
            // Arrange
            var entries = new List<(string TypeName, string? MethodName)>
            {
                ("Namespace.TypeA", null)
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CallSiteWhitelistExtensions.AddRange(null!, entries));
        }

        [Fact]
        public void AddRange_NullEntries_ThrowsArgumentNullException()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => whitelist.AddRange(null!));
        }

        [Fact]
        public void AddRange_EmptyCollection_NoChange()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();
            var initialCount = whitelist.GetEntryCount();

            // Act
            whitelist.AddRange(Array.Empty<(string, string?)>());

            // Assert
            Assert.Equal(initialCount, whitelist.GetEntryCount());
        }

        [Fact]
        public void AddPatterns_HappyPath_AddsAllPatterns()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();
            var patterns = new[] { "PatternA*", "PatternB*" };

            // Act
            whitelist.AddPatterns(patterns);

            // Assert
            Assert.Equal(patterns.Length, whitelist.GetEntryCount());
        }

        [Fact]
        public void AddPatterns_NullWhitelist_ThrowsArgumentNullException()
        {
            // Arrange
            var patterns = new[] { "PatternA*" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CallSiteWhitelistExtensions.AddPatterns(null!, patterns));
        }

        [Fact]
        public void AddPatterns_NullPatterns_ThrowsArgumentNullException()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => whitelist.AddPatterns(null!));
        }

        [Fact]
        public void AddPatterns_EmptyCollection_NoChange()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();
            var initialCount = whitelist.GetEntryCount();

            // Act
            whitelist.AddPatterns(Array.Empty<string>());

            // Assert
            Assert.Equal(initialCount, whitelist.GetEntryCount());
        }

        [Fact]
        public void GetEntryCount_ReturnsCurrentCount()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();
            whitelist.Add("TypeA");
            whitelist.AddPattern("Pattern*");

            // Act
            var count = whitelist.GetEntryCount();

            // Assert
            Assert.Equal(2, count);
        }

        [Fact]
        public void IsEmpty_WhenEmpty_ReturnsTrue()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();

            // Act
            var empty = whitelist.IsEmpty();

            // Assert
            Assert.True(empty);
        }

        [Fact]
        public void IsEmpty_WhenNotEmpty_ReturnsFalse()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();
            whitelist.Add("TypeA");

            // Act
            var empty = whitelist.IsEmpty();

            // Assert
            Assert.False(empty);
        }

        [Fact]
        public void ClearAll_WhenNotEmpty_RemovesAllEntries_ReturnsRemovedCount()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();
            whitelist.Add("TypeA");
            whitelist.AddPattern("Pattern*");
            var beforeCount = whitelist.GetEntryCount();

            // Act
            var removed = whitelist.ClearAll();

            // Assert
            Assert.Equal(beforeCount, removed);
            Assert.True(whitelist.IsEmpty());
        }

        [Fact]
        public void ClearAll_WhenEmpty_ReturnsZero()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();

            // Act
            var removed = whitelist.ClearAll();

            // Assert
            Assert.Equal(0, removed);
            Assert.True(whitelist.IsEmpty());
        }
    }
}
