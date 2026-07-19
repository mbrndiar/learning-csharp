# Reading Log Architecture

## High-level design

The capstone uses four small projects with clear boundaries:

```text
ReadingLog.Cli --HTTP--> ReadingLog.Api -----> ReadingLog.Core
                           |
                           +-----> ReadingLog.Storage.Json -----> ReadingLog.Core
```

### Responsibilities

- **`ReadingLog.Core`**
  - domain records (`Book`, `ReadingEntry`)
  - validation
  - repository contract
  - application service/query behavior
- **`ReadingLog.Storage.Json`**
  - JSON file persistence only
  - implements `ReadingLog.Core.IReadingLogRepository`
  - no HTTP knowledge
- **`ReadingLog.Api`**
  - HTTP endpoints, DTO mapping, DI, configuration, logging, problem responses
  - composes the Core service with the JSON repository implementation
- **`ReadingLog.Cli`**
  - argument parsing, `HttpClient`, JSON parsing, console output, exit codes

## Data flow

### Add a book

1. CLI parses `add-book` arguments.
2. CLI sends JSON to `POST /books`.
3. API validates the DTO and maps it to `CreateBookRequest`.
4. `ReadingLogService` validates the domain request.
5. Service loads the current snapshot from `IReadingLogRepository`.
6. Service appends the new `Book` and saves the updated snapshot.
7. JSON repository writes a temporary UTF-8 file and atomically replaces the old file.
8. API returns `201 Created` with the created book.
9. CLI prints a success message.

### Show a book

1. CLI sends `GET /books/{id}`.
2. API asks `ReadingLogService` for details.
3. Service loads the snapshot and uses LINQ to gather related entries and totals.
4. API maps the domain result to response DTO JSON.
5. CLI parses the JSON and prints a friendly summary.

## Storage shape

The JSON repository stores one snapshot document:

```json
{
  "books": [],
  "entries": []
}
```

That keeps persistence simple for beginners:

- one file
- one root object
- no external database
- easy round-trip tests

## Configuration

The API reads:

- `ReadingLog:StorageDirectory`
- `ReadingLog:StorageFileName`

The repository combines those into a controlled path. The file name must stay a simple file name so the app does not write outside the configured directory.

The CLI reads the base URL from:

1. `--base-url <url>`
2. `READING_LOG_API_URL`
3. default loopback URL `http://127.0.0.1:5071/`

## Safe behavior rules

### File safety

- Only read/write inside the configured storage directory.
- Do not accept arbitrary file paths from CLI users or HTTP clients.
- Treat malformed JSON as an explicit error.
- Replace storage atomically instead of partially overwriting the live file.

### Network safety

- The API is intended for `127.0.0.1` loopback use.
- The CLI uses a finite timeout so it does not hang forever.
- Tests use in-process HTTP or fake handlers, not random live ports.

### Input safety

- DTOs and domain objects are validated.
- Missing/blank/invalid values return validation problems instead of corrupting storage.
- Unknown book IDs return a clear not-found response.

## Testing strategy

The shared `Tests/ReadingLog.Tests.csproj` conditionally references either `../Starter/...` or `../Solution/...` through the `CapstoneImplementation` property.

- Default: `Solution`
- Starter smoke: `dotnet test --project Tests/ReadingLog.Tests.csproj -p:CapstoneImplementation=Starter -- --filter-class ReadingLog.Tests.Smoke.StarterSmokeTests`

Test layers:

- domain validation
- service/query behavior
- JSON repository behavior
- API contracts through in-process integration tests
- CLI behavior through fake `HttpMessageHandler`
- representative end-to-end flow through API + CLI together

## Intentionally omitted production concerns

To keep scope appropriate for a first capstone, this architecture does not include:

- authentication/authorization
- public internet hosting hardening
- database storage
- optimistic/pessimistic concurrency control
- caching
- retries/backoff policies
- structured distributed tracing
- secrets managers
- encryption or key rotation
- multi-user collaboration

## Why this architecture fits the course level

It introduces one layer at a time:

1. plain C# models and validation
2. interfaces and dependency injection
3. async JSON file I/O
4. Minimal API request/response flow
5. `HttpClient` and console UX
6. tests that connect the layers

That makes the capstone large enough to feel real, but still small enough to understand end to end.
