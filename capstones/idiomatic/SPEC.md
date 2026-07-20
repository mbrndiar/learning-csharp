# Reading Log Specification

## Product goal

Build a **local-only personal book catalog and reading journal**. A single user can:

- add books to a catalog
- record reading activity for a book
- list books and entries
- fetch book details with summary information
- view a simple overall overview
- interact through either a Minimal API or a CLI

The project must work **offline** and keep data in a **JSON file on disk**.
It is the C#-idiomatic capstone, separate from the shared Tasks applied project
and frozen comparative capstone at
[`../../projects/tasks/`](../../projects/tasks/) and
[`../comparative/`](../comparative/).

[Lesson 13](../../lessons/13_sql_and_sqlite/) supplies SQL persistence
context, while this capstone deliberately requires JSON persistence rather than
SQLite. Its HTTP and composition prerequisites are
[Lesson 14](../../lessons/14_http_clients_and_minimal_apis/) and
[Lesson 15](../../lessons/15_application_composition/).

## Required projects

Both `starter/` and `solution/` must contain the same four projects and matching public namespaces/contracts:

- `ReadingLog.Core`
- `ReadingLog.Storage.Json`
- `ReadingLog.Api`
- `ReadingLog.Cli`

## Functional requirements

### 1. Domain model

`ReadingLog.Core` must expose:

- `Book`
- `ReadingEntry`
- `ReadingLogSnapshot`
- `IReadingLogRepository`
- validation helpers and `DomainValidationException`
- application/query service behavior through `ReadingLogService`

Minimum domain rules:

- `Book.Title` is required and cannot be blank.
- `Book.Author` is required and cannot be blank.
- `PublicationYear` is optional, but when present it must be in a sensible range.
- `Isbn` is optional, but when present it cannot be blank or absurdly long.
- `ReadingEntry.BookId` must not be empty.
- `StartedOn` is required.
- `FinishedOn`, when present, cannot be earlier than `StartedOn`.
- CLI date text must use the invariant ISO calendar format `yyyy-MM-dd`.
- `PagesRead` must be positive.
- `Rating`, when present, must be between 1 and 5.
- `Notes`, when present, must stay within a reasonable maximum length.

### 2. Storage

`ReadingLog.Storage.Json` must implement `IReadingLogRepository` with:

- async UTF-8 JSON load/save via `System.Text.Json`
- a controlled storage path rooted in configured directory + file name
- cancellation token support on load and save
- explicit malformed data errors for invalid JSON or invalid stored objects
- explicit malformed data errors for `null` books or entries inside stored arrays
- atomic replacement when saving

### 3. API

`ReadingLog.Api` must expose a loopback-oriented Minimal API with:

- dependency injection
- configuration for storage directory/file name
- logging for meaningful operations/errors
- DTO mapping and validation
- consistent JSON success responses
- consistent RFC7807-style problem responses

Required endpoints:

- `GET /` – lightweight status
- `GET /overview`
- `GET /books`
- `GET /books/{id}`
- `GET /entries?bookId={guid}`
- `POST /books`
- `POST /entries`

### 4. CLI

`ReadingLog.Cli` must:

- call the API with `HttpClient`
- use a finite timeout
- support cancellation
- accept only an absolute HTTP or HTTPS API base URL
- handle JSON responses explicitly
- print user-facing output to `stdout`
- print errors to `stderr`
- return meaningful exit codes

Required commands:

- `help`
- `list-books`
- `show-book --book-id <guid>`
- `add-book --title <text> --author <text> [--year <int>] [--isbn <text>]`
- `add-entry --book-id <guid> --started-on <yyyy-MM-dd> --pages-read <int> [--finished-on <yyyy-MM-dd>] [--rating <1-5>] [--notes <text>]`

## Acceptance criteria

### Normal flow

| Area | Scenario | Expected result |
| --- | --- | --- |
| Core | Add a valid book | Service returns saved book with generated `Id`. |
| Core | Add a valid reading entry for an existing book | Service persists the entry and keeps the link to the book. |
| Queries | Get book details | Response includes the book, related entries, total pages, finish flag, and average rating when ratings exist. |
| Storage | Save then load snapshot | Loaded snapshot matches saved books and entries. |
| API | `POST /books` with valid JSON | Returns `201 Created`, JSON body, and `Location` header for the created book. |
| API | `GET /books` | Returns `200 OK` and a JSON array. |
| CLI | `list-books` against healthy API | Prints a friendly list and exits with code `0`. |

