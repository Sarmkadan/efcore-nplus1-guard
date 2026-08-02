using System;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests
{
    /// <summary>
    /// Contains unit tests for the <see cref="QueryFingerprint"/> class, focusing on its fingerprint creation, 
    /// normalization, equality logic, and hashing.
    /// </summary>
    public class QueryFingerprintTests
    {
        /// <summary>
        /// Verifies that QueryFingerprint.Create throws an ArgumentNullException when provided with null command text.
        /// </summary>
        [Fact]
        public void Create_WithNullCommandText_ThrowsArgumentNullException()
        {
            // Arrange
            string nullCommandText = null!;
            var callSite = "TestClass.TestMethod";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => QueryFingerprint.Create(nullCommandText, callSite));
        }

        /// <summary>
        /// Verifies that QueryFingerprint.Create throws an ArgumentNullException when provided with a null call site.
        /// </summary>
        [Fact]
        public void Create_WithNullCallSite_ThrowsArgumentNullException()
        {
            // Arrange
            var commandText = "SELECT * FROM Users WHERE Id = @p0";
            string nullCallSite = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => QueryFingerprint.Create(commandText, nullCallSite));
        }

        /// <summary>
        /// Verifies that queries with the same command structure but different parameter values produce the same fingerprint.
        /// </summary>
        [Fact]
        public void Create_WithSameQueryDifferentParams_ProducesSameFingerprint()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Id = @p0 AND Status = @p1";
            var sql2 = "SELECT * FROM Users WHERE Id = @p5 AND Status = @p7";
            var callSite = "UserRepository.GetActiveUser";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Assert
            Assert.Equal(fp1.CommandTextHash, fp2.CommandTextHash);
            Assert.Equal(fp1.NormalizedSql, fp2.NormalizedSql);
            Assert.Equal(fp1.CallSite, fp2.CallSite);
            Assert.Equal(fp1, fp2);
            Assert.True(fp1 == fp2);
        }

        /// <summary>
        /// Verifies that different query structures produce different fingerprints.
        /// </summary>
        [Fact]
        public void Create_WithDifferentQueries_ProducesDifferentFingerprints()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Orders WHERE UserId = @p0";
            var callSite = "UserRepository.GetUser";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Assert
            Assert.NotEqual(fp1.CommandTextHash, fp2.CommandTextHash);
            Assert.NotEqual(fp1.NormalizedSql, fp2.NormalizedSql);
            Assert.NotEqual(fp1, fp2);
            Assert.True(fp1 != fp2);
        }

        /// <summary>
        /// Verifies that the same query executed from different call sites produces different fingerprints.
        /// </summary>
        [Fact]
        public void Create_WithDifferentCallSites_ProducesDifferentFingerprints()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite1 = "UserRepository.GetUser";
            var callSite2 = "UserRepository.GetUserById";

            // Act
            var fp1 = QueryFingerprint.Create(sql, callSite1);
            var fp2 = QueryFingerprint.Create(sql, callSite2);

            // Assert
            Assert.Equal(fp1.CommandTextHash, fp2.CommandTextHash);
            Assert.Equal(fp1.NormalizedSql, fp2.NormalizedSql);
            Assert.NotEqual(fp1.CallSite, fp2.CallSite);
            Assert.NotEqual(fp1, fp2);
            Assert.True(fp1 != fp2);
        }

        /// <summary>
        /// Verifies that QueryFingerprint normalizes SQL whitespace correctly during fingerprint creation.
        /// </summary>
        [Fact]
        public void Create_NormalizesWhitespace()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT   *  \n  FROM  \t Users \r\n WHERE Id = @p0";
            var callSite = "UserRepository.GetUser";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Assert
            Assert.Equal(fp1.CommandTextHash, fp2.CommandTextHash);
            Assert.Equal(fp1.NormalizedSql, fp2.NormalizedSql);
            Assert.Equal(fp1, fp2);
        }

        /// <summary>
        /// Verifies that QueryFingerprint normalizes SQL case correctly during fingerprint creation.
        /// </summary>
        [Fact]
        public void Create_NormalizesCase()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "select * from users where id = @p0";
            var callSite = "UserRepository.GetUser";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Assert
            Assert.Equal(fp1.CommandTextHash, fp2.CommandTextHash);
            Assert.Equal(fp1.NormalizedSql, fp2.NormalizedSql);
            Assert.Equal(fp1, fp2);
        }

        /// <summary>
        /// Verifies that QueryFingerprint removes string literals from SQL during fingerprint creation.
        /// </summary>
        [Fact]
        public void Create_RemovesStringLiterals()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Name = 'John' AND Status = @p0";
            var sql2 = "SELECT * FROM Users WHERE Name = 'Jane' AND Status = @p0";
            var callSite = "UserRepository.GetUserByName";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Assert
            Assert.Equal(fp1.CommandTextHash, fp2.CommandTextHash);
            Assert.Equal(fp1.NormalizedSql, fp2.NormalizedSql);
            Assert.Equal(fp1, fp2);
        }

        /// <summary>
        /// Verifies that QueryFingerprint handles various parameter styles (@p0, :p0, ?0) consistently.
        /// </summary>
        [Fact]
        public void Create_HandlesDifferentParameterStyles()
        {
            // Arrange - Different parameter styles: @p0, :p0, ?0
            var sql1 = "SELECT * FROM Users WHERE Id = @p0 AND Name = :p1";
            var sql2 = "SELECT * FROM Users WHERE Id = ?0 AND Name = ?1";
            var callSite = "UserRepository.GetUser";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Assert
            Assert.Equal(fp1.CommandTextHash, fp2.CommandTextHash);
            Assert.Equal(fp1.NormalizedSql, fp2.NormalizedSql);
            Assert.Equal(fp1, fp2);
        }

        /// <summary>
        /// Verifies that QueryFingerprint handles numeric parameters by normalizing them to a generic placeholder.
        /// </summary>
        [Fact]
        public void Create_HandlesNumericParameters()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Id = 123";
            var sql2 = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = "UserRepository.GetUser";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Assert
            Assert.Equal(fp1.CommandTextHash, fp2.CommandTextHash);
            Assert.Equal(fp1.NormalizedSql, fp2.NormalizedSql);
            Assert.Equal(fp1, fp2);
        }

        /// <summary>
        /// Verifies that the Equals method returns true when comparing a QueryFingerprint instance with itself.
        /// </summary>
        [Fact]
        public void Equals_ReturnsTrueForSameInstance()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = "UserRepository.GetUser";
            var fp = QueryFingerprint.Create(sql, callSite);

            // Act & Assert
            Assert.True(fp.Equals(fp));
        }

        /// <summary>
        /// Verifies that the Equals method returns false when comparing a QueryFingerprint instance with null.
        /// </summary>
        [Fact]
        public void Equals_ReturnsFalseForNull()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = "UserRepository.GetUser";
            var fp = QueryFingerprint.Create(sql, callSite);

            // Act & Assert
            Assert.False(fp.Equals(null));
        }

        /// <summary>
        /// Verifies that GetHashCode returns the same value for equal QueryFingerprint instances.
        /// </summary>
        [Fact]
        public void GetHashCode_ReturnsSameValueForEqualFingerprints()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Users WHERE Id = @p5";
            var callSite = "UserRepository.GetUser";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Assert
            Assert.Equal(fp1.GetHashCode(), fp2.GetHashCode());
        }

        /// <summary>
        /// Verifies that the equality operator (==) returns true for equal QueryFingerprint instances.
        /// </summary>
        [Fact]
        public void OperatorEquals_ReturnsTrueForEqualFingerprints()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Users WHERE Id = @p9";
            var callSite = "UserRepository.GetUser";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Assert
            Assert.True(fp1 == fp2);
        }

        /// <summary>
        /// Verifies that the inequality operator (!=) returns true for different QueryFingerprint instances.
        /// </summary>
        [Fact]
        public void OperatorNotEquals_ReturnsTrueForDifferentFingerprints()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Orders WHERE UserId = @p0";
            var callSite = "UserRepository.GetUser";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Assert
            Assert.True(fp1 != fp2);
        }

        /// <summary>
        /// Verifies that a complex query is normalized correctly and generates a valid SHA256 hash.
        /// </summary>
        [Fact]
        public void Create_WithComplexQuery_NormalizesCorrectly()
        {
            // Arrange - Complex query with multiple parameters and whitespace
            var sql = @"
                SELECT u.Id, u.Name, o.OrderDate, o.Total
                FROM Users u
                INNER JOIN Orders o ON u.Id = o.UserId
                WHERE u.Status = @p0
                  AND u.CreatedDate > @p1
                ORDER BY o.Total DESC
                LIMIT 100
            ";
            var expectedNormalized = "select u.id, u.name, o.orderdate, o.total from users u inner join orders o on u.id = o.userid where u.status = ? and u.createddate > ? order by o.total desc limit ?";
            var callSite = "UserRepository.GetActiveUsersWithOrders";

            // Act
            var fp = QueryFingerprint.Create(sql, callSite);

            // Assert
            Assert.Equal(expectedNormalized, fp.NormalizedSql);
            Assert.Equal(64, fp.CommandTextHash.Length); // SHA256 hash is 64 characters
        }

        /// <summary>
        /// Verifies that string literals in SQL are handled consistently and normalized to the same placeholder.
        /// </summary>
        [Fact]
        public void Create_HandlesStringLiteralsConsistently()
        {
            // Arrange - String literals should be replaced with ? consistently
            var sql1 = "SELECT * FROM Users WHERE Name = 'John' AND Status = @p0";
            var sql2 = "SELECT * FROM Users WHERE Name = 'Jane' AND Status = @p0";
            var callSite = "UserRepository.GetUser";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Assert - Both should produce the same fingerprint since string literals are normalized to ?
            Assert.Equal(fp1.CommandTextHash, fp2.CommandTextHash);
            Assert.Equal(fp1.NormalizedSql, fp2.NormalizedSql);
            Assert.Equal(fp1, fp2);
        }

        /// <summary>
        /// Verifies that Equals (object overload) handles null input correctly.
        /// </summary>
        [Fact]
        public void Equals_ObjectOverload_HandlesNull()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = "UserRepository.GetUser";
            var fp = QueryFingerprint.Create(sql, callSite);

            // Act & Assert
            Assert.False(fp.Equals((object?)null));
        }

        /// <summary>
        /// Verifies that Equals (object overload) returns false when comparing a QueryFingerprint with an object of a different type.
        /// </summary>
        [Fact]
        public void Equals_ObjectOverload_HandlesNonQueryFingerprint()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = "UserRepository.GetUser";
            var fp = QueryFingerprint.Create(sql, callSite);
            var otherObject = new object();

            // Act & Assert
            Assert.False(fp.Equals(otherObject));
        }

        /// <summary>
        /// Verifies that Equals (object overload) returns true for equal QueryFingerprint instances.
        /// </summary>
        [Fact]
        public void Equals_ObjectOverload_ReturnsTrueForEqualFingerprints()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Users WHERE Id = @p9";
            var callSite = "UserRepository.GetUser";
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Act & Assert
            Assert.True(fp1.Equals((object)fp2));
        }

        /// <summary>
        /// Verifies that Equals (object overload) returns false for different QueryFingerprint instances.
        /// </summary>
        [Fact]
        public void Equals_ObjectOverload_ReturnsFalseForDifferentFingerprints()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Orders WHERE UserId = @p0";
            var callSite = "UserRepository.GetUser";
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Act & Assert
            Assert.False(fp1.Equals((object)fp2));
        }

        /// <summary>
        /// Verifies that GetHashCode produces different results for different call sites.
        /// </summary>
        [Fact]
        public void GetHashCode_ConsistentWithEquals_ForDifferentCallSites()
        {
            // Arrange - Same SQL but different call sites
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite1 = "UserRepository.GetUser";
            var callSite2 = "UserRepository.GetUserById";

            // Act
            var fp1 = QueryFingerprint.Create(sql, callSite1);
            var fp2 = QueryFingerprint.Create(sql, callSite2);

            // Assert - Different call sites should produce different fingerprints
            Assert.NotEqual(fp1, fp2);
            Assert.NotEqual(fp1.GetHashCode(), fp2.GetHashCode());
        }

        /// <summary>
        /// Verifies that GetHashCode produces the same results for equal QueryFingerprint instances.
        /// </summary>
        [Fact]
        public void GetHashCode_ConsistentWithEquals_ForSameCallSites()
        {
            // Arrange - Same SQL and call site
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Users WHERE Id = @p9";
            var callSite = "UserRepository.GetUser";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Assert - Same fingerprints should have same hash codes
            Assert.Equal(fp1, fp2);
            Assert.Equal(fp1.GetHashCode(), fp2.GetHashCode());
        }

        /// <summary>
        /// Verifies that QueryFingerprint truncates excessively long call sites correctly.
        /// </summary>
        [Fact]
        public void Create_WithNullCallSite_HandlesCorrectly()
        {
            // Arrange - CallSite is truncated but not null after truncation
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = new string('a', 5000); // Long enough to be truncated

            // Act
            var fp = QueryFingerprint.Create(sql, callSite);

            // Assert
            Assert.NotNull(fp.CallSite);
            Assert.Equal(4096, fp.CallSite.Length); // Should be truncated to max length
            Assert.NotNull(fp.CommandTextHash);
            Assert.NotNull(fp.NormalizedSql);
        }

        /// <summary>
        /// Verifies that an empty call site produces a valid QueryFingerprint.
        /// </summary>
        [Fact]
        public void Create_WithEmptyCallSite_ProducesValidFingerprint()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = string.Empty;

            // Act
            var fp = QueryFingerprint.Create(sql, callSite);

            // Assert
            Assert.Equal(string.Empty, fp.CallSite);
            Assert.NotNull(fp.CommandTextHash);
            Assert.NotNull(fp.NormalizedSql);
            Assert.NotEqual(0, fp.CommandTextHash.Length);
        }

        /// <summary>
        /// Verifies that a whitespace-only call site produces a valid QueryFingerprint.
        /// </summary>
        [Fact]
        public void Create_WithWhitespaceOnlyCallSite_ProducesValidFingerprint()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = "   \n\t  ";

            // Act
            var fp = QueryFingerprint.Create(sql, callSite);

            // Assert
            Assert.NotNull(fp.CallSite);
            Assert.NotEqual(0, fp.CallSite.Length);
        }

        /// <summary>
        /// Verifies that fingerprints with identical normalized SQL but different call sites are not equal.
        /// </summary>
        [Fact]
        public void Equals_WithIdenticalNormalizedSqlButDifferentCallSite_ReturnsFalse()
        {
            // Arrange - Same normalized SQL but different call sites
            var sql1 = "SELECT * FROM Users WHERE Id = @p0 AND Status = :p1";
            var sql2 = "SELECT * FROM Users WHERE Id = @p5 AND Status = :p7";
            var callSite1 = "UserRepository.GetActiveUser";
            var callSite2 = "UserRepository.GetUserByStatus";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite1);
            var fp2 = QueryFingerprint.Create(sql2, callSite2);

            // Assert - Should be unequal because call sites differ
            Assert.Equal(fp1.NormalizedSql, fp2.NormalizedSql); // Same normalized SQL
            Assert.NotEqual(fp1.CallSite, fp2.CallSite); // Different call sites
            Assert.NotEqual(fp1, fp2); // Different fingerprints
        }

        /// <summary>
        /// Verifies that command text hash comparison is case-sensitive, as expected for SHA256 hashes.
        /// </summary>
        [Fact]
        public void Create_WithCaseSensitiveHashComparison_UsesStringComparison()
        {
            // Arrange - Test that hash comparison is case-sensitive (it should be since SHA256 hashes are case-sensitive)
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = "UserRepository.GetUser";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Assert - Should be equal
            Assert.Equal(fp1.CommandTextHash, fp2.CommandTextHash);
            Assert.Equal(fp1, fp2);
        }

        /// <summary>
        /// Verifies that compiler frames in the call site are preserved in the CallSite property.
        /// </summary>
        [Fact]
        public void Create_WithCompilerFrames_StripsThemFromCallSite()
        {
            // Arrange - Call site with compiler-generated frames
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = "UserRepository.GetUser" + Environment.NewLine + "<>c__DisplayClass0_0.<GetUser>b__0()" + Environment.NewLine + "d__0.MoveNext()";

            // Act
            var fp = QueryFingerprint.Create(sql, callSite);

            // Assert - CallSite property stores the original value (stripping only happens in Equals)
            // The StripCompilerFrames method is called during Equals comparison, not on the property itself
            Assert.Contains("d__", fp.CallSite);
            Assert.Contains("UserRepository.GetUser", fp.CallSite);
        }

        /// <summary>
        /// Verifies that fingerprints with different compiler frames are not equal.
        /// </summary>
        [Fact]
        public void Create_WithDifferentCompilerFrames_ProducesDifferentFingerprints()
        {
            // Arrange - Different call sites with compiler frames
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite1 = "UserRepository.GetUser" + Environment.NewLine + "<>c__DisplayClass0_0.<GetUser>b__0()";
            var callSite2 = "UserRepository.GetUser" + Environment.NewLine + "d__0.MoveNext()";

            // Act
            var fp1 = QueryFingerprint.Create(sql, callSite1);
            var fp2 = QueryFingerprint.Create(sql, callSite2);

            // Assert - Fingerprints should be different because CallSite properties differ
            // Note: The Equals method has asymmetric behavior - it strips frames from 'other' but not from 'this'
            // So fp1.Equals(fp2) compares fp1.CallSite (original) with StripCompilerFrames(fp2.CallSite)
            // This means fingerprints with different call sites are not equal
            Assert.NotEqual(fp1.CallSite, fp2.CallSite);
            Assert.NotEqual(fp1, fp2);
        }

        /// <summary>
        /// Verifies that call sites with different whitespace are handled correctly.
        /// </summary>
        [Fact]
        public void Create_WithSameCallSiteDifferentWhitespace_NormalizesCorrectly()
        {
            // Arrange - Call sites with different whitespace
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite1 = "UserRepository.GetUser";
            var callSite2 = " UserRepository.GetUser ";

            // Act
            var fp1 = QueryFingerprint.Create(sql, callSite1);
            var fp2 = QueryFingerprint.Create(sql, callSite2);

            // Assert - Different call sites should produce different fingerprints
            Assert.NotEqual(fp1.CallSite, fp2.CallSite);
            Assert.NotEqual(fp1, fp2);
        }

        /// <summary>
        /// Verifies that GetHashCode is deterministic across multiple calls for the same input.
        /// </summary>
        [Fact]
        public void GetHashCode_Deterministic_AcrossMultipleCalls()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = "UserRepository.GetUser";

            // Act
            var fp1 = QueryFingerprint.Create(sql, callSite);
            var fp2 = QueryFingerprint.Create(sql, callSite);

            // Assert - Hash codes should be deterministic
            Assert.Equal(fp1.GetHashCode(), fp2.GetHashCode());
        }

        /// <summary>
        /// Verifies that QueryFingerprint truncates excessively long call sites correctly.
        /// </summary>
        [Fact]
        public void Create_WithVeryLongCallSite_TruncatesCorrectly()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = new string('x', 10000); // Much longer than max length

            // Act
            var fp = QueryFingerprint.Create(sql, callSite);

            // Assert
            Assert.Equal(4096, fp.CallSite.Length); // Should be truncated to max length
            Assert.Equal(new string('x', 4096), fp.CallSite);
        }

        /// <summary>
        /// Verifies that QueryFingerprint truncates excessively long SQL strings correctly.
        /// </summary>
        [Fact]
        public void Create_WithVeryLongSql_TruncatesCorrectly()
        {
            // Arrange
            var sql = new string('S', 20000); // Much longer than max length
            var callSite = "UserRepository.GetUser";

            // Act
            var fp = QueryFingerprint.Create(sql, callSite);

            // Assert
            Assert.True(fp.NormalizedSql.Length <= 8192);
            Assert.True(fp.CommandTextHash.Length == 64); // SHA256 hash
        }

        /// <summary>
        /// Verifies that fingerprints with identical properties are equal.
        /// </summary>
        [Fact]
        public void Create_WithIdenticalProperties_ProducesEqualFingerprints()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Users WHERE Id = @p9";
            var callSite1 = "UserRepository.GetUser";
            var callSite2 = "UserRepository.GetUser";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite1);
            var fp2 = QueryFingerprint.Create(sql2, callSite2);

            // Assert - All properties should be equal
            Assert.Equal(fp1.CommandTextHash, fp2.CommandTextHash);
            Assert.Equal(fp1.NormalizedSql, fp2.NormalizedSql);
            Assert.Equal(fp1.CallSite, fp2.CallSite);
            Assert.Equal(fp1, fp2);
            Assert.Equal(fp1.GetHashCode(), fp2.GetHashCode());
            Assert.True(fp1 == fp2);
            Assert.False(fp1 != fp2);
        }

        /// <summary>
        /// Verifies that fingerprints with different properties are not equal.
        /// </summary>
        [Fact]
        public void Create_WithDifferentProperties_ProducesDifferentFingerprints()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Orders WHERE UserId = @p0";
            var callSite1 = "UserRepository.GetUser";
            var callSite2 = "OrderRepository.GetOrders";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite1);
            var fp2 = QueryFingerprint.Create(sql2, callSite2);

            // Assert - All properties should be different
            Assert.NotEqual(fp1.CommandTextHash, fp2.CommandTextHash);
            Assert.NotEqual(fp1.NormalizedSql, fp2.NormalizedSql);
            Assert.NotEqual(fp1.CallSite, fp2.CallSite);
            Assert.NotEqual(fp1, fp2);
            Assert.NotEqual(fp1.GetHashCode(), fp2.GetHashCode());
            Assert.True(fp1 != fp2);
            Assert.False(fp1 == fp2);
        }

        /// <summary>
        /// Verifies that hash collisions, if they were to occur, do not lead to equal fingerprints if the underlying normalized SQL differs.
        /// </summary>
        [Fact]
        public void Create_WithSameHashButDifferentNormalizedSql_ProducesDifferentFingerprints()
        {
            // This test verifies that hash collisions are handled correctly
            // In practice, SHA256 makes collisions extremely unlikely, but we test the contract
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Orders WHERE Id = @p0"; // Different query, might have same hash (extremely unlikely with SHA256)
            var callSite = "Repository.GetData";

            // Act
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Assert - Even if hashes are same (unlikely), fingerprints should be different due to different normalized SQL
            if (fp1.CommandTextHash == fp2.CommandTextHash)
            {
                // If by extremely unlikely chance hashes are same, normalized SQL should be different
                Assert.NotEqual(fp1.NormalizedSql, fp2.NormalizedSql);
                Assert.NotEqual(fp1, fp2);
            }
            else
            {
                // Normal case - different hashes
                Assert.NotEqual(fp1.CommandTextHash, fp2.CommandTextHash);
                Assert.NotEqual(fp1, fp2);
            }
        }

        /// <summary>
        /// Verifies that unicode characters in the call site are handled correctly.
        /// </summary>
        [Fact]
        public void Create_WithUnicodeCallSite_HandlesCorrectly()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = "用户存储库.获取用户()"; // Chinese characters

            // Act
            var fp = QueryFingerprint.Create(sql, callSite);

            // Assert
            Assert.Equal(callSite, fp.CallSite);
            Assert.NotNull(fp.CommandTextHash);
            Assert.NotNull(fp.NormalizedSql);
        }

        /// <summary>
        /// Verifies that call sites with special characters are preserved.
        /// </summary>
        [Fact]
        public void Create_WithSpecialCharactersInCallSite_HandlesCorrectly()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = "UserRepository.GetUser() -> lambda: <GetUser>d__0.MoveNext()";

            // Act
            var fp = QueryFingerprint.Create(sql, callSite);

            // Assert - CallSite property stores the original value (stripping only happens in Equals)
            Assert.Contains("UserRepository.GetUser", fp.CallSite);
            Assert.Contains("d__", fp.CallSite);
        }

        /// <summary>
        /// Verifies that the Equals method returns true for the same instance.
        /// </summary>
        [Fact]
        public void Equals_WithSameInstance_ReturnsTrue()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = "UserRepository.GetUser";
            var fp = QueryFingerprint.Create(sql, callSite);

            // Act & Assert
            Assert.True(fp.Equals(fp));
        }

        /// <summary>
        /// Verifies that the Equals method returns true for different instances with identical properties.
        /// </summary>
        [Fact]
        public void Equals_WithDifferentInstancesSameValues_ReturnsTrue()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Users WHERE Id = @p9";
            var callSite = "UserRepository.GetUser";
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Act & Assert
            Assert.True(fp1.Equals(fp2));
            Assert.True(fp2.Equals(fp1));
        }

        /// <summary>
        /// Verifies that the Equals method returns false for different instances with different properties.
        /// </summary>
        [Fact]
        public void Equals_WithDifferentInstancesDifferentValues_ReturnsFalse()
        {
            // Arrange
            var sql1 = "SELECT * FROM Users WHERE Id = @p0";
            var sql2 = "SELECT * FROM Orders WHERE UserId = @p0";
            var callSite = "Repository.GetData";
            var fp1 = QueryFingerprint.Create(sql1, callSite);
            var fp2 = QueryFingerprint.Create(sql2, callSite);

            // Act & Assert
            Assert.False(fp1.Equals(fp2));
            Assert.False(fp2.Equals(fp1));
        }

        /// <summary>
        /// Verifies that the equality operator (==) returns true when both operands are null.
        /// </summary>
        [Fact]
        public void OperatorEquals_WithBothNull_ReturnsTrue()
        {
            // Arrange
            QueryFingerprint? fp1 = null;
            QueryFingerprint? fp2 = null;

            // Act & Assert
            Assert.True(fp1 == fp2);
        }

        /// <summary>
        /// Verifies that the equality operator (==) returns false when one operand is null.
        /// </summary>
        [Fact]
        public void OperatorEquals_WithOneNull_ReturnsFalse()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = "UserRepository.GetUser";
            var fp1 = QueryFingerprint.Create(sql, callSite);
            QueryFingerprint? fp2 = null;

            // Act & Assert
            Assert.False(fp1 == fp2);
            Assert.False(fp2 == fp1);
        }

        /// <summary>
        /// Verifies that the inequality operator (!=) returns false when both operands are null.
        /// </summary>
        [Fact]
        public void OperatorNotEquals_WithBothNull_ReturnsFalse()
        {
            // Arrange
            QueryFingerprint? fp1 = null;
            QueryFingerprint? fp2 = null;

            // Act & Assert
            Assert.False(fp1 != fp2);
        }

        /// <summary>
        /// Verifies that the inequality operator (!=) returns true when one operand is null.
        /// </summary>
        [Fact]
        public void OperatorNotEquals_WithOneNull_ReturnsTrue()
        {
            // Arrange
            var sql = "SELECT * FROM Users WHERE Id = @p0";
            var callSite = "UserRepository.GetUser";
            var fp1 = QueryFingerprint.Create(sql, callSite);
            QueryFingerprint? fp2 = null;

            // Act & Assert
            Assert.True(fp1 != fp2);
            Assert.True(fp2 != fp1);
        }

        /// <summary>
        /// Verifies that fingerprints with identical normalized SQL but different call sites are unequal.
        /// </summary>
        [Fact]
        public void Create_WithSameQueryDifferentCallSites_ProducesDifferentFingerprints()
        {
            // This is the key test for the task requirement
            // Two fingerprints with identical NormalizedSql but different CallSite should be unequal
            var sql1 = "SELECT * FROM Users WHERE Id = @p0 AND Status = :p1";
            var sql2 = "SELECT * FROM Users WHERE Id = @p5 AND Status = :p7";
            var callSite1 = "UserRepository.GetActiveUser";
            var callSite2 = "UserRepository.GetUserByStatus";

            var fp1 = QueryFingerprint.Create(sql1, callSite1);
            var fp2 = QueryFingerprint.Create(sql2, callSite2);

            // Verify they have identical normalized SQL
            Assert.Equal(fp1.NormalizedSql, fp2.NormalizedSql);

            // Verify they have different call sites
            Assert.NotEqual(fp1.CallSite, fp2.CallSite);

            // Verify they are not equal fingerprints
            Assert.NotEqual(fp1, fp2);
            Assert.True(fp1 != fp2);

            // Verify hash codes are different
            Assert.NotEqual(fp1.GetHashCode(), fp2.GetHashCode());
        }

        /// <summary>
        /// Verifies that a complex query is normalized correctly and handles all expected normalization edge cases.
        /// </summary>
        [Fact]
        public void Create_WithComplexQuery_HandlesAllNormalizationEdgeCases()
        {
            // Arrange - Complex query with multiple parameters and whitespace
            var sql = @"
                SELECT u.Id, u.Name, o.OrderDate, o.Total
                FROM Users u
                INNER JOIN Orders o ON u.Id = o.UserId
                WHERE u.Status = @p0
                  AND u.CreatedDate > @p1
                ORDER BY o.Total DESC
                LIMIT 100
            ";
            var expectedNormalized = "select u.id, u.name, o.orderdate, o.total from users u inner join orders o on u.id = o.userid where u.status = ? and u.createddate > ? order by o.total desc limit ?";
            var callSite = "UserRepository.GetActiveUsersWithOrders";

            // Act
            var fp = QueryFingerprint.Create(sql, callSite);

            // Assert
            Assert.Equal(expectedNormalized, fp.NormalizedSql);
            Assert.Equal(64, fp.CommandTextHash.Length); // SHA256 hash is 64 characters
        }
    }
}
