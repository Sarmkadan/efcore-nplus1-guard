// tests/CallSiteWhitelistValidationTests.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using EfCoreNPlusOneGuard;
using Xunit;

namespace EfCoreNPlusOneGuard.Tests
{
    public class CallSiteWhitelistValidationTests
    {
        private static CallSiteWhitelist CreateValidWhitelist()
        {
            var whitelist = new CallSiteWhitelist();
            whitelist.Add("MyNamespace.MyClass", "MyMethod");
            whitelist.AddPattern("MyNamespace.*");
            whitelist.AddFingerprint("deadbeef");
            return whitelist;
        }

        [Fact]
        public void Validate_HappyPath_ReturnsEmpty()
        {
            // Arrange
            var whitelist = CreateValidWhitelist();

            // Act
            var problems = whitelist.Validate();

            // Assert
            Assert.Empty(problems);
        }

        [Fact]
        public void IsValid_HappyPath_ReturnsTrue()
        {
            // Arrange
            var whitelist = CreateValidWhitelist();

            // Act
            var isValid = whitelist.IsValid();

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void EnsureValid_HappyPath_DoesNotThrow()
        {
            // Arrange
            var whitelist = CreateValidWhitelist();

            // Act / Assert
            var exception = Record.Exception(() => whitelist.EnsureValid());
            Assert.Null(exception);
        }

        [Fact]
        public void Validate_DetectsInvalidExactEntry()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();
            whitelist.Add("Valid.Type", "ValidMethod");

            // Corrupt the ExactEntry's TypeName to whitespace via reflection
            var entriesField = typeof(CallSiteWhitelist).GetField("_entries",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var entries = (IList<object>)entriesField.GetValue(whitelist)!;
            var exactEntry = entries[0];
            var typeNameProp = exactEntry.GetType().GetProperty("TypeName")!;
            typeNameProp.SetValue(exactEntry, "   ");

            // Act
            var problems = whitelist.Validate();

            // Assert
            Assert.Contains(problems, p => p.Contains("ExactEntry") && p.Contains("TypeName"));
        }

        [Fact]
        public void Validate_DetectsInvalidPatternEntry()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();
            whitelist.AddPattern("ValidPattern*");

            // Corrupt the PatternEntry's Pattern to null via reflection
            var entriesField = typeof(CallSiteWhitelist).GetField("_entries",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var entries = (IList<object>)entriesField.GetValue(whitelist)!;
            var patternEntry = entries[0];
            var patternProp = patternEntry.GetType().GetProperty("Pattern")!;
            patternProp.SetValue(patternEntry, null);

            // Act
            var problems = whitelist.Validate();

            // Assert
            Assert.Contains(problems, p => p.Contains("PatternEntry") && p.Contains("Pattern"));
        }

        [Fact]
        public void Validate_DetectsInvalidFingerprintEntry()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();
            whitelist.AddFingerprint("validhash");

            // Corrupt the FingerprintEntry's FingerprintHash to whitespace via reflection
            var entriesField = typeof(CallSiteWhitelist).GetField("_entries",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var entries = (IList<object>)entriesField.GetValue(whitelist)!;
            var fingerprintEntry = entries[0];
            var hashProp = fingerprintEntry.GetType().GetProperty("FingerprintHash")!;
            hashProp.SetValue(fingerprintEntry, "");

            // Act
            var problems = whitelist.Validate();

            // Assert
            Assert.Contains(problems, p => p.Contains("FingerprintEntry") && p.Contains("FingerprintHash"));
        }

        [Fact]
        public void EnsureValid_Invalid_ThrowsArgumentException()
        {
            // Arrange
            var whitelist = new CallSiteWhitelist();
            whitelist.Add("Only.Type");

            // Corrupt the entry
            var entriesField = typeof(CallSiteWhitelist).GetField("_entries",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var entries = (IList<object>)entriesField.GetValue(whitelist)!;
            var exactEntry = entries[0];
            var typeNameProp = exactEntry.GetType().GetProperty("TypeName")!;
            typeNameProp.SetValue(exactEntry, null);

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => whitelist.EnsureValid());
            Assert.Contains("CallSiteWhitelist is invalid", ex.Message);
        }

        [Fact]
        public void Validate_NullWhitelist_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CallSiteWhitelistValidation.Validate(null!));
        }

        [Fact]
        public void IsValid_NullWhitelist_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CallSiteWhitelistValidation.IsValid(null!));
        }

        [Fact]
        public void EnsureValid_NullWhitelist_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CallSiteWhitelistValidation.EnsureValid(null!));
        }
    }
}
