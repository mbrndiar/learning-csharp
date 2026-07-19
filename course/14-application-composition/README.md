# Unit 14 - Application Composition

## Objectives

By the end of this unit you will be able to:

- separate domain rules, application coordination, and I/O code;
- identify the composition root of a CLI application;
- send normal output to stdout and errors to stderr intentionally;
- return meaningful process exit codes;
- keep configuration at the boundary instead of leaking it into domain logic;
- explain the difference between `dotnet build`, `dotnet test`, and `dotnet publish` artifacts;
- run a full quality loop before capstone work.

## Prerequisites

Finish Units 11-13 first.
You should already be comfortable with files, JSON, async code, tests, and dependency injection ideas.

## Causal mental model

A real application is easier to change when responsibilities stay separate:

- **Domain** - rules and calculations that describe the problem.
- **Application** - orchestration: which steps happen, in which order.
- **I/O / infrastructure** - files, console, configuration, HTTP, databases.
- **Composition root** - the one place that wires the concrete pieces together.

If the domain knows about file paths or `Console.WriteLine`, the boundaries are leaking.
If the composition root is tiny, the rest of the code stays easier to test.

## Authentic fragments

A domain-only calculation:

```csharp
ReadingSummary summary = ReadingSummaryCalculator.Create(entries, minimumRating);
```

Application orchestration through an abstraction:

```csharp
IReadOnlyList<ReadingEntry> entries = await source.LoadAsync(configuration.DataFile, cancellationToken);
return Format(summary);
```

CLI boundary with stdout, stderr, and exit codes:

```csharp
await stderr.WriteLineAsync("Usage: summary <config-path>");
return 2;
```

Composition root:

```csharp
var command = new SummaryCommand(new SummaryApplication(new JsonReadingLogSource()));
return await command.RunAsync(args, Console.Out, Console.Error);
```

## Sample project

Run the sample from the repository root:

```bash
dotnet run --project course/14-application-composition/Samples/ReadingLogCliSample/ReadingLogCliSample.csproj
```

Expected behavior:

- writes sample config and data files under the output folder;
- composes the app from a file source plus an application service;
- prints a reading summary to stdout;
- returns exit code `0` for success.

## Practice contract

Default solution tests:

```bash
dotnet test --project course/14-application-composition/Practice/Tests/ReadingLogComposition.Tests/ReadingLogComposition.Tests.csproj
```

Starter feedback:

```bash
dotnet test --project course/14-application-composition/Practice/Tests/ReadingLogComposition.Tests/ReadingLogComposition.Tests.csproj -p:CourseImplementation=Starter
```

Run the composed CLI manually:

```bash
dotnet run --project course/14-application-composition/Practice/Solution/ReadingLog.Cli/ReadingLog.Cli.csproj -- summary course/14-application-composition/Practice/Tests/ReadingLogComposition.Tests/TestData/sample-config.json
```

Publish the CLI from the repository root:

```bash
dotnet publish course/14-application-composition/Practice/Solution/ReadingLog.Cli/ReadingLog.Cli.csproj -c Release -o course/14-application-composition/Practice/Solution/publish
```

Your implementation must make these statements true:

1. Domain code can compute a reading summary without touching files or the console.
2. Application code depends on `IReadingLogSource`, not concrete file APIs.
3. The CLI loads configuration from the boundary and resolves relative data paths.
4. Success output goes to stdout and failures go to stderr.
5. Usage/configuration errors return exit code `2`.
6. Malformed data returns exit code `3`.
7. The composition root is small and easy to inspect.

Deterministic feedback:

- if boundaries leak, the layering tests fail;
- if exit-code behavior is wrong, the CLI tests fail;
- if malformed JSON is swallowed, the invalid-data test fails.

## Experiment

1. Change `minimumRating` in the config file and re-run the CLI.
2. Break the JSON data file and observe stderr plus exit code `3`.
3. Move the data file beside a different config file and confirm relative path resolution still works.
4. Run `dotnet publish` and compare the publish folder with a normal build output.

## Common mistakes and diagnosis

- **Mistake:** putting file I/O inside the domain layer.
  **Diagnosis:** pure calculations become hard to test without the filesystem.

- **Mistake:** printing all messages to stdout.
  **Diagnosis:** scripts cannot distinguish success output from failures.

- **Mistake:** returning `0` even when input is invalid.
  **Diagnosis:** automation treats failure as success.

- **Mistake:** letting configuration objects spread everywhere.
  **Diagnosis:** low-level settings leak into places that only need business concepts.

## Full quality loop and capstone preparation

Before you start the capstone, repeat this loop intentionally:

```bash
dotnet build course/14-application-composition/Practice/Solution/ReadingLog.Cli/ReadingLog.Cli.csproj
dotnet test --project course/14-application-composition/Practice/Tests/ReadingLogComposition.Tests/ReadingLogComposition.Tests.csproj
dotnet publish course/14-application-composition/Practice/Solution/ReadingLog.Cli/ReadingLog.Cli.csproj -c Release -o course/14-application-composition/Practice/Solution/publish
```

That loop proves your code compiles, behaves correctly, and produces deployable artifacts.

## Summary

Composition is the act of wiring pure logic to messy reality at the edge.
A clean composition root keeps the domain pure, the application focused, and the CLI predictable.

## Review questions

1. What belongs in the domain layer versus the application layer?
2. Why should the composition root be small?
3. Why do stderr and exit codes matter for automation?
4. What makes configuration a boundary concern?
5. What extra confidence does `dotnet publish` give compared with only `dotnet build`?

## Official Microsoft Learn links

- [Dependency injection in .NET](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
- [Standard input, output, and error streams](https://learn.microsoft.com/dotnet/api/system.console.error)
- [Create .NET CLI tools and console apps](https://learn.microsoft.com/dotnet/core/tools/)
- [Publish .NET apps with the CLI](https://learn.microsoft.com/dotnet/core/deploying/deploy-with-cli)
