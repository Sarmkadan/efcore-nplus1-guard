using System;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests
{
    public class QueryFingerprintValidationResultTests
    {
        [Fact]
        public void Success_CreatesValidResult()
        {
            // Arrange
            var commandTextHash = "ABC123";
            var normalizedSql = "SELECT * FROM Users";
            var callSite = "UserRepository.GetAll";

            // Act
            var result = QueryFingerprintValidationResult.Success(commandTextHash, normalizedSql, callSite);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(commandTextHash, result.CommandTextHash);
            Assert.Equal(normalizedSql, result.NormalizedSql);
            Assert.Equal(callSite, result.CallSite);
            Assert.True(result.IsValid);
            Assert.Empty(result.ValidationErrors);
        }

        [Fact]
        public void Success_WithNullCommandTextHash_ThrowsArgumentNullException()
        {
            // Arrange
            string nullCommandTextHash = null!;
            var normalizedSql = "SELECT * FROM Users";
            var callSite = "UserRepository.GetAll";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                QueryFingerprintValidationResult.Success(nullCommandTextHash, normalizedSql, callSite));
        }

        [Fact]
        public void Success_WithNullNormalizedSql_ThrowsArgumentNullException()
        {
            // Arrange
            var commandTextHash = "ABC123";
            string nullNormalizedSql = null!;
            var callSite = "UserRepository.GetAll";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                QueryFingerprintValidationResult.Success(commandTextHash, nullNormalizedSql, callSite));
        }

        [Fact]
        public void Success_WithNullCallSite_ThrowsArgumentNullException()
        {
            // Arrange
            var commandTextHash = "ABC123";
            var normalizedSql = "SELECT * FROM Users";
            string nullCallSite = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                QueryFingerprintValidationResult.Success(commandTextHash, normalizedSql, nullCallSite));
        }

        [Fact]
        public void Failure_WithErrors_CreatesInvalidResult()
        {
            // Arrange
            var commandTextHash = "ABC123";
            var normalizedSql = "SELECT * FROM Users";
            var callSite = "UserRepository.GetAll";
            var errors = new[] { "Error 1", "Error 2" };

            // Act
            var result = QueryFingerprintValidationResult.Failure(commandTextHash, normalizedSql, callSite, errors);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(commandTextHash, result.CommandTextHash);
            Assert.Equal(normalizedSql, result.NormalizedSql);
            Assert.Equal(callSite, result.CallSite);
            Assert.False(result.IsValid);
            Assert.Equal(2, result.ValidationErrors.Count);
            Assert.Equal(errors[0], result.ValidationErrors[0]);
            Assert.Equal(errors[1], result.ValidationErrors[1]);
        }

        [Fact]
        public void Failure_WithEmptyErrorsArray_CreatesValidResult()
        {
            // Arrange
            var commandTextHash = "ABC123";
            var normalizedSql = "SELECT * FROM Users";
            var callSite = "UserRepository.GetAll";
            var errors = Array.Empty<string>();

            // Act
            var result = QueryFingerprintValidationResult.Failure(commandTextHash, normalizedSql, callSite, errors);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(commandTextHash, result.CommandTextHash);
            Assert.Equal(normalizedSql, result.NormalizedSql);
            Assert.Equal(callSite, result.CallSite);
            Assert.True(result.IsValid);
            Assert.Empty(result.ValidationErrors);
        }

        [Fact]
        public void Failure_WithNullErrors_ThrowsArgumentNullException()
        {
            // Arrange
            var commandTextHash = "ABC123";
            var normalizedSql = "SELECT * FROM Users";
            var callSite = "UserRepository.GetAll";
            string[] nullErrors = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                QueryFingerprintValidationResult.Failure(commandTextHash, normalizedSql, callSite, nullErrors));
        }

        [Fact]
        public void Failure_WithSingleError_CreatesInvalidResult()
        {
            // Arrange
            var commandTextHash = "ABC123";
            var normalizedSql = "SELECT * FROM Users";
            var callSite = "UserRepository.GetAll";
            var error = "Single validation error";

            // Act
            var result = QueryFingerprintValidationResult.Failure(commandTextHash, normalizedSql, callSite, error);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(commandTextHash, result.CommandTextHash);
            Assert.Equal(normalizedSql, result.NormalizedSql);
            Assert.Equal(callSite, result.CallSite);
            Assert.False(result.IsValid);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(error, result.ValidationErrors[0]);
        }

        [Fact]
        public void Constructor_WithAllParameters_CreatesResultWithErrors()
        {
            // Arrange
            var commandTextHash = "ABC123";
            var normalizedSql = "SELECT * FROM Users";
            var callSite = "UserRepository.GetAll";
            var errors = new[] { "Error 1", "Error 2" };

            // Act
            var result = new QueryFingerprintValidationResult(commandTextHash, normalizedSql, callSite, errors);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(commandTextHash, result.CommandTextHash);
            Assert.Equal(normalizedSql, result.NormalizedSql);
            Assert.Equal(callSite, result.CallSite);
            Assert.False(result.IsValid);
            Assert.Equal(2, result.ValidationErrors.Count);
        }

        [Fact]
        public void Constructor_WithNullValidationErrors_ThrowsArgumentNullException()
        {
            // Arrange
            var commandTextHash = "ABC123";
            var normalizedSql = "SELECT * FROM Users";
            var callSite = "UserRepository.GetAll";
            IReadOnlyList<string> nullErrors = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new QueryFingerprintValidationResult(commandTextHash, normalizedSql, callSite, nullErrors));
        }

        [Fact]
        public void IsValid_WhenValidationErrorsIsEmpty_ReturnsTrue()
        {
            // Arrange
            var result = QueryFingerprintValidationResult.Success("ABC123", "SELECT * FROM Users", "UserRepository.GetAll");

            // Act
            var isValid = result.IsValid;

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValid_WhenValidationErrorsIsNotEmpty_ReturnsFalse()
        {
            // Arrange
            var result = QueryFingerprintValidationResult.Failure("ABC123", "SELECT * FROM Users", "UserRepository.GetAll", "Error");

            // Act
            var isValid = result.IsValid;

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidationErrors_ReturnsReadOnlyList()
        {
            // Arrange
            var result = QueryFingerprintValidationResult.Success("ABC123", "SELECT * FROM Users", "UserRepository.GetAll");

            // Act
            var errors = result.ValidationErrors;

            // Assert
            Assert.NotNull(errors);
            Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<string>>(errors);
        }

        [Fact]
        public void CommandTextHash_ReturnsNonNullString()
        {
            // Arrange
            var result = QueryFingerprintValidationResult.Success("ABC123", "SELECT * FROM Users", "UserRepository.GetAll");

            // Assert
            Assert.NotNull(result.CommandTextHash);
            Assert.NotEmpty(result.CommandTextHash);
        }

        [Fact]
        public void NormalizedSql_ReturnsNonNullString()
        {
            // Arrange
            var result = QueryFingerprintValidationResult.Success("ABC123", "SELECT * FROM Users", "UserRepository.GetAll");

            // Assert
            Assert.NotNull(result.NormalizedSql);
            Assert.NotEmpty(result.NormalizedSql);
        }

        [Fact]
        public void CallSite_ReturnsNonNullString()
        {
            // Arrange
            var result = QueryFingerprintValidationResult.Success("ABC123", "SELECT * FROM Users", "UserRepository.GetAll");

            // Assert
            Assert.NotNull(result.CallSite);
            Assert.NotEmpty(result.CallSite);
        }
    }
}
