# 🧩 Exercise 15 · Application composition

## 🎯 Goal

Wire a small CLI (`ReadingLog.Cli`) around a pure domain calculation
(`ReadingLog.Domain`) and an application layer (`ReadingLog.Application`)
that depends only on `IReadingLogSource`, so the composition root can swap a
file adapter for an HTTP adapter without changing observable behavior.

## 🧩 Your task

### `ReadingLog.Domain` — `ReadingSummaryCalculator.cs`

- **`Create(entries, minimumRating)`**
  - Reject a `null` entry sequence and a `minimumRating` outside `1..5`.
  - Validate every entry (non-blank title, positive pages, rating in
    `1..5`) as invalid data.
  - Compute `TotalBooks`, `TotalPages`, the rounded `AverageRating`, and
    `RecommendedTitles` (entries at or above `minimumRating`, ordered by
    rating descending, then by title ordinal/ascending) into a
    `ReadingSummary`.
  - Stay pure: no file, HTTP, or console access.

### `ReadingLog.Application` — `SummaryApplication.cs`

- **`RunAsync(configuration, cancellationToken)`**
  - Reject a `null` configuration and a missing data file.
  - Load entries through the injected `IReadingLogSource` (honoring
    cancellation), run the domain calculation, and shape a `SummaryReport`
    whose `OutputLines` describe the summary for the CLI to print, in this
    exact order and shape: `Total books: <count>`, `Total pages: <pages>`,
    `Average rating: <two decimals>`, and
    `Recommended: <comma-separated titles or (none)>`.
  - Never reference `HttpClient` or the file system directly - only
    `IReadingLogSource`.

### `ReadingLog.Cli` — `ConfigurationLoader.cs`

- **`LoadAsync(configPath, resolveDataFileAsFilePath, cancellationToken)`**
  - Reject a blank `configPath` and a missing config file
    (`ConfigurationException`).
  - Read and deserialize the configuration JSON asynchronously; report
    malformed or empty configuration as `ConfigurationException`.
  - Validate the data file and the minimum rating (`1..5`).
  - Resolve the data file against the config file's own directory only
    when `resolveDataFileAsFilePath` is `true` (the file adapter); leave it
    untouched for the HTTP adapter.

### `ReadingLog.Cli` — `JsonReadingLogSource.cs`

- **`LoadAsync(location, cancellationToken)`**
  - Reject a blank `location` and a missing file.
  - Read and deserialize the JSON array asynchronously; treat malformed or
    empty content as invalid data.

### `ReadingLog.Cli` — `HttpReadingLogSource.cs`

- **`LoadAsync(location, cancellationToken)`**
  - Issue a `GET` request against `location` using the injected
    `HttpClient`.
  - Check the response status before reading; a failed status must surface
    as `HttpRequestException`.
  - Deserialize a JSON array of reading entries from the response body; an
    empty/`null` body must surface as `InvalidDataException`.

### `ReadingLog.Cli` — `SummaryCommand.cs`

- **`RunAsync(args, stdout, stderr, cancellationToken)`**
  - Reject `null` `args`, `stdout`, or `stderr`.
  - Validate the `summary <config-path>` usage; wrong usage writes a usage
    message to `stderr` and returns exit code `2`.
  - Load configuration, run the application, and write each report output
    line to `stdout` on success, returning exit code `0`.
  - Map configuration/usage/missing-file failures to `stderr` and exit code
    `2`.
  - Map malformed-data failures to `stderr` and exit code `3`.
  - Map HTTP adapter failures to `stderr` and exit code `4`.
  - Never write anything to `stdout` on a failing run.

## ✅ Done when

- All tests in `ReadingLogComposition.Tests` pass against your starter
  implementation.
- `ReadingLog.Domain` and `ReadingLog.Application` contain no reference to
  `HttpClient` or `File.`.
- The CLI returns exit codes `0`, `2`, `3`, and `4` exactly for success,
  usage/configuration errors, malformed data, and HTTP adapter failures
  respectively, with success output on `stdout` and failures on `stderr`
  only.

## 🔗 Related lesson

[Lesson 15 · Application composition](../../lessons/15-application-composition/README.md)

## ▶️ Build, test, and watch

Build the starter first:

```bash
dotnet build exercises/15-application-composition/starter/ReadingLog.Cli/ReadingLog.Cli.csproj
```

Run the shared tests against your starter implementation (the default):

```bash
dotnet test --project exercises/15-application-composition/tests/ReadingLogComposition.Tests/ReadingLogComposition.Tests.csproj
```

Get continuous feedback while you edit:

```bash
dotnet watch test --project exercises/15-application-composition/tests/ReadingLogComposition.Tests/ReadingLogComposition.Tests.csproj
```

## 🆚 Compare with the solution

After a genuine attempt, run the same tests against the reference solution:

```bash
dotnet test --project exercises/15-application-composition/tests/ReadingLogComposition.Tests/ReadingLogComposition.Tests.csproj -p:CourseImplementation=Solution
```
