using System;
using Xunit;

namespace EfCoreNPlusOneGuard;

public class QueryFingerprintJsonExtensionsTests
{
    [Fact]
    public void ToJson_WithValidQueryFingerprint_ReturnsJsonString()
    {
        // Arrange
        var fingerprint = QueryFingerprint.Create(
            "SELECT * FROM Users WHERE Id = @id",
            "UserService.GetUser");

        // Act
        var json = fingerprint.ToJson();

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Contains("commandTextHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("normalizedSql", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("callSite", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToJson_WithIndentedTrue_ReturnsFormattedJson()
    {
        // Arrange
        var fingerprint = QueryFingerprint.Create(
            "SELECT * FROM Users WHERE Id = @id",
            "UserService.GetUser");

        // Act
        var json = fingerprint.ToJson(indented: true);

        // Assert
        Assert.NotNull(json);
        Assert.Contains(Environment.NewLine, json);
    }

    [Fact]
    public void ToJson_WithIndentedFalse_ReturnsCompactJson()
    {
        // Arrange
        var fingerprint = QueryFingerprint.Create(
            "SELECT * FROM Users WHERE Id = @id",
            "UserService.GetUser");

        // Act
        var json = fingerprint.ToJson(indented: false);

        // Assert
        Assert.NotNull(json);
        Assert.DoesNotContain(Environment.NewLine, json);
    }

    [Fact]
    public void ToJson_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        QueryFingerprint? fingerprint = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => fingerprint!.ToJson());
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void FromJson_WithValidJson_ReturnsQueryFingerprint()
    {
        // Arrange
        var original = QueryFingerprint.Create(
            "SELECT * FROM Products WHERE Category = @cat",
            "ProductService.GetProducts");
        var json = original.ToJson();

        // Act
        var deserialized = QueryFingerprintJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.CommandTextHash, deserialized.CommandTextHash);
        Assert.Equal(original.NormalizedSql, deserialized.NormalizedSql);
        Assert.Equal(original.CallSite, deserialized.CallSite);
    }

    [Fact]
    public void FromJson_WithNullJson_ThrowsArgumentNullException()
    {
        // Arrange
        string? json = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => QueryFingerprintJsonExtensions.FromJson(json!));
        Assert.Equal("json", ex.ParamName);
    }

    [Fact]
    public void FromJson_WithEmptyJson_ThrowsArgumentException()
    {
        // Arrange
        var json = string.Empty;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => QueryFingerprintJsonExtensions.FromJson(json));
        Assert.Equal("json", ex.ParamName);
    }

    [Fact]
    public void FromJson_WithWhitespaceJson_ThrowsJsonException()
    {
        // Arrange
        var json = "   ";

        // Act & Assert
        Assert.Throws<System.Text.Json.JsonException>(() => QueryFingerprintJsonExtensions.FromJson(json));
    }

    [Fact]
    public void FromJson_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        var json = "{ invalid json }";

        // Act & Assert
        Assert.Throws<System.Text.Json.JsonException>(() => QueryFingerprintJsonExtensions.FromJson(json));
    }

    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndDeserializedValue()
    {
        // Arrange
        var original = QueryFingerprint.Create(
            "SELECT COUNT(*) FROM Orders WHERE Status = @status",
            "OrderService.CountOrders");
        var json = original.ToJson();

        // Act
        var result = QueryFingerprintJsonExtensions.TryFromJson(json, out var deserialized);

        // Assert
        Assert.True(result);
        Assert.NotNull(deserialized);
        Assert.Equal(original.CommandTextHash, deserialized.CommandTextHash);
        Assert.Equal(original.NormalizedSql, deserialized.NormalizedSql);
        Assert.Equal(original.CallSite, deserialized.CallSite);
    }

    [Fact]
    public void TryFromJson_WithNullJson_ThrowsArgumentNullException()
    {
        // Arrange
        string? json = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => QueryFingerprintJsonExtensions.TryFromJson(json!, out var _));
    }

    [Fact]
    public void TryFromJson_WithEmptyJson_ThrowsArgumentException()
    {
        // Arrange
        var json = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => QueryFingerprintJsonExtensions.TryFromJson(json, out var _));
    }

    [Fact]
    public void TryFromJson_WithWhitespaceJson_ReturnsFalseAndNullValue()
    {
        // Arrange
        var json = "   ";

        // Act
        var result = QueryFingerprintJsonExtensions.TryFromJson(json, out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_WithInvalidJson_ReturnsFalseAndNullValue()
    {
        // Arrange
        var json = "{ invalid }";

        // Act
        var result = QueryFingerprintJsonExtensions.TryFromJson(json, out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void RoundTrip_WithQueryFingerprint_PreservesAllProperties()
    {
        // Arrange
        var original = QueryFingerprint.Create(
            "SELECT u.Id, u.Name, u.Email FROM Users u WHERE u.IsActive = 1 AND u.CreatedAt > @date",
            "UserRepository.GetActiveUsersAfterDate");

        // Act
        var json = original.ToJson();
        var deserialized = QueryFingerprintJsonExtensions.FromJson(json);

        // Assert
        Assert.Equal(original.CommandTextHash, deserialized.CommandTextHash);
        Assert.Equal(original.NormalizedSql, deserialized.NormalizedSql);
        Assert.Equal(original.CallSite, deserialized.CallSite);
        Assert.Equal(original, deserialized);
    }

    [Fact]
    public void RoundTrip_WithIndentedSerialization_PreservesProperties()
    {
        // Arrange
        var original = QueryFingerprint.Create(
            "INSERT INTO Logs (Message, Timestamp) VALUES (@msg, @ts)",
            "LoggingService.LogMessage");

        // Act
        var json = original.ToJson(indented: true);
        var deserialized = QueryFingerprintJsonExtensions.FromJson(json);

        // Assert
        Assert.Equal(original.CommandTextHash, deserialized.CommandTextHash);
        Assert.Equal(original.NormalizedSql, deserialized.NormalizedSql);
        Assert.Equal(original.CallSite, deserialized.CallSite);
    }

    [Fact]
    public void RoundTrip_WithComplexCallSite_PreservesCallSite()
    {
        // Arrange
        var callSite = "MyNamespace.MyClass.MyMethod" + Environment.NewLine +
                       "at MyNamespace.MyClass.MyMethod in MyFile.cs:line 42" + Environment.NewLine +
                       "at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)";
        var original = QueryFingerprint.Create(
            "UPDATE Products SET Price = @price WHERE Id = @id",
            callSite);

        // Act
        var json = original.ToJson();
        var deserialized = QueryFingerprintJsonExtensions.FromJson(json);

        // Assert
        Assert.Equal(original.CallSite, deserialized.CallSite);
    }

    [Fact]
    public void RoundTrip_WithEmptyCallSite_PreservesEmptyCallSite()
    {
        // Arrange
        var original = QueryFingerprint.Create(
            "SELECT 1",
            string.Empty);

        // Act
        var json = original.ToJson();
        var deserialized = QueryFingerprintJsonExtensions.FromJson(json);

        // Assert
        Assert.Equal(string.Empty, deserialized.CallSite);
    }
}