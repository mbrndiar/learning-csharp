# 🧭 Lesson 15 · Application composition

## 🎯 Objectives

By the end of this lesson you will be able to:

- separate domain rules, application coordination, and I/O code;
- identify the composition root of a CLI application, and make it small;
- let the composition root select between a file adapter and a server/client
  (HTTP) adapter for the same abstraction, without either adapter's
  dependencies leaking into Domain or Application code;
- treat configuration and the process environment as boundary concerns the
  composition root owns, not something Domain/Application code reads;
- send normal output to stdout and errors to stderr intentionally, and
  return distinct, meaningful process exit codes per failure class;
- explain the difference between `dotnet build`, `dotnet test`, and
  `dotnet publish` artifacts;
- run a full quality loop before the required applied project and either
  capstone.

## ✅ Prerequisites

Finish Lessons 1-14 first. You should already be comfortable with files, JSON,
async code, tests, and dependency injection ideas. Lesson 14's Minimal API and
`HttpClient` work is a direct prerequisite here: this lesson's HTTP adapter is
a typed `HttpClient` consumer exactly like the one you built there, just
wired in from the opposite direction (a CLI's composition root instead of
another web app).

## 🧠 Causal mental model

A real application is easier to change when responsibilities stay separate:

- **Domain** - rules and calculations that describe the problem.
- **Application** - orchestration: which steps happen, in which order.
- **I/O / infrastructure** - files, console, configuration, HTTP, databases.
- **Composition root** - the one place that wires the concrete pieces
  together.

If the domain knows about file paths, `HttpClient`, or `Console.WriteLine`,
the boundaries are leaking. If the composition root is tiny, the rest of the
code stays easy to test.

### Composition roots select adapters

