using System;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests
{
	public class ValidationResultPatternTests
	{
		[Fact]
		public void QueryFingerprintValidationResult_Success_CreatesValidResultWithNoErrors()
		{
			// Act
			var result = QueryFingerprintValidationResult.Success("abc123", "SELECT * FROM Users", "TestClass.TestMethod");

			// Assert
			Assert.True(result.IsValid);
			Assert.Empty(result.ValidationErrors);
			Assert.Equal("abc123", result.CommandTextHash);
			Assert.Equal("SELECT * FROM Users", result.NormalizedSql);
			Assert.Equal("TestClass.TestMethod", result.CallSite);
		}

		[Fact]
		public void QueryFingerprintValidationResult_Failure_CreatesInvalidResultWithErrors()
		{
			// Arrange
			var errors = new[] { "Error 1", "Error 2" };

			// Act
			var result = QueryFingerprintValidationResult.Failure("abc123", "SELECT * FROM Users", "TestClass.TestMethod", errors);

			// Assert
			Assert.False(result.IsValid);
			Assert.Equal(2, result.ValidationErrors.Count);
			Assert.Equal("Error 1", result.ValidationErrors[0]);
			Assert.Equal("Error 2", result.ValidationErrors[1]);
			Assert.Equal("abc123", result.CommandTextHash);
			Assert.Equal("SELECT * FROM Users", result.NormalizedSql);
			Assert.Equal("TestClass.TestMethod", result.CallSite);
		}

		[Fact]
		public void QueryFingerprintValidationResult_IsValid_DerivedFromValidationErrors()
		{
			// Success case
			var successResult = QueryFingerprintValidationResult.Success("hash", "sql", "callsite");
			Assert.True(successResult.IsValid);

			// Failure case
			var failureResult = QueryFingerprintValidationResult.Failure("hash", "sql", "callsite", new[] { "error" });
			Assert.False(failureResult.IsValid);
		}

		[Fact]
		public void QueryFingerprintValidationResult_EmptyErrorsArray_IsValid()
		{
			// Act
			var result = QueryFingerprintValidationResult.Failure("hash", "sql", "callsite", Array.Empty<string>());

			// Assert - empty array should still be valid since IsValid is derived from ValidationErrors.Length == 0
			Assert.True(result.IsValid);
			Assert.Empty(result.ValidationErrors);
		}

		[Fact]
		public void QueryFingerprintValidationResult_ValidationErrorsEmptyArray_IsValid()
		{
			// This test proves that even when created with Failure(), an empty error array results in a valid state
			// This demonstrates that IsValid is properly derived from ValidationErrors.Count == 0

			// Arrange & Act - create a failure result with empty errors
			var result = QueryFingerprintValidationResult.Failure("hash", "sql", "callsite", Array.Empty<string>());

			// Assert - empty error array should result in IsValid = true
			Assert.True(result.IsValid);
			Assert.Empty(result.ValidationErrors);
		}


		[Fact]
		public void QueryFingerprintValidationResult_Immutable_PropertiesHaveNoSetters()
		{
			// This test verifies that the result is immutable by checking that we can't set properties
			// We can't directly test immutability in C# without reflection, but we can verify the behavior
			var result = QueryFingerprintValidationResult.Success("hash", "sql", "callsite");

			// All properties should be get-only (no init or set)
			var properties = typeof(QueryFingerprintValidationResult).GetProperties();
			foreach (var prop in properties)
			{
				Assert.False(prop.CanWrite, $"Property {prop.Name} should be immutable (get-only)");
			}
		}

		[Fact]
		public void QueryStatisticsValidation_Validate_ReturnsQueryFingerprintValidationResult()
		{
			// Arrange
			var stats = new QueryStatistics();

			// Act
			var result = QueryStatisticsValidation.Validate(stats);

			// Assert
			Assert.NotNull(result);
			Assert.IsType<QueryFingerprintValidationResult>(result);
			Assert.True(result.IsValid);
		}

		[Fact]
		public void QueryStatisticsValidation_IsValid_ReturnsTrueForValidStatistics()
		{
			// Arrange
			var stats = new QueryStatistics();

			// Act & Assert
			Assert.True(QueryStatisticsValidation.IsValid(stats));
		}

		[Fact]
		public void NPlusOneGuardOptionsValidation_Validate_ReturnsValidateOptionsResult()
		{
			// Arrange
			var options = new NPlusOneGuardOptions
			{
				Threshold = 10,
				DetectionWindow = TimeSpan.FromSeconds(30),
				LowSeverityThreshold = 5,
				MediumSeverityThreshold = 15,
				IgnoredQueryPatterns = new List<string> { "pattern1", "pattern2" }
			};

			var validator = new NPlusOneGuardOptionsValidation();

			// Act
			var result = validator.Validate("TestOptions", options);

			// Assert
			Assert.NotNull(result);
			Assert.True(result.Succeeded);
		}

		[Fact]
		public void NPlusOneGuardOptionsValidation_ValidateInternal_ReturnsQueryFingerprintValidationResult()
		{
			// Arrange
			var options = new NPlusOneGuardOptions
			{
				Threshold = 10,
				DetectionWindow = TimeSpan.FromSeconds(30),
				LowSeverityThreshold = 5,
				MediumSeverityThreshold = 15,
				IgnoredQueryPatterns = new List<string> { "pattern1", "pattern2" }
			};

			var validator = new NPlusOneGuardOptionsValidation();

			// Act
			var result = validator.ValidateInternal(options);

			// Assert
			Assert.NotNull(result);
			Assert.IsType<QueryFingerprintValidationResult>(result);
			Assert.True(result.IsValid);
		}
	}
}