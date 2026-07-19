# 🏁 Reading Log Capstone

Reading Log is a beginner-friendly C# capstone for a **personal book catalog and reading journal**. It stays **local, offline, and loopback-only**: one JSON file on your computer, one Minimal API listening on `127.0.0.1`, and one CLI that talks to that API with `HttpClient`.

## ✅ Audience

This capstone is written for someone who has completed roughly **14 learning units** and is ready to combine:

- classes and records
- collections and LINQ
- validation and exceptions
- calendar dates, UTC instants, and durations (`DateOnly`, `DateTimeOffset`, `TimeSpan` from Unit 07)
- async file I/O
- `System.Text.Json`
- dependency injection
- Minimal APIs
- `HttpClient`
- automated testing

It intentionally avoids undeclared production topics such as authentication, cloud hosting, databases, message queues, background workers, and ORMs.

## 🗺️ Folder layout

- `Starter/` – milestone scaffolding with explicit `TODO(m1)` to `TODO(m6)` gaps.
- `Solution/` – completed reference implementation for the stated scope.
- `Tests/` – one shared test project that can target either implementation.
- `SPEC.md` – exact requirements and acceptance criteria.
- `ARCHITECTURE.md` – project boundaries, data flow, safety rules, and trade-offs.

## 🏁 Six milestones

1. **Core models and validation** – books, reading entries, snapshot contract, domain rules.
2. **JSON storage** – load/save snapshot asynchronously with controlled path handling.
3. **Application service and queries** – add books, add entries, list books, compute summaries.
4. **Minimal API** – DTO mapping, DI, configuration, logging, and problem responses.
5. **CLI** – call the API safely with `HttpClient`, parse JSON, print results, return exit codes.
6. **Polish and tests** – run the shared test suite, fix bugs, and review edge cases.

## ▶️ Exact commands

From this folder:

```bash
dotnet format Starter/ReadingLog.Starter.slnx --no-restore
dotnet format Solution/ReadingLog.Solution.slnx --no-restore
dotnet format Tests/ReadingLog.Tests.csproj --no-restore

dotnet build Starter/ReadingLog.Starter.slnx --nologo
dotnet build Solution/ReadingLog.Solution.slnx --nologo
dotnet build Tests/ReadingLog.Tests.csproj --configuration Release --nologo

dotnet test --project Tests/ReadingLog.Tests.csproj -p:CapstoneImplementation=Starter -- --filter-class ReadingLog.Tests.Smoke.StarterSmokeTests
dotnet test --project Tests/ReadingLog.Tests.csproj
dotnet test --project Tests/ReadingLog.Tests.csproj --configuration Release --no-build --results-directory Tests/TestResults -- --coverage --coverage-output solution-coverage.cobertura.xml --coverage-output-format cobertura --coverage-settings Tests/coverage.runsettings
dotnet run --project ../../tools/CourseVerifier -- coverage capstone/reading-log/Tests 0.85
```

The coverage test run adds `--configuration Release --no-build` because the
preceding `dotnet build Tests/ReadingLog.Tests.csproj --configuration Release`
command already produced that build; reusing it keeps the coverage
measurement tied to the same binaries CI checks.

`Tests/TestResults` is an ignored generated folder for temporary coverage
artifacts. Delete it with your shell or file manager after inspecting the
report.

Run the completed API and CLI on loopback:

```bash
dotnet run --project Solution/ReadingLog.Api -- --urls http://127.0.0.1:5071
dotnet run --project Solution/ReadingLog.Cli -- list-books
dotnet run --project Solution/ReadingLog.Cli -- add-book --title "Dune" --author "Frank Herbert" --year 1965
dotnet run --project Solution/ReadingLog.Cli -- add-entry --book-id <book-id> --started-on 2026-07-19 --pages-read 45 --rating 5 --notes "Great pacing"
```

`--started-on` and `--finished-on` accept only a strict ISO `yyyy-MM-dd`
calendar date (Unit 07's `DateOnly.TryParseExact` pattern with the invariant
culture) - not a locale-specific date shape. A reading entry's `StartedOn`
and `FinishedOn` are `DateOnly` calendar dates; timestamps such as when the
core service created a record are `DateTimeOffset` UTC instants, and any
elapsed-time calculation is a `TimeSpan`. Units 12 and 13 are where you
practiced the async I/O and HTTP boundaries that this CLI and API reuse.

## 🔒 What “safe” means in this capstone

- The API is meant for **loopback use** (`127.0.0.1`), not public internet exposure.
- Storage is a **single JSON file** in a configured application data folder.
- File paths are **controlled by configuration**, not arbitrary user input.
- One repository instance serializes file loads and saves; cross-process
  multi-writer coordination remains out of scope.
- The CLI uses a **finite timeout** and supports **cancellation**.
- Request bodies and domain objects are **validated before saving**.
- Malformed JSON storage data causes an **explicit error** instead of a silent fallback.

## 🧪 Normal, boundary, and failure examples

- **Normal:** add a book, add a reading entry, list books, fetch book details.
- **Boundary:** empty storage file, no books yet, optional ISBN, optional finish date, optional rating.
- **Failure:** blank title, impossible year, unknown book ID, malformed JSON file, API timeout, cancelled request.

The detailed acceptance criteria live in `SPEC.md`.

## 🚫 Intentionally omitted production concerns

This project does **not** try to solve:

- user accounts or authentication
- cloud sync or sharing
- databases or migrations
- concurrent multi-writer editing
- retries/circuit breakers
- encryption at rest
- analytics/telemetry backends
- rich search indexing

Those are extension ideas, not baseline requirements.

## 🔭 Extension ideas

Small:

- delete a book
- edit notes
- filter by author
- sort by recently finished

Medium:

- export/import JSON backups
- reading goals and yearly totals
- tags or genres
- pagination

Ambitious:

- desktop UI
- mobile companion app that still talks to the local API
- full-text search
- multiple shelves or reading lists
- optional SQLite storage while keeping the same core contracts
