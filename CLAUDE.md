# CLAUDE.md

EfCoreNPlusOneGuard: a `net10.0` class library that detects EF Core N+1 query patterns at runtime via a `DbCommandInterceptor`, enabled with one call to `UseNPlusOneGuard` on `DbContextOptionsBuilder`.

## Build

No `.sln` file. Build the projects directly (SDK 10.0.x required):

```bash
dotnet build src/efcore-nplus1-guard.csproj
dotnet build tests/EfCoreNPlusOneGuard.Tests.csproj   # also builds src
dotnet pack src/efcore-nplus1-guard.csproj            # NuGet package EfCoreNPlusOneGuard
```

## Test

xUnit 2.9 via `Microsoft.NET.Test.Sdk`:

```bash
dotnet test tests/EfCoreNPlusOneGuard.Tests.csproj
dotnet test tests/EfCoreNPlusOneGuard.Tests.csproj --filter "FullyQualifiedName~QueryTrackerTests"
```

`python3 aider_buildcmd.py` runs plain `dotnet test` from the root; it does not work without a solution file, prefer the explicit project path above.

## Lint / format

No `.editorconfig`, analyzers or CI configured. `Nullable` and `ImplicitUsings` are enabled and `GenerateDocumentationFile` is on, so missing XML docs on public members produce warnings. Use `dotnet format src/efcore-nplus1-guard.csproj` if formatting is needed.

## Layout

- `src/` - the library, flat, single namespace `EfCoreNPlusOneGuard`. No layers, no DI, one dependency (`Microsoft.EntityFrameworkCore.Relational` 10.0.0).
  - `NPlusOneGuardExtensions.cs` - `UseNPlusOneGuard(...)`, the only public entry point.
  - `NPlusOneGuardInterceptor.cs` - `DbCommandInterceptor` overriding `ReaderExecuting(Async)` only.
  - `QueryFingerprint.cs` - SQL normalization + SHA256 identity of "the same query".
  - `QueryTracker.cs` - thread-safe sliding-window counter; raises `NPlusOneIncident`.
  - `NPlusOneGuardOptions.cs` - `Threshold`, `DetectionWindow`, `ThrowOnDetection`, `LogOnDetection`, `IgnoredQueryPatterns`.
  - `IIncidentReporter.cs` + `File/Json/Csv/Markdown/GitHubAnnotation/InMemoryIncidentReporter.cs`, `HtmlIncidentReportWriter.cs`, `IncidentAggregator.cs` - reporting sinks; never auto-registered, wire them through the `onDetected` callback.
  - `DuplicateQueryDetector.cs`, `QueryStatistics.cs`, `CallSiteWhitelist.cs` - standalone analysis helpers, not used by the interceptor pipeline.
- `tests/` - xUnit project, namespace `EfCoreNPlusOneGuard.Tests`, one `*Tests.cs` per source type.
- `docs/ARCHITECTURE.md` - data flow, design decisions, known limitations (read this first). Other `docs/*.md` describe individual types.
- `README.md` - per-type usage examples.

Oddities to be aware of: `src/CallSiteWhitelistExtensionsTests.cs`, `src/CallSiteWhitelistJsonExtensionsTests.cs` and `src/QueryTrackerJsonExtensionsTests.cs` are test files that live in the library project; a stray file named `}` sits in the repo root. Do not add new tests under `src/`.

## Conventions

- File-scoped namespaces, `#nullable enable`, MIT license header comment at top of source files, XML doc comments on all public members.
- Naming: `NPlusOne*` prefix for core types; companions per core type follow `<Type>Extensions` (query/sort helpers), `<Type>Validation` (+ `*ValidationResult`, invariant checks), `<Type>JsonExtensions` (System.Text.Json round-trip). Companions are stateless static classes.
- Guard arguments with `ArgumentNullException.ThrowIfNull`.
- Concurrency: `ConcurrentDictionary` + immutable collections rather than locks in the tracker; reporters that do I/O use a private lock object.
- Reporting is explicit and pull/callback-based; the library never writes files or logs on its own beyond the optional `Console.Error` line (no `ILogger` dependency by design).
- Tests: `[Fact]`, method names `Member_Scenario_ExpectedResult`, plain `Assert.*`, no mocking library.
- Commit messages: conventional prefixes (`feat:`, `fix:`, `docs:`, `chore:`).
