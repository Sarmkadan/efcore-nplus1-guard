using System;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests
{
    public class QueryFingerprintValidationTests
    {
        [Fact]
        public void Validate_ValidQueryFingerprint_ReturnsEmptyList()
        {
            // Arrange
            var validFingerprint = QueryFingerprint.Create(
                "SELECT * FROM Users WHERE id = @id",
                "TestClass.TestMethod");

            // Act
            var result = validFingerprint.Validate();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Validate_NullQueryFingerprint_ThrowsArgumentNullException()
        {
            // Arrange
            QueryFingerprint? nullFingerprint = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => nullFingerprint!.Validate());
        }

        [Fact]
        public void Validate_EmptyCommandTextHash_ReturnsError()
        {
            // Arrange
            var fingerprint = CreateTestFingerprint("", "SELECT * FROM Users", "TestClass.TestMethod");

            // Act
            var result = fingerprint.Validate();

            // Assert
            Assert.Contains("CommandTextHash cannot be null or whitespace.", result);
        }

        [Fact]
        public void Validate_WhitespaceCommandTextHash_ReturnsError()
        {
            // Arrange
            var fingerprint = CreateTestFingerprint("   ", "SELECT * FROM Users", "TestClass.TestMethod");

            // Act
            var result = fingerprint.Validate();

            // Assert
            Assert.Contains("CommandTextHash cannot be null or whitespace.", result);
        }

        [Fact]
        public void Validate_InvalidLengthCommandTextHash_ReturnsError()
        {
            // Arrange
            var fingerprint = CreateTestFingerprint("tooshort", "SELECT * FROM Users", "TestClass.TestMethod");

            // Act
            var result = fingerprint.Validate();

            // Assert
            Assert.Contains("CommandTextHash must be a 64-character SHA256 hash.", result);
        }

        [Fact]
        public void Validate_EmptyNormalizedSql_ReturnsError()
        {
            // Arrange
            var fingerprint = CreateTestFingerprint(
                "0000000000000000000000000000000000000000000000000000000000000000",
                "",
                "TestClass.TestMethod");

            // Act
            var result = fingerprint.Validate();

            // Assert
            Assert.Contains("NormalizedSql cannot be null or whitespace.", result);
        }

        [Fact]
        public void Validate_EmptyCallSite_ReturnsError()
        {
            // Arrange
            var fingerprint = CreateTestFingerprint(
                "0000000000000000000000000000000000000000000000000000000000000000",
                "SELECT * FROM Users",
                "");

            // Act
            var result = fingerprint.Validate();

            // Assert
            Assert.Contains("CallSite cannot be null or whitespace.", result);
        }

        [Fact]
        public void IsValid_ValidQueryFingerprint_ReturnsTrue()
        {
            // Arrange
            var validFingerprint = QueryFingerprint.Create(
                "SELECT * FROM Users WHERE id = @id",
                "TestClass.TestMethod");

            // Act
            var result = validFingerprint.IsValid();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValid_NullQueryFingerprint_ThrowsArgumentNullException()
        {
            // Arrange
            QueryFingerprint? nullFingerprint = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => nullFingerprint!.IsValid());
        }

        [Fact]
        public void IsValid_InvalidQueryFingerprint_ReturnsFalse()
        {
            // Arrange
            var fingerprint = CreateTestFingerprint("", "SELECT * FROM Users", "TestClass.TestMethod");

            // Act
            var result = fingerprint.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void EnsureValid_ValidQueryFingerprint_DoesNotThrow()
        {
            // Arrange
            var validFingerprint = QueryFingerprint.Create(
                "SELECT * FROM Users WHERE id = @id",
                "TestClass.TestMethod");

            // Act & Assert
            var exception = Record.Exception(() => validFingerprint.EnsureValid());
            Assert.Null(exception);
        }

        [Fact]
        public void EnsureValid_NullQueryFingerprint_ThrowsArgumentNullException()
        {
            // Arrange
            QueryFingerprint? nullFingerprint = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => nullFingerprint!.EnsureValid());
        }

        [Fact]
        public void EnsureValid_InvalidQueryFingerprint_ThrowsArgumentException()
        {
            // Arrange
            var fingerprint = CreateTestFingerprint("", "SELECT * FROM Users", "TestClass.TestMethod");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => fingerprint.EnsureValid());
            Assert.Contains("QueryFingerprint is invalid:", exception.Message);
        }

        private QueryFingerprint CreateTestFingerprint(string commandTextHash, string normalizedSql, string callSite)
        {
            var constructor = typeof(QueryFingerprint).GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(string), typeof(string) },
                null);

            if (constructor != null)
            {
                return (QueryFingerprint)constructor.Invoke(new object[] { commandTextHash, normalizedSql, callSite });
            }

            return QueryFingerprint.Create("SELECT 1", "Test");
        }
    }
}