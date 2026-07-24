// Copyright (c) 2023-present EF Core N+1 Guard contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Extensions.Options;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests
{
    /// <summary>
    /// Tests for <see cref="NPlusOneGuardOptionsValidation"/> class.
    /// </summary>
    public class NPlusOneGuardOptionsValidationTests
    {
        private readonly NPlusOneGuardOptionsValidation _validator = new();

        /// <summary>
        /// Creates a valid options instance for testing.
        /// </summary>
        /// <returns>A valid <see cref="NPlusOneGuardOptions"/> instance.</returns>
        private static NPlusOneGuardOptions CreateValidOptions()
        {
            return new NPlusOneGuardOptions
            {
                Threshold = 5,
                DetectionWindow = TimeSpan.FromSeconds(2),
                LowSeverityThreshold = 10,
                MediumSeverityThreshold = 50,
                IgnoredQueryPatterns = new System.Collections.Generic.List<string> { "pattern1", "pattern2" }
            };
        }

        /// <summary>
        /// Tests that valid options produce a success result.
        /// </summary>
        [Fact]
        public void Validate_ValidOptions_ReturnsSuccess()
        {
            // Arrange
            var options = CreateValidOptions();

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.Equal(ValidateOptionsResult.Success, result);
        }

        /// <summary>
        /// Tests that valid options produce a successful internal validation result.
        /// </summary>
        [Fact]
        public void ValidateInternal_ValidOptions_ReturnsSuccess()
        {
            // Arrange
            var options = CreateValidOptions();

            // Act
            var result = _validator.ValidateInternal(options);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.ValidationErrors);
        }

        /// <summary>
        /// Tests that null options throw an exception.
        /// </summary>
        [Fact]
        public void Validate_NullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _validator.Validate(null, null!));
        }

        /// <summary>
        /// Tests that null options throw an exception in ValidateInternal.
        /// </summary>
        [Fact]
        public void ValidateInternal_NullOptions_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _validator.ValidateInternal(null!));
        }

        /// <summary>
        /// Tests that Threshold must be greater than 0.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Validate_ThresholdLessThanOrEqualToZero_ReturnsFailure(int threshold)
        {
            // Arrange
            var options = CreateValidOptions();
            options.Threshold = threshold;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("Threshold must be greater than 0.", result.Failures);
        }

        /// <summary>
        /// Tests that Threshold == 1 is valid (boundary value).
        /// </summary>
        [Fact]
        public void Validate_ThresholdEqualsOne_ReturnsSuccess()
        {
            // Arrange
            var options = CreateValidOptions();
            options.Threshold = 1;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.Equal(ValidateOptionsResult.Success, result);
        }

        /// <summary>
        /// Tests that Threshold == 5 is valid (default value).
        /// </summary>
        [Fact]
        public void Validate_ThresholdEqualsFive_ReturnsSuccess()
        {
            // Arrange
            var options = CreateValidOptions();
            options.Threshold = 5;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.Equal(ValidateOptionsResult.Success, result);
        }

        /// <summary>
        /// Tests that DetectionWindow must be greater than TimeSpan.Zero.
        /// </summary>
        [Theory]
        [InlineData(0, 0, 0)] // TimeSpan.Zero
        [InlineData(-1, 0, 0)] // Negative ticks
        [InlineData(0, -1, 0)] // Negative milliseconds
        public void Validate_DetectionWindowLessThanOrEqualToZero_ReturnsFailure(int days, int hours, int milliseconds)
        {
            // Arrange
            var options = CreateValidOptions();
            options.DetectionWindow = new TimeSpan(days, hours, 0, 0, milliseconds);

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("DetectionWindow must be greater than zero.", result.Failures);
        }

        /// <summary>
        /// Tests that DetectionWindow == TimeSpan.FromSeconds(1) is valid (boundary value).
        /// </summary>
        [Fact]
        public void Validate_DetectionWindowEqualsOneSecond_ReturnsSuccess()
        {
            // Arrange
            var options = CreateValidOptions();
            options.DetectionWindow = TimeSpan.FromSeconds(1);

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.Equal(ValidateOptionsResult.Success, result);
        }

        /// <summary>
        /// Tests that DetectionWindow == TimeSpan.FromSeconds(2) is valid (default value).
        /// </summary>
        [Fact]
        public void Validate_DetectionWindowEqualsTwoSeconds_ReturnsSuccess()
        {
            // Arrange
            var options = CreateValidOptions();
            options.DetectionWindow = TimeSpan.FromSeconds(2);

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.Equal(ValidateOptionsResult.Success, result);
        }

        /// <summary>
        /// Tests that LowSeverityThreshold must be non-negative.
        /// </summary>
        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Validate_LowSeverityThresholdNegative_ReturnsFailure(int lowSeverityThreshold)
        {
            // Arrange
            var options = CreateValidOptions();
            options.LowSeverityThreshold = lowSeverityThreshold;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("LowSeverityThreshold must be non-negative.", result.Failures);
        }

        /// <summary>
        /// Tests that LowSeverityThreshold == 0 is valid (boundary value).
        /// </summary>
        [Fact]
        public void Validate_LowSeverityThresholdEqualsZero_ReturnsSuccess()
        {
            // Arrange
            var options = CreateValidOptions();
            options.LowSeverityThreshold = 0;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.Equal(ValidateOptionsResult.Success, result);
        }

        /// <summary>
        /// Tests that LowSeverityThreshold == 10 is valid (default value).
        /// </summary>
        [Fact]
        public void Validate_LowSeverityThresholdEqualsTen_ReturnsSuccess()
        {
            // Arrange
            var options = CreateValidOptions();
            options.LowSeverityThreshold = 10;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.Equal(ValidateOptionsResult.Success, result);
        }

        /// <summary>
        /// Tests that MediumSeverityThreshold must be greater than LowSeverityThreshold.
        /// </summary>
        [Theory]
        [InlineData(9, 10)] // Less than LowSeverityThreshold
        [InlineData(10, 10)] // Equal to LowSeverityThreshold
        public void Validate_MediumSeverityThresholdLessThanOrEqualToLowSeverityThreshold_ReturnsFailure(int mediumThreshold, int lowThreshold)
        {
            // Arrange
            var options = CreateValidOptions();
            options.MediumSeverityThreshold = mediumThreshold;
            options.LowSeverityThreshold = lowThreshold;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("MediumSeverityThreshold must be greater than LowSeverityThreshold.", result.Failures);
        }

        /// <summary>
        /// Tests that MediumSeverityThreshold > LowSeverityThreshold is valid.
        /// </summary>
        [Fact]
        public void Validate_MediumSeverityThresholdGreaterThanLowSeverityThreshold_ReturnsSuccess()
        {
            // Arrange
            var options = CreateValidOptions();
            options.LowSeverityThreshold = 10;
            options.MediumSeverityThreshold = 50;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.Equal(ValidateOptionsResult.Success, result);
        }

        /// <summary>
        /// Tests that MediumSeverityThreshold == LowSeverityThreshold + 1 is valid (boundary value).
        /// </summary>
        [Fact]
        public void Validate_MediumSeverityThresholdEqualsLowSeverityThresholdPlusOne_ReturnsSuccess()
        {
            // Arrange
            var options = CreateValidOptions();
            options.LowSeverityThreshold = 10;
            options.MediumSeverityThreshold = 11;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.Equal(ValidateOptionsResult.Success, result);
        }

        /// <summary>
        /// Tests that IgnoredQueryPatterns cannot be null.
        /// </summary>
        [Fact]
        public void Validate_IgnoredQueryPatternsNull_ReturnsFailure()
        {
            // Arrange
            var options = CreateValidOptions();
            options.IgnoredQueryPatterns = null!;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("IgnoredQueryPatterns cannot be null.", result.Failures);
        }

        /// <summary>
        /// Tests that IgnoredQueryPatterns can be empty but not null.
        /// </summary>
        [Fact]
        public void Validate_IgnoredQueryPatternsEmptyList_ReturnsSuccess()
        {
            // Arrange
            var options = CreateValidOptions();
            options.IgnoredQueryPatterns = new System.Collections.Generic.List<string>();

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.Equal(ValidateOptionsResult.Success, result);
        }

        /// <summary>
        /// Tests that IgnoredQueryPatterns cannot contain whitespace patterns.
        /// </summary>
        [Fact]
        public void Validate_IgnoredQueryPatternsContainsWhitespace_ReturnsFailure()
        {
            // Arrange
            var options = CreateValidOptions();
            options.IgnoredQueryPatterns.Add("   ");

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("IgnoredQueryPatterns contains an empty or whitespace pattern.", result.Failures);
        }

        /// <summary>
        /// Tests that IgnoredQueryPatterns cannot contain null patterns.
        /// </summary>
        [Fact]
        public void Validate_IgnoredQueryPatternsContainsNull_ReturnsFailure()
        {
            // Arrange
            var options = CreateValidOptions();
            options.IgnoredQueryPatterns.Add(null!);

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("IgnoredQueryPatterns contains an empty or whitespace pattern.", result.Failures);
        }

        /// <summary>
        /// Tests that IgnoredQueryPatterns cannot contain empty strings.
        /// </summary>
        [Fact]
        public void Validate_IgnoredQueryPatternsContainsEmptyString_ReturnsFailure()
        {
            // Arrange
            var options = CreateValidOptions();
            options.IgnoredQueryPatterns.Add(string.Empty);

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains("IgnoredQueryPatterns contains an empty or whitespace pattern.", result.Failures);
        }

        /// <summary>
        /// Tests that multiple validation failures are reported together.
        /// </summary>
        [Fact]
        public void Validate_MultipleFailures_ReturnsAllFailures()
        {
            // Arrange
            var options = CreateValidOptions();
            options.Threshold = 0;
            options.DetectionWindow = TimeSpan.Zero;
            options.LowSeverityThreshold = -5;
            options.IgnoredQueryPatterns = null!;

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(result.Failures, f => f.Contains("Threshold must be greater than 0."));
            Assert.Contains(result.Failures, f => f.Contains("DetectionWindow must be greater than zero."));
            Assert.Contains(result.Failures, f => f.Contains("LowSeverityThreshold must be non-negative."));
            Assert.Contains(result.Failures, f => f.Contains("IgnoredQueryPatterns cannot be null."));
        }

        /// <summary>
        /// Tests that ValidateInternal returns a failure result with all validation errors.
        /// </summary>
        [Fact]
        public void ValidateInternal_MultipleFailures_ReturnsAllErrors()
        {
            // Arrange
            var options = CreateValidOptions();
            options.Threshold = 0;
            options.DetectionWindow = TimeSpan.Zero;

            // Act
            var result = _validator.ValidateInternal(options);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(2, result.ValidationErrors.Count);
            Assert.Contains("Threshold must be greater than 0.", result.ValidationErrors);
            Assert.Contains("DetectionWindow must be greater than zero.", result.ValidationErrors);
        }

        /// <summary>
        /// Tests that ValidateInternal returns a success result with empty errors.
        /// </summary>
        [Fact]
        public void ValidateInternal_ValidOptions_ReturnsEmptyErrors()
        {
            // Arrange
            var options = CreateValidOptions();

            // Act
            var result = _validator.ValidateInternal(options);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.ValidationErrors);
        }
    }
}