# NPlusOneGuardOptions

Configuration options for the Entity Framework Core N+1 query guard. These settings control how and when the guard detects and reacts to N+1 query patterns during query execution.

## API

### `Threshold`
- **Purpose**: The maximum allowed number of repeated queries for a given pattern before triggering detection.
- **Type**: `int`
- **Default**: Implementation-defined.
- **Usage**: Set to a positive integer to adjust sensitivity. Values less than 1 may disable threshold-based detection.

### `DetectionWindow`
- **Purpose**: The time span during which repeated queries are aggregated for detection.
- **Type**: `TimeSpan`
- **Default**: Implementation-defined.
- **Usage**: Controls the window for counting repeated queries. Must be a non-negative duration.

### `ThrowOnDetection`
- **Purpose**: Determines whether to throw an exception when an N+1 pattern is detected.
- **Type**: `bool`
- **Default**: `true`
- **Usage**: Set to `false` to suppress exceptions and allow execution to continue with logging only.

### `LogOnDetection`
- **Purpose**: Determines whether to log a warning when an N+1 pattern is detected.
- **Type**: `bool`
- **Default**: `true`
- **Usage**: Set to `false` to disable logging entirely, even if `ThrowOnDetection` is `false`.

### `IgnoredQueryPatterns`
- **Purpose**: List of regular expression patterns for queries that should be ignored during detection.
- **Type**: `List<string>`
- **Default**: Empty list.
- **Usage**: Add patterns to exclude known safe or intentional N+1 patterns from triggering the guard.

## Usage