### Boundary flow

| Area | Scenario | Expected result |
| --- | --- | --- |
| Storage | Storage file does not exist | Repository returns an empty snapshot. |
| Storage | Storage file exists but is zero bytes | Repository returns an empty snapshot. |
| Core | Optional `PublicationYear` omitted | Book is still valid. |
| Core | Optional `Isbn`, `FinishedOn`, `Rating`, `Notes` omitted | Entry/service behavior still works. |
| API | `GET /books/{id}` for an existing book with no entries | Returns empty `entries` list and summary totals of zero. |
| CLI | `show-book` for a valid book with no entries | Prints the book summary and a “no entries yet” message. |

### Failure flow

| Area | Scenario | Expected result |
| --- | --- | --- |
| Core | Blank title or author | Throws `DomainValidationException`. |
| Core | Invalid year, pages, rating, or reversed dates | Throws `DomainValidationException`. |
| Storage | JSON text is malformed | Throws explicit malformed data error. |
| Storage | JSON shape is valid but stored objects fail validation | Throws explicit malformed data error. |
| Storage | A `books` or `entries` array contains `null` | Throws explicit malformed data error. |
| API | `GET /books/{id}` for missing book | Returns `404` problem response. |
| API | `POST /books` or `POST /entries` with invalid DTO values | Returns `400` validation problem response. |
| CLI | API returns non-success status | Writes error to `stderr` and exits with non-zero status. |
| CLI | API returns malformed JSON | Writes error to `stderr` and exits with non-zero status. |
| CLI | Request times out or is cancelled | Exits with dedicated timeout/cancel code. |
| CLI | Base URL is malformed or uses a non-HTTP scheme | Writes a clear error to `stderr` and exits non-zero. |
| CLI | Date input is not exact `yyyy-MM-dd` text | Rejects it before making an HTTP request. |

## Milestone breakdown

### Milestone 1 – Core models and validation

Finish the domain records, request objects, and validation helpers.

Done means:

- valid requests succeed
- invalid requests throw `DomainValidationException`
- snapshot validation can reject invalid stored data

### Milestone 2 – JSON storage

Implement the JSON repository.

Done means:

- missing/empty file works
- round-trip works
- malformed file throws explicit error
- save uses atomic replacement
- cancellation is honored

### Milestone 3 – Application service and queries

Implement `ReadingLogService`.

Done means:

- add/list/get flows work
- LINQ summaries are correct
- adding an entry for unknown book fails clearly

### Milestone 4 – API

Implement the Minimal API.

Done means:

- DI is wired
- DTOs map cleanly to core requests
- success contracts and problem contracts are stable
- loopback usage is documented

### Milestone 5 – CLI

Implement the console client.

Done means:

- commands parse correctly
- API calls use timeout + cancellation
- success and error output are understandable
- exit codes distinguish common failure modes

### Milestone 6 – Tests and polish

Use the shared test project to harden the app.

Done means:

- starter smoke harness passes
- solution full suite passes
- build is warning-free
- formatting succeeds

## Exact validation commands

```bash
dotnet format starter/ReadingLog.Starter.slnx --no-restore
dotnet format solution/ReadingLog.Solution.slnx --no-restore
dotnet format tests/ReadingLog.Tests.csproj --no-restore

dotnet build starter/ReadingLog.Starter.slnx --nologo
dotnet build solution/ReadingLog.Solution.slnx --nologo
dotnet build tests/ReadingLog.Tests.csproj --nologo

dotnet test --project tests/ReadingLog.Tests.csproj -p:CapstoneImplementation=Starter -- --filter-class ReadingLog.Tests.Smoke.StarterSmokeTests
dotnet test --project tests/ReadingLog.Tests.csproj
dotnet test --project tests/ReadingLog.Tests.csproj --results-directory tests/TestResults -- --coverage --coverage-output solution-coverage.cobertura.xml --coverage-output-format cobertura --coverage-settings tests/coverage.runsettings
dotnet run --project ../../tools/CourseVerifier -- coverage capstones/idiomatic/tests 0.85
```

`tests/TestResults` is generated during coverage collection and should not be kept as a long-lived project artifact.
