// tests/NPlusOneGuardOptionsJsonExtensionsTests.cs
namespace EfCoreNPlusOneGuard.Tests
{
    using Xunit;

    public class NPlusOneGuardOptionsJsonExtensionsTests
    {
        [Fact]
        public void ToJson_HappyPath_ReturnsJsonString()
        {
            // Arrange
            var options = new NPlusOneGuardOptions();
            var expectedJson = "{\"key\":\"value\"}";

            // Act
            var actualJson = NPlusOneGuardOptionsJsonExtensions.ToJson(options);

            // Assert
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void ToJson_NullOptions_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => NPlusOneGuardOptionsJsonExtensions.ToJson(null));
        }

        [Fact]
        public void FromJson_HappyPath_ReturnsOptions()
        {
            // Arrange
            var json = "{\"key\":\"value\"}";
            var expectedOptions = new NPlusOneGuardOptions();

            // Act
            var actualOptions = NPlusOneGuardOptionsJsonExtensions.FromJson(json);

            // Assert
            Assert.Equal(expectedOptions, actualOptions);
        }

        [Fact]
        public void FromJson_NullJson_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => NPlusOneGuardOptionsJsonExtensions.FromJson(null));
        }

        [Fact]
        public void FromJson_EmptyJson_ReturnsNull()
        {
            // Act
            var actualOptions = NPlusOneGuardOptionsJsonExtensions.FromJson("");

            // Assert
            Assert.Null(actualOptions);
        }

        [Fact]
        public void TryFromJson_HappyPath_ReturnsTrue()
        {
            // Arrange
            var json = "{\"key\":\"value\"}";
            var expectedOptions = new NPlusOneGuardOptions();

            // Act
            var result = NPlusOneGuardOptionsJsonExtensions.TryFromJson(json, out var actualOptions);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedOptions, actualOptions);
        }

        [Fact]
        public void TryFromJson_NullJson_ReturnsFalse()
        {
            // Act
            var result = NPlusOneGuardOptionsJsonExtensions.TryFromJson(null, out _);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryFromJson_EmptyJson_ReturnsFalse()
        {
            // Act
            var result = NPlusOneGuardOptionsJsonExtensions.TryFromJson("", out _);

            // Assert
            Assert.False(result);
        }
    }
}
