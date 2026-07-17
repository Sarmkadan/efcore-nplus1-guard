# CallSiteWhitelistJsonExtensions

Provides JSON serialization and deserialization extensions for `CallSiteWhitelist` instances, enabling round-trip serialization of whitelist configurations.

## API

### `ToJson`

```csharp
public static string ToJson(this CallSiteWhitelist value, bool indented = false)
```

Serializes a `CallSiteWhitelist` instance to a JSON string.

**Parameters:**
- `value` - The whitelist to serialize. Must not be null.
- `indented` - When true, formats the JSON with indentation for readability. Defaults to false.

**Returns:**
- A JSON string representing the serialized whitelist.

**Exceptions:**
- `ArgumentNullException` - Thrown when `value` is null.

**Remarks:**
The JSON output uses camelCase property naming policy. The serialized format represents each whitelist entry as an object with a `type` field indicating either `"exact"` or `"pattern"`, followed by the appropriate properties for that entry type.

---

### `FromJson`

```csharp
public static CallSiteWhitelist? FromJson(string json)
```

Deserializes a `CallSiteWhitelist` instance from a JSON string.

**Parameters:**
- `json` - The JSON string to deserialize. Must not be null or empty.

**Returns:**
- The deserialized `CallSiteWhitelist` instance, or null if the JSON represents a null value.

**Exceptions:**
- `ArgumentException` - Thrown when `json` is null or empty.
- `JsonException` - Thrown when JSON parsing fails or when required properties are missing from the JSON.

**Remarks:**
The method uses camelCase property naming policy during deserialization. The JSON format must match the structure produced by `ToJson`, with each entry represented as an object containing a `type` property.

---

### `TryFromJson`

```csharp
public static bool TryFromJson(string json, out CallSiteWhitelist? value)
```

Attempts to deserialize a `CallSiteWhitelist` instance from a JSON string.

**Parameters:**
- `json` - The JSON string to deserialize.
- `value` - Output parameter that receives the deserialized whitelist if successful, or null if deserialization fails.

**Returns:**
- `true` if deserialization succeeds; `false` otherwise.

**Remarks:**
This method provides a safe alternative to `FromJson` that does not throw exceptions on failure. Instead, it returns false and sets `value` to null when the JSON is malformed or contains invalid data.

---

### `CallSiteWhitelistConverter`

```csharp
internal sealed class CallSiteWhitelistConverter : JsonConverter<CallSiteWhitelist>
```

A custom JSON converter that handles serialization and deserialization of `CallSiteWhitelist` instances.

**Remarks:**
This converter is used internally by the JSON serialization methods and is not intended for direct use. It handles the conversion between the internal representation of whitelist entries and the JSON format.

---

### `Read`

```csharp
public override CallSiteWhitelist? Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options)
```

Reads and converts JSON to a `CallSiteWhitelist` instance.

**Parameters:**
- `reader` - The JSON reader.
- `typeToConvert` - The type to convert (should be `CallSiteWhitelist`).
- `options` - The serializer options.

**Returns:**
- The deserialized `CallSiteWhitelist` instance.

**Exceptions:**
- `JsonException` - Thrown when JSON parsing fails or when required properties (`typeName` for exact entries, `pattern` for pattern entries) are missing.

**Remarks:**
This method is called by the JSON serializer during deserialization. It expects an array of entry objects, each containing a `type` field with value `"exact"` or `"pattern"`.

---

### `Write`

```csharp
public override void Write(
    Utf8JsonWriter writer,
    CallSiteWhitelist value,
    JsonSerializerOptions options)
```

Writes a `CallSiteWhitelist` instance to JSON.

**Parameters:**
- `writer` - The JSON writer.
- `value` - The whitelist to serialize.
- `options` - The serializer options.

**Exceptions:**
- `InvalidOperationException` - Thrown when the internal `_entries` field cannot be found via reflection.

**Remarks:**
This method is called by the JSON serializer during serialization. It writes the whitelist as a JSON array of entry objects with camelCase property names.

## Usage

### Serializing a whitelist

```csharp
var whitelist = new CallSiteWhitelist();
whitelist.Add("MyNamespace.MyType");
whitelist.Add("MyNamespace.AnotherType", "SpecificMethod");
whitelist.AddPattern("System.*");

// Serialize with compact JSON
string json = whitelist.ToJson();

// Serialize with pretty-printed JSON
string prettyJson = whitelist.ToJson(indented: true);
```

### Deserializing a whitelist

```csharp
string json = """
[
  {"type": "exact", "typeName": "MyNamespace.MyType"},
  {"type": "exact", "typeName": "MyNamespace.AnotherType", "methodName": "SpecificMethod"},
  {"type": "pattern", "pattern": "System.*"}
]
""";

// Safe deserialization with error handling
if (CallSiteWhitelistJsonExtensions.TryFromJson(json, out var loadedWhitelist))
{
    // Use loadedWhitelist
    Console.WriteLine($"Loaded {loadedWhitelist.Count} whitelist entries");
}
else
{
    Console.WriteLine("Failed to deserialize whitelist");
}

// Alternative: direct deserialization (may throw)
var whitelist = CallSiteWhitelistJsonExtensions.FromJson(json);
```

## Notes

- **Thread safety**: The `CallSiteWhitelistJsonExtensions` class is thread-safe for concurrent read operations. The internal `JsonSerializerOptions` instance is immutable after construction, and the `CallSiteWhitelistConverter` maintains no mutable state between serialization calls.

- **Null handling**: `FromJson` returns null when deserializing a JSON null value, while `TryFromJson` returns false and sets the output parameter to null in the same scenario.

- **Format consistency**: The JSON format produced by `ToJson` must be used as input to `FromJson` and `TryFromJson`. The format uses camelCase property names and represents whitelist entries as objects with a `type` field.

- **Error handling**: `FromJson` throws `JsonException` for malformed JSON or missing required properties, while `TryFromJson` provides a non-throwing alternative for error handling scenarios.

- **Performance**: The `ToJson` method creates a new `JsonSerializerOptions` instance when `indented` is true, avoiding any mutation of the shared default options.