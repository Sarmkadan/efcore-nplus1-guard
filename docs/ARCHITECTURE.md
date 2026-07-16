# Architecture

EfCoreNPlusOneGuard is a single-project class library (`net10.0`, one dependency:
`Microsoft.EntityFrameworkCore.Relational`) that detects N+1 query patterns at runtime
by intercepting EF Core command execution. Everything lives in `src/` under the
`EfCoreNPlusOneGuard` namespace - there are no layers, no DI container, no hosted services.
That is deliberate: the library must be droppable into any app with one line in
`AddDbContext` and zero infrastructure assumptions.

## Data flow

```
DbContext executes a query
        |
        v
NPlusOneGuardInterceptor (DbCommandInterceptor)
  ReaderExecuting / ReaderExecutingAsync
        |  skip if CommandText matches an IgnoredQueryPatterns substring
        v
QueryTracker.TrackExecution(commandText)
        |
        v
QueryFingerprint.Create(sql, callSite)
  - normalize SQL (strip literals/params, collapse whitespace, lowercase)
  - SHA256 of the normalized text
        |
        v
QueryTracker.Record(fingerprint)
  - sliding window of timestamps per fingerprint
    (ConcurrentDictionary<QueryFingerprint, ImmutableList<DateTimeOffset>>)
  - prune entries older than DetectionWindow
  - if count in window >= Threshold -> NPlusOneIncident
        |
        v
on incident:
  - onDetected callback (user-provided, optional)
  - Console.Error line if LogOnDetection
  - throw NPlusOneDetectedException if ThrowOnDetection
```

## Components

### Detection core

- **`NPlusOneGuardExtensions.UseNPlusOneGuard`** - the single public entry point.
  Builds an `NPlusOneGuardOptions`, applies the user's configure delegate, and registers
  an `NPlusOneGuardInterceptor` on the `DbContextOptionsBuilder`. The interceptor owns
  its own `QueryTracker`; state is therefore scoped to one `UseNPlusOneGuard` call, not
  global. If you register the guard on two contexts you get two independent trackers.

- **`NPlusOneGuardInterceptor`** - a `DbCommandInterceptor` overriding only
  `ReaderExecuting(Async)`. Inserts/updates/deletes go through `NonQueryExecuting`, which
  is intentionally not intercepted: N+1 is a read problem, and tracking writes would just
  add noise and overhead. The "async" tracking path is synchronous under the hood
  (returns `ValueTask.CompletedTask`) - tracking is an in-memory dictionary update, there
  is nothing to await, but the override must exist so async queries are seen at all.

- **`QueryFingerprint`** - immutable value object identifying "the same query".
  Normalization is regex-based (`@p0`/`:p0`/`?0`, numeric and string literals -> `?`),
  so `WHERE Id = 1` and `WHERE Id = 2` collapse into one fingerprint - that collapse is
  exactly what turns N separate lookups into one countable pattern. Equality is
  (hash, call site); the SHA256 is precomputed at construction so dictionary lookups
  stay cheap.

- **`QueryTracker`** - thread-safe sliding window counter. Trade-off worth knowing:
  cleanup of expired timestamps runs inline on every `Record` call and walks all keys.
  Simple and allocation-friendly for the intended use (a dev/staging diagnostic with a
  2-second default window), but it is O(total tracked queries) per execution - one more
  reason this guard is not meant to stay enabled on a hot production path. The
  `ImmutableList` values make the read-modify-write in `AddOrUpdate` safe without locks.

- **`NPlusOneGuardOptions`** - plain mutable options object: `Threshold` (default 5),
  `DetectionWindow` (default 2 s), `ThrowOnDetection` (default false),
  `LogOnDetection` (default true), `IgnoredQueryPatterns` (substring match,
  case-insensitive; useful for migrations-history noise).

- **`NPlusOneDetectedException`** - carries the incident; only thrown when the user
  opts into `ThrowOnDetection`. Failing the request is a valid CI/test strategy but a
  bad production default, hence off by default.

### Incident model and reporting

- **`NPlusOneIncident` / `NPlusOneSeverity`** (`src/NPlusOneIncident.cs`) - mutable POCO
  with `SqlQuery` (normalized text), `Count`, `Severity`, `StackTrace`. Mutable and
  JSON-serializable on purpose so reporters and user callbacks can round-trip it without
  ceremony.

