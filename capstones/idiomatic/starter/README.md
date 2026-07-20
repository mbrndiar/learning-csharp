# 🏁 C#-Idiomatic Reading Log Starter

This starter compiles and runs, but it intentionally leaves milestone-sized gaps.
Work through the milestones in order, from this folder
(`capstones/idiomatic/starter/`), and use the [capstone guide](../README.md),
[`SPEC.md`](../SPEC.md), and [`ARCHITECTURE.md`](../ARCHITECTURE.md) as your
requirements - this page is a map, not the answer key.

## 🗺️ Milestones and where to work

| Milestone | Gap | Where | Related course units |
| --- | --- | --- | --- |
| m1 | Tighten and extend domain validation rules. | `ReadingLog.Core/ReadingLogValidation.cs` | [Lesson 07](../../../lessons/07_modeling_data_and_behavior/) (modeling and invariants), [Lesson 02](../../../lessons/02_values_types_and_null/) (values and null) |
| m2 | Harden JSON storage with malformed-data checks and atomic replacement. | `ReadingLog.Storage.Json/JsonReadingLogRepository.cs` | [Lesson 11](../../../lessons/11_files_streams_and_json/) (files, streams, and JSON) |
| m3 | Implement write operations and richer LINQ queries in `ReadingLogService`. | `ReadingLog.Core/ReadingLogService.cs` | [Lesson 09](../../../lessons/09_linq_and_transformations/) (LINQ), [Lesson 12](../../../lessons/12_async_cancellation_and_concurrency/) (async) |
| m4 | Finish POST endpoints, DTO validation paths, and problem contracts. | `ReadingLog.Api/Program.cs` | [Lesson 14](../../../lessons/14_http_clients_and_minimal_apis/) (HTTP clients and Minimal APIs) |
| m5 | Finish CLI write commands and richer error handling. | `ReadingLog.Cli/CliApplication.cs`, `ReadingLog.Cli/ReadingLogApiClient.cs` | [Lesson 14](../../../lessons/14_http_clients_and_minimal_apis/) (`HttpClient`), [Lesson 05](../../../lessons/05_methods_errors_and_debugging/) (errors) |
| m6 | Run the full shared suite and polish edge cases until everything passes. | `ReadingLog.Cli/CliApplication.cs` and across all projects | [Lesson 10](../../../lessons/10_testing_and_dependency_boundaries/) (testing), [Lesson 15](../../../lessons/15_application_composition/) (composition, projects, and Tasks) |

Search the starter source for `TODO(m1)` through `TODO(m6)` to find the exact
lines that mark each gap.

Reading entries use `DateOnly` calendar dates parsed with the same strict ISO
`yyyy-MM-dd` pattern you practiced in Lesson 07 - see the
[capstone guide](../README.md) for how dates, instants, and durations stay
distinct through this project.

[Lesson 13](../../../lessons/13_sql_and_sqlite/) provides SQL persistence
context. This C#-idiomatic capstone intentionally continues to use its JSON
repository, so SQLite is not a milestone prerequisite.

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
dotnet test --project ../tests/ReadingLog.Tests.csproj -p:CapstoneImplementation=Starter -- --filter-class ReadingLog.Tests.Smoke.StarterSmokeTests
dotnet test --project ../tests/ReadingLog.Tests.csproj -p:CapstoneImplementation=Starter
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
