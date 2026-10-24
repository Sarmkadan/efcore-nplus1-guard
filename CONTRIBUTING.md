# Contributing to EfCoreNPlusOneGuard

Thanks for helping out. This guide covers the setup, build and test commands, conventions and PR process.

## Prerequisites

- **.NET SDK 10.0.x.** The library and tests target `net10.0` (see `src/efcore-nplus1-guard.csproj`). There is no `global.json`, so any 10.0 SDK works.
- Git.

Check your setup:

```bash
dotnet --version   # should print 10.0.x
```

## Build

There is no `.sln` file. Build the projects directly:

```bash
dotnet build src/efcore-nplus1-guard.csproj
dotnet build tests/EfCoreNPlusOneGuard.Tests.csproj   # also builds src
```

A build should finish with no new warnings. Missing XML doc comments on public members are reported as warnings, so fix them before opening a PR.

## Test

Tests use xUnit and live in `tests/`:

```bash
dotnet test tests/EfCoreNPlusOneGuard.Tests.csproj
dotnet test tests/EfCoreNPlusOneGuard.Tests.csproj --filter "FullyQualifiedName~QueryTrackerTests"
```

Do not run `dotnet test` from the repository root; there is no solution file to pick up.

## Coding conventions

- Follow the conventions in [CLAUDE.md](CLAUDE.md) and the existing code. There is no `.editorconfig` yet, so match the surrounding style.
- `Nullable` and `ImplicitUsings` are enabled. Keep new code nullable-clean.
- Use file-scoped namespaces, `#nullable enable`, and the MIT license header at the top of source files.
- Every public member needs an XML doc comment.
- Guard arguments with `ArgumentNullException.ThrowIfNull`.
- Name tests `Member_Scenario_ExpectedResult` and use `[Fact]` with plain `Assert.*`.
- Put new tests in `tests/`, one `*Tests.cs` file per source type. Do not add tests under `src/`.
- Format only the code you touch. If you need to reformat, use `dotnet format src/efcore-nplus1-guard.csproj`.

## Pull requests

1. Branch from an up-to-date `main`. Do not commit to `main` directly.
2. Keep each PR small and focused on one change. Split unrelated fixes and refactors into separate PRs.
3. Add or update tests for every behavior change. A PR without tests needs a clear reason in the description.
4. Use conventional commit prefixes: `feat:`, `fix:`, `docs:`, `test:`, `chore:`.
5. Make sure the build and tests pass locally before pushing.
6. CI runs restore, build and test on every PR to `main`. It must pass before merge.
7. In the PR description, say what changed and why, and how you tested it.