- **`IIncidentReporter`** - the one-method sink interface (`Report(NPlusOneIncident)`).
  Wire a reporter up in the `onDetected` callback of `UseNPlusOneGuard`; the library does
  not auto-register any reporter, again to avoid I/O side effects the caller did not ask for.

- **`FileIncidentReporter`** - pipe-delimited text lines, append or truncate-once
  semantics, internal lock for cross-thread writes.

- **`JsonIncidentReporter`** - JSON Lines output, one incident per line, same locking
  approach.

- **`IncidentAggregator`** - in-memory collector that groups incidents by SQL text and
  produces a summary string (totals, unique fingerprints, top offenders). Meant for
  end-of-test-run reporting rather than streaming.

- **`HtmlIncidentReportWriter`** - renders a standalone HTML report (inline CSS,
  severity-colored table, collapsible stack traces). Output is fully self-contained so
  it can be attached to CI artifacts. All user-controlled strings go through
  `HtmlEncoder` - SQL text in a report is untrusted input like any other.

### Auxiliary analysis types

- **`DuplicateQueryDetector`** - a simpler, window-less duplicate counter
  (`Record`/`GetDuplicates`/`Clear`) for manual, batch-style analysis of a captured query
  log. It is not used by the interceptor pipeline.

- **`QueryStatistics`** - records per-query counts and durations for ad-hoc profiling.
  Also standalone.

- **`CallSiteWhitelist`** - holds call-site patterns that should be exempt from
  detection. Standalone as well; the interceptor currently passes a constant call site
  (`"QueryTracker.TrackExecution"`), so whitelist matching is a consumer-side concern
  for now (see limitations).

### Extension/validation/JSON satellites

Most core types have `*Extensions`, `*Validation`, and `*JsonExtensions` companions
(sorting/filtering helpers, invariant checks, System.Text.Json round-tripping). They are
pure helpers over the core types - no state, no cross-dependencies - and can be ignored
when reasoning about the detection pipeline.

## Key design decisions

1. **Interceptor, not a wrapper provider.** `DbCommandInterceptor` is public, stable EF
   API and composes with any relational provider. The alternative (a diagnostics
   listener) sees more events but is stringly-typed and version-fragile.
2. **Fingerprint by normalized SQL, not raw SQL.** Raw text would make every
   parameterized lookup unique and the detector blind to the very pattern it hunts.
   Cost: two structurally different queries that normalize identically are merged; in
   practice EF-generated SQL makes such collisions unlikely.
3. **No background timer for window cleanup.** Cleanup piggybacks on `Record`. A timer
   would need lifecycle management (dispose, app shutdown) that a library configured
   through `DbContextOptionsBuilder` has no good hook for.
4. **Reporting is pull/callback, never automatic.** The guard itself only counts;
   anything with I/O (file, JSON, HTML) is invoked explicitly by the consumer. A
   diagnostics library silently writing files is how you get support tickets.
5. **`Console.Error` for the built-in log line.** No `ILogger` dependency keeps the
   package dependency-free beyond EF itself. The trade-off (unstructured logging) is
   mitigated by `onDetected`, where the consumer can bridge to any logging framework.

## Extension points

- `onDetected` callback in `UseNPlusOneGuard` - the primary hook; feed incidents to an
  `IIncidentReporter`, an `IncidentAggregator`, metrics, or your logger.
- `IIncidentReporter` - implement for custom sinks (Slack, OTLP, database).
- `NPlusOneGuardOptions.IgnoredQueryPatterns` - suppression by SQL substring.
- `HtmlIncidentReportWriter.Generate` returns a string, so the HTML can be embedded
  rather than written to disk.

## Known limitations

- **Call-site capture is a stub.** `QueryTracker.TrackExecution` fingerprints with a
  constant call site and grabs `Environment.StackTrace` only after detection, so
  `CallSiteWhitelist` cannot yet suppress incidents inside the pipeline. Wiring real
  call-site extraction into `QueryFingerprint.Create` is the obvious next step.
- **Severity is always `High`** when the tracker raises an incident; the
  `Low`/`Medium` levels are only meaningful for manually constructed incidents today.
- **Window cleanup is O(all fingerprints) per query** - fine for dev/test, not free on
  a busy production context.
- **The detector cannot distinguish a legitimate loop** (e.g. intentional per-tenant
  queries) from an accidental N+1; that is what `IgnoredQueryPatterns` and the
  non-throwing default are for.
- No test project in the repo yet.