This lesson's `IReadingLogSource` abstraction has **two** implementations:
`JsonReadingLogSource` reads a local file; `HttpReadingLogSource` fetches the
same shape of data from an HTTP endpoint using a plain `HttpClient` (Lesson
14's raw/typed-client ideas, reused here as the composition root's own
infrastructure choice rather than a web app's). Domain and Application code
depend only on the interface:

```csharp
public interface IReadingLogSource
{
    Task<IReadOnlyList<ReadingEntry>> LoadAsync(string location, CancellationToken cancellationToken);
}
```

Only `Program.cs` - the composition root - knows which concrete adapter is
active, and it decides based on the process environment:

```csharp
bool useHttpSource = ReadingLogSourceSelector.ShouldUseHttpSource(
    Environment.GetEnvironmentVariable("READINGLOG_SOURCE"));

IReadingLogSource source = useHttpSource
    ? new HttpReadingLogSource(CreateApiHttpClient())
    : new JsonReadingLogSource();
```

Neither `ReadingLog.Domain` nor `ReadingLog.Application` references
`HttpClient`, `System.IO`, or `Environment` anywhere - grep them and confirm
it yourself. That is what "adapter selection without core depending on
ASP.NET/HttpClient types" means in practice: the dependency points inward,
from infrastructure toward the abstraction, never the other way.

### Configuration and environment ownership

Two different boundary concerns are deliberately kept separate here:

- **Configuration** (the JSON config file) owns *domain-shaped* knobs:
  `minimumRating`, and a `dataFile` value whose meaning depends on which
  adapter is active (a path for the file adapter, a resource path for the
  HTTP adapter).
- **The environment** (`READINGLOG_SOURCE`, `READINGLOG_API_BASEURL`) owns
  *which adapter runs at all* and where the HTTP one points. That is an
  infrastructure/deployment decision, not a business rule, so it does not
  belong in a config file that travels with the data.

`ConfigurationLoader` only resolves `dataFile` into an absolute local path
when the file adapter is active; the composition root tells it not to
otherwise, so an HTTP resource path is never mangled through
`Path.Combine`.

### stdout, stderr, and exit codes

```csharp
await stderr.WriteLineAsync("Usage: summary <config-path>");
return 2;
```

Success output goes to stdout; every failure writes to stderr and returns a
**distinct** exit code for its failure class: `2` for usage/configuration
mistakes, `3` for malformed data, and `4` for an HTTP adapter's network/
server failure. Automation (a shell script, a CI job) can react differently
to each without parsing message text.

## 🔤 Authentic fragments

A domain-only calculation:

```csharp
ReadingSummary summary = ReadingSummaryCalculator.Create(entries, minimumRating);
```

Application orchestration through an abstraction:

```csharp
IReadOnlyList<ReadingEntry> entries = await source.LoadAsync(configuration.DataFile, cancellationToken);
return Format(summary);
```

The HTTP adapter - the only place in this lesson that references `HttpClient`:

```csharp
public sealed class HttpReadingLogSource(HttpClient httpClient) : IReadingLogSource
{
    public async Task<IReadOnlyList<ReadingEntry>> LoadAsync(string location, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(location, cancellationToken);
        response.EnsureSuccessStatusCode();
        ReadingEntry[]? entries = await response.Content.ReadFromJsonAsync<ReadingEntry[]>(cancellationToken: cancellationToken);
        return entries ?? throw new InvalidDataException("The reading log service returned an empty response.");
    }
}
```

Composition root:

```csharp
var command = new SummaryCommand(new SummaryApplication(new JsonReadingLogSource()));
return await command.RunAsync(args, Console.Out, Console.Error);
```

## ▶️ Sample project

Run the sample from the repository root:

```bash
dotnet run --project lessons/15-application-composition/ReadingLogCliSample/ReadingLogCliSample.csproj
```

Expected behavior:

- writes sample config and data files under the output folder;
- runs the **file** adapter first and prints a reading summary;
- starts a tiny loopback HTTP server exposing the same entries, runs the
  **HTTP** adapter against it, and prints an identical summary;
- both runs return exit code `0`, proving the composition root swapped
  infrastructure without changing behavior.

## 🧪 Exercise

The matching exercise lives in
[`exercises/15-application-composition/`](../../exercises/15-application-composition/).
Run its tests from the repository root:

```bash
dotnet test --project exercises/15-application-composition/tests/ReadingLogComposition.Tests/ReadingLogComposition.Tests.csproj
dotnet test --project exercises/15-application-composition/tests/ReadingLogComposition.Tests/ReadingLogComposition.Tests.csproj -p:CourseImplementation=Starter
```

Run the composed CLI manually (file adapter, the default):

```bash
dotnet run --project exercises/15-application-composition/solution/ReadingLog.Cli/ReadingLog.Cli.csproj -- summary exercises/15-application-composition/tests/ReadingLogComposition.Tests/TestData/sample-config.json
```

Publish the CLI from the repository root:

```bash
dotnet publish exercises/15-application-composition/solution/ReadingLog.Cli/ReadingLog.Cli.csproj -c Release -o artifacts/lesson15-publish
```

`artifacts/` is listed in `.gitignore`, so publish output never lands inside
tracked source folders no matter which lesson or project you publish.

Your implementation must make these statements true:

1. Domain code can compute a reading summary without touching files, HTTP,
   or the console.
2. Application code depends on `IReadingLogSource`, not concrete file or
   HTTP APIs.
3. The CLI loads configuration from the boundary and resolves relative data
   paths only when the file adapter is active.
4. `HttpReadingLogSource` checks the response status before deserializing,
   exactly like a Lesson 14 client.
5. Success output goes to stdout and failures go to stderr.
6. Usage/configuration errors return exit code `2`.
7. Malformed data returns exit code `3`.
8. An HTTP adapter failure returns exit code `4`.
9. The composition root is small and easy to inspect.

Deterministic feedback:

- if boundaries leak, the layering tests fail;
- if exit-code behavior is wrong, the CLI tests fail;
- if malformed JSON is swallowed, the invalid-data test fails;
- if the HTTP adapter mishandles status codes or empty bodies, its tests
  fail against a fake handler - no real network required.

## 🧩 Experiment

1. Change `minimumRating` in the config file and re-run the CLI.
2. Break the JSON data file and observe stderr plus exit code `3`.
3. Move the data file beside a different config file and confirm relative
   path resolution still works for the file adapter.
4. Set `READINGLOG_SOURCE=http` and `READINGLOG_API_BASEURL` against a
   stopped/unreachable address and observe exit code `4`.
5. Search `ReadingLog.Domain` and `ReadingLog.Application` for the strings
   `HttpClient` and `File.` - finding zero matches is the point.
6. Run `dotnet publish` and compare the publish folder with a normal build
   output.

## ⚠️ Common mistakes and diagnosis

- **Mistake:** putting file or HTTP I/O inside the domain layer.
  **Diagnosis:** pure calculations become hard to test without a filesystem
  or a network.

- **Mistake:** printing all messages to stdout.
  **Diagnosis:** scripts cannot distinguish success output from failures.

- **Mistake:** returning `0` even when input is invalid, or reusing the same
  exit code for every failure class.
  **Diagnosis:** automation cannot tell a usage mistake from malformed data
  from a network outage.

- **Mistake:** letting an adapter-specific value (a URL, a connection
  string) live inside domain-facing configuration.
  **Diagnosis:** switching adapters requires touching business
  configuration, and the domain layer's tests start needing infrastructure
  to run.

- **Mistake:** deciding which adapter to use somewhere other than the
  composition root (for example, inside `SummaryApplication`).
  **Diagnosis:** application code now depends on infrastructure types, and
  the "one small composition root" property is gone.

## 🔁 Full quality loop and what comes next

Before you start the required applied project, repeat the quality loop this
repository's CI workflow runs. The authoritative, exact command sequence is
recorded in `.github/workflows/course.yml`; at a minimum it restores with a
locked dependency graph, verifies formatting, builds in Release, runs every
test, and runs `tools/CourseVerifier`'s checks (`verify`, `starters`,
`external-links`) plus each project's own coverage gate:

