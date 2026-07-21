using System;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests
{
    public class QueryFingerprintTests
    {
        [Fact]
        public void Create_WithNullCommandText_ThrowsArgumentNullException()
        {
            // Arrange
            string nullCommandText = null!;
            var callSite = "TestClass.TestMethod";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => QueryFingerprint.Create(nullCommandText, callSite));
        }

        [Fact]
        public void Create_WithNullCallSite_ThrowsArgumentNullException()
        {
            // Arrange
            var commandText = "SELECT * FROM Users WHERE Id = @p0";
            string nullCallSite = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => QueryFingerprint.Create(commandText, nullCallSite));
        }

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
    }
}
