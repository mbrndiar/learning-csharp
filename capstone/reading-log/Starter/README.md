# 🏁 Reading Log Starter

This starter compiles and runs, but it intentionally leaves milestone-sized gaps.
Work through the milestones in order, from this folder
(`capstone/reading-log/Starter/`), and use the [capstone guide](../README.md),
[`SPEC.md`](../SPEC.md), and [`ARCHITECTURE.md`](../ARCHITECTURE.md) as your
requirements - this page is a map, not the answer key.

## 🗺️ Milestones and where to work

| Milestone | Gap | Where | Related course units |
| --- | --- | --- | --- |
| m1 | Tighten and extend domain validation rules. | `ReadingLog.Core/ReadingLogValidation.cs` | [Unit 07](../../../course/07-modeling-data-and-behavior/) (modeling and invariants), [Unit 02](../../../course/02-values-types-and-null/) (values and null) |
| m2 | Harden JSON storage with malformed-data checks and atomic replacement. | `ReadingLog.Storage.Json/JsonReadingLogRepository.cs` | [Unit 11](../../../course/11-files-streams-and-json/) (files, streams, and JSON) |
| m3 | Implement write operations and richer LINQ queries in `ReadingLogService`. | `ReadingLog.Core/ReadingLogService.cs` | [Unit 09](../../../course/09-linq-and-transformations/) (LINQ), [Unit 12](../../../course/12-async-cancellation-and-concurrency/) (async) |
| m4 | Finish POST endpoints, DTO validation paths, and problem contracts. | `ReadingLog.Api/Program.cs` | [Unit 13](../../../course/13-http-clients-and-minimal-apis/) (HTTP clients and Minimal APIs) |
| m5 | Finish CLI write commands and richer error handling. | `ReadingLog.Cli/CliApplication.cs`, `ReadingLog.Cli/ReadingLogApiClient.cs` | [Unit 13](../../../course/13-http-clients-and-minimal-apis/) (`HttpClient`), [Unit 05](../../../course/05-methods-errors-and-debugging/) (errors) |
| m6 | Run the full shared suite and polish edge cases until everything passes. | `ReadingLog.Cli/CliApplication.cs` and across all projects | [Unit 10](../../../course/10-testing-and-dependency-boundaries/) (testing), [Unit 14](../../../course/14-application-composition/) (composition and the full quality loop) |

Search the starter source for `TODO(m1)` through `TODO(m6)` to find the exact
lines that mark each gap.

Reading entries use `DateOnly` calendar dates parsed with the same strict ISO
`yyyy-MM-dd` pattern you practiced in Unit 07 - see the
[capstone guide](../README.md) for how dates, instants, and durations stay
distinct through this project.

## ▶️ Commands from this folder

Build the starter:

```bash
dotnet build ReadingLog.Starter.slnx --nologo
```

Verify the starter is already formatted:

```bash
dotnet format ReadingLog.Starter.slnx --verify-no-changes --no-restore
```

Run the shared test suite against the starter (expect focused failures for
unfinished milestones, not a build break):

```bash
dotnet test --project ../Tests/ReadingLog.Tests.csproj -p:CapstoneImplementation=Starter -- --filter-class ReadingLog.Tests.Smoke.StarterSmokeTests
dotnet test --project ../Tests/ReadingLog.Tests.csproj -p:CapstoneImplementation=Starter
```

Run the starter API and CLI locally once earlier milestones compile:

```bash
dotnet run --project ReadingLog.Api -- --urls http://127.0.0.1:5071
dotnet run --project ReadingLog.Cli -- list-books
```

## 🧩 Milestone gaps

- `TODO(m1)` – tighten and extend domain validation rules.
- `TODO(m2)` – harden JSON storage with malformed-data checks and atomic replacement.
- `TODO(m3)` – implement write operations and richer LINQ queries in `ReadingLogService`.
- `TODO(m4)` – finish POST endpoints, DTO validation paths, and problem contracts.
- `TODO(m5)` – finish CLI write commands and richer error handling.
- `TODO(m6)` – run the full shared suite and polish edge cases until everything passes.
