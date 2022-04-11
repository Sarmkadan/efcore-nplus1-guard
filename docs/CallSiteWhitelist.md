# CallSiteWhitelist

The `CallSiteWhitelist` type maintains a collection of patterns that define which call sites should be excluded from N+1 query detection in Entity Framework Core operations. It allows precise control over whitelisting by supporting exact type-method matches, wildcard patterns, and regular expressions, enabling selective suppression of false positives in query analysis.

## API

### `public string TypeName`
Gets or sets the fully qualified type name for exact matching. When set, `MethodName` must also be specified to form a complete exact entry. If `TypeName` is non-null while `MethodName` is null, the behavior is undefined.

### `public string? MethodName`
Gets or sets the method name for exact matching. If non-null, `TypeName` must also be non-null. If both are non-null, they form an exact entry (`ExactEntry`) used for whitelisting.

### `public ExactEntry ExactEntry`
Represents a combined exact match entry derived from `TypeName` and `MethodName`. Throws `InvalidOperationException` if accessed when either `TypeName` or `MethodName` is null.

### `public string Pattern`
Gets or sets a wildcard pattern (e.g., `MyNamespace.MyType.*`) for whitelisting. Patterns are internally converted to regular expressions. Setting this property clears any existing `Regex` value.

### `public Regex Regex`
Gets the compiled regular expression used for whitelisting. This is derived from `Pattern` or set directly via `AddPattern`. Throws `InvalidOperationException` if accessed when no pattern or regex is defined.

### `public PatternEntry PatternEntry`
Represents a pattern-based entry derived from `Pattern` or `Regex`. Throws `InvalidOperationException` if accessed when neither `Pattern` nor `Regex` is defined.

### `public void Add(ExactEntry entry)`
Adds an exact type-method entry to the whitelist. Parameters:
- `entry`: The exact entry to add. Must not be null.

Throws `ArgumentNullException` if `entry` is null.

### `public void AddPattern(PatternEntry entry)`
Adds a pattern-based entry to the whitelist. Parameters:
- `entry`: The pattern entry to add. Must not be null.

Throws `ArgumentNullException` if `entry` is null.

### `public bool IsWhitelisted(string typeName, string methodName)`
Determines whether a given call site is whitelisted. Parameters:
- `typeName`: The fully qualified type name of the call site.
- `methodName`: The method name of the call site.

Returns `true` if the call site matches any exact entry or pattern in the whitelist; otherwise, `false`.

### `public void Clear()`
Removes all entries from the whitelist, resetting it to an empty state.

## Usage

### Example 1: Whitelisting Exact Call Sites