```bash
dotnet restore --locked-mode
dotnet format --verify-no-changes --no-restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet run --project tools/CourseVerifier --configuration Release --no-build -- verify
dotnet run --project tools/CourseVerifier --configuration Release --no-build -- starters
dotnet run --project tools/CourseVerifier --configuration Release --no-build -- external-links
```

Each step has a distinct job: locked restore reproduces the exact recorded
dependency graph; `dotnet format` and the Release build catch style and
analyzer regressions; the test run checks every module's behavior at once;
`CourseVerifier verify` checks the course map, declared artifacts, samples,
and local links; `CourseVerifier starters` confirms every untouched starter
still compiles with the expected focused failures; and `external-links`
checks that documentation URLs still resolve.

That loop proves your code compiles, is formatted, behaves correctly, and
satisfies the course's own verification tooling before you rely on any of it
while building further work.

**What comes next:** Lessons 1-15 are the prerequisite-safe path through this
course. The next step is the required [Tasks applied
project](../../projects/tasks/README.md), which reuses exactly this lesson's
ideas at a larger scale - SQLite and Markdown persistence adapters, a
low-level middleware server *and* a Minimal API server, raw *and* typed
`HttpClient` transports, and one CLI composition root selecting between
them - before either final capstone. If you can explain why this lesson's
`Program.cs` is the only file that mentions `HttpClient`, you are ready for
that project's composition root.

## 📝 Summary

Composition is the act of wiring pure logic to messy reality at the edge. A
clean composition root keeps the domain pure, the application focused, and
the CLI predictable - and it is also the only place that should ever decide
which concrete adapter, which configuration source, and which environment
variable are in play.

## ❓ Review questions

1. What belongs in the domain layer versus the application layer?
2. Why should the composition root be small, and what should never leak
   into `IReadingLogSource` consumers?
3. Why does an adapter-selection decision belong in the environment rather
   than in domain-facing configuration?
4. Why do stderr and distinct exit codes matter for automation, and why is
   one generic "failure" exit code not enough?
5. What extra confidence does `dotnet publish` give compared with only
   `dotnet build`?
6. How does this lesson's HTTP adapter reuse Lesson 14's ideas, and what is
   different about where it is wired in?

## 📚 Official Microsoft Learn links

- [Dependency injection in .NET](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
- [Standard input, output, and error streams](https://learn.microsoft.com/dotnet/api/system.console.error)
- [Create .NET CLI tools and console apps](https://learn.microsoft.com/dotnet/core/tools/)
- [Publish .NET apps with the CLI](https://learn.microsoft.com/dotnet/core/deploying/deploy-with-cli)
- [Use `IHttpClientFactory` to implement resilient HTTP requests](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory)
