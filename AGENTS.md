# AGENTS.md

Instructions for AI coding agents working in this repository.

## What This Repo Does

FluentAssertions Migrator is a .NET tool that automatically rewrites [FluentAssertions](https://fluentassertions.com/) assertions into their [xUnit](https://xunit.net/) equivalents. It:

1. Opens a solution via Roslyn's `MSBuildWorkspace`
2. Walks every document's syntax tree with a `CSharpSyntaxRewriter`
3. Recognises `.Should().<Assertion>(...)` invocations and rewrites them to the matching `Assert.<Method>(...)` call
4. Removes the now-unused `using FluentAssertions;` directives
5. Applies all changes back to the solution

It is published as a .NET global tool on NuGet: `FluentAssertionsMigrator` (command: `migrate-fluentassertions`).

The full list of supported assertion mappings lives in [`README.md`](README.md); keep it in sync when adding or changing handlers.

## Tech Stack

- **.NET 10** (C#, top-level statements entry point)
- **Roslyn** (`Microsoft.CodeAnalysis.CSharp.Workspaces`, `MSBuildWorkspace`) for semantic analysis and rewriting
- **Microsoft.Build.Locator** to locate the installed MSBuild at runtime
- **xUnit v3** on the **Microsoft Testing Platform (MTP)** for testing
- **Microsoft.Extensions.Logging** (console logger with SimpleConsole formatter)

## Project Structure

```
src/
├── FluentAssertionsMigrator.sln            # Solution (tool + tests + fixture)
├── FluentAssertionsMigrator/
│   ├── Program.cs                          # Everything: entry point, solution/document
│   │                                       # migrators, and the CSharpSyntaxRewriter that
│   │                                       # holds all the .Should() -> Assert handlers
│   └── FluentAssertionsMigrator.csproj     # Package metadata, tool config, <Version>
└── FluentAssertionsMigrator.Tests/
    ├── FluentAssertionsSolutionMigratorTests.cs  # Integration test (see below)
    └── FluentAssertionsMigrator.Tests.csproj

testsolution/                               # Test fixture that the integration test migrates
├── TestSolution.sln
└── TestProject/
    ├── TestAssertions.cs                   # One [Fact] per assertion pattern, written with
    │                                       # real FluentAssertions calls
    └── TestProject.csproj                  # References FluentAssertions (the migration input)
```

## Build & Test

```bash
# Restore + build the solution (Release)
dotnet build src/FluentAssertionsMigrator.sln --configuration Release

# Run the tests
dotnet test src/FluentAssertionsMigrator.sln

# Build release + pack as dotnet tool
dotnet pack src/FluentAssertionsMigrator/FluentAssertionsMigrator.csproj --configuration Release --output ./nupkg
```

The CI workflow (`.github/workflows/dotnet.yml`) runs build + test on every push and PR.

> **Test runner note:** this repo uses the Microsoft Testing Platform. `global.json` pins
> `"test": { "runner": "Microsoft.Testing.Platform" }`, so `dotnet test` works natively.
> Test projects are executables (`<OutputType>Exe</OutputType>`) referencing `xunit.v3.mtp-v2`.

## How the Migrator Works

Everything lives in `src/FluentAssertionsMigrator/Program.cs`:

- **`FluentAssertionsSolutionMigrator`** opens the solution and iterates projects/documents.
- **`FluentAssertionsDocumentMigrator`** strips the `using FluentAssertions;` directives and runs the rewriter.
- **`FluentAssertionsSyntaxRewriter`** (a `CSharpSyntaxRewriter`) contains `HandleAssertionExpression`, a long chain of handlers. Each handler:
  - matches a specific `.Should().<Method>` suffix (via `EndsWith(...)`, or a `[GeneratedRegex]` when the method can take generic type arguments), then
  - builds the replacement `Assert.<...>` expression with `CreateAssertExpression`.
- Semantic helpers (`IsNullable`, `IsCollection`, `IsArray`, `IsLambdaExpression`) use the `SemanticModel` and **swallow exceptions**, returning `null` on failure — so a Roslyn/target-framework mismatch degrades migration quality silently rather than crashing. Keep this in mind when the tool "does nothing".

### Adding a new assertion handler

1. Add the handler to `HandleAssertionExpression` in `Program.cs`.
   - For methods that accept generic type arguments (e.g. `BeOfType<T>`), match with a `[GeneratedRegex]` using `(?:<.+>)?$` (the `<.+>` form is required to match **nested** generics like `List<int>`), not a plain `EndsWith`.
   - Watch handler ordering: a shorter suffix must not shadow a longer one (e.g. `BeGreaterThanOrEqualTo` must be checked such that it isn't caught by `BeGreaterThan`).
2. **Add a fixture case** in `testsolution/TestProject/TestAssertions.cs`: a `[Fact]` that calls the real FluentAssertions method. The integration test will migrate it and require the result to compile and pass.
3. Update the mappings table in `README.md`.
4. **Only add a handler when the xUnit translation is faithful.** If there is no sound equivalent (e.g. `NotBeEquivalentTo` — xUnit has no `Assert.NotEquivalent`, and `Assert.NotEqual` uses different semantics), leave it unhandled for manual migration rather than emit a subtly-wrong assertion.

### The integration test

`FluentAssertionsSolutionMigratorTests` is the primary safety net. It copies `testsolution/` to a temp folder, runs the migrator against it, then invokes `dotnet test` on the migrated solution. A green run proves that every fixture assertion both **migrated** and **compiles + passes** under xUnit. Prefer extending the fixture over adding narrowly-scoped unit tests.

## Releasing

Releases are managed via a manually-triggered GitHub Actions workflow (`.github/workflows/release.yml`) that uses [Versionize](https://github.com/versionize/versionize):

1. Go to **Actions → Release → Run workflow** (must run from `main`).
2. Versionize bumps the `<Version>` in the tool csproj (from commit history), updates `CHANGELOG.md`, commits, and tags — all locally.
3. Build + test + pack run in Release.
4. The package is pushed to nuget.org, and **only then** are the version commit and tag pushed, so a failed publish never leaves a dangling release tag.
5. A GitHub Release is created with notes extracted from `CHANGELOG.md`.

Requires a `NUGET_API_KEY` repository secret. Use the workflow's `version-override` input to set an explicit version (needed for the first release when there is no prior tag to bump from).

## Commit Messages

This repository uses **Conventional Commits** compatible with [Versionize](https://github.com/versionize/versionize). **Commit messages drive the version bump and the changelog**, so following this format is not optional — non-conventional messages produce empty changelog entries.

```
type(optional-scope): description

optional body

optional footer
```

### Allowed Types

| Type       | Purpose                          | Version Bump | In Changelog |
|------------|----------------------------------|--------------|--------------|
| `feat`     | New feature (e.g. a new handler) | Minor        | Yes          |
| `fix`      | Bug fix                          | Patch        | Yes          |
| `perf`     | Performance improvement          | Patch        | Yes          |
| `refactor` | Code change (no feature/fix)     | Patch        | Yes          |
| `docs`     | Documentation only               | None         | No           |
| `ci`       | CI/CD configuration              | None         | No           |
| `chore`    | Maintenance tasks                | None         | No           |
| `test`     | Adding or fixing tests           | None         | No           |
| `style`    | Formatting, whitespace           | None         | No           |

The changelog sections and their visibility are configured in [`.versionize`](.versionize).

### Breaking Changes

Append `!` after the type or include `BREAKING CHANGE:` in the footer to trigger a **major** version bump:

```
feat!: drop support for .NET 9

BREAKING CHANGE: the tool now targets net10.0 and requires the .NET 10 SDK
```

### Examples

```
feat: add handler for Should().ContainKey()
fix: match nested generic type arguments in the BeOfType regex
test: add fixture cases for the inclusive numeric comparison handlers
ci: add release publishing workflow with versionize
docs: document the supported ContainKey mapping in the README
```

### Rules

- Use lowercase for the type and description start
- Do not end the description with a period
- Keep the subject line under 72 characters
- Use the body for additional context when the subject alone is insufficient
