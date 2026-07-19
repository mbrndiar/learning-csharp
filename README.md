# learning-csharp

A hands-on introduction to modern C# on .NET for people who have never
programmed before. You will predict and run small programs, change them,
complete focused practice, and finish by building a tested book catalog and
reading journal with a command-line client and a local HTTP API.

## Who this course is for

You need basic file and terminal skills, but no programming experience. The
course explains concepts before it relies on them. Follow the modules in order
unless you can already complete their practice without consulting the solution.

The supported platform is:

- .NET 10 LTS and C# 14;
- Linux, macOS, or Windows;
- the editor-independent `dotnet` command-line interface.

CI verifies all three operating-system families. Newer SDKs, older .NET
versions, .NET Framework, and preview language features are outside the tested
support boundary.

## What you will learn

By the end of the course, you will be able to:

- explain how C# source becomes a running .NET program;
- model values, text, numbers, nullability, collections, and custom types;
- control program flow and decompose behavior into methods and abstractions;
- use records, classes, interfaces, generics, delegates, lambdas, and LINQ;
- read compiler diagnostics, debug failures, and handle expected errors;
- create SDK projects and solutions, manage packages, and use the build tools;
- test behavior with xUnit v3 and Microsoft Testing Platform;
- cross the object/JSON/UTF-8/stream/file boundary without confusing the
  representation with the I/O mechanism;
- write asynchronous, cancellable I/O and reason about concurrency ownership;
- build and test an ASP.NET Core Minimal API and a safe `HttpClient` client;
- compose a multi-project application and run its complete quality workflow;
- independently build the reading-log capstone from its staged starter.

## Set up and run your first program

Install the supported SDK and verify it:

```console
dotnet --version
```

The command must report a compatible `10.0.x` SDK. Then, from the repository
root:

```console
dotnet course/01-first-program/Samples/hello-first-program.cs
```

See [the setup guide](docs/SETUP.md) if the command is unavailable or selects
another SDK.

## How to study

For every module:

1. Read its objectives and prerequisites.
2. Predict a sample's behavior before running it.
3. Run it and compare the result with the guide.
4. Change the suggested value or condition and explain the new result.
5. Complete `Practice/Starter` before reading `Practice/Solution`.
6. Run the supplied tests and use each failure as feedback.
7. Answer the review questions without looking back.

Do not memorize punctuation in isolation. Explain what value or object exists,
who owns it, which operation transforms it, and where an error can cross a
boundary.

## Course map

| Module | Main outcome |
| --- | --- |
| [01 - First program](course/01-first-program/) | Run a file-based C# app and use the predict-run-modify loop. |
| [02 - Values, types, and null](course/02-values-types-and-null/) | Choose and inspect values, conversions, text, numbers, and nullable types. |
| [03 - Decisions and repetition](course/03-decisions-and-repetition/) | Express branches and repeat work while tracing changing state. |
| [04 - Collections and iteration](course/04-collections-and-iteration/) | Store, find, and iterate values with safe boundary handling. |
| [05 - Methods, errors, and debugging](course/05-methods-errors-and-debugging/) | Decompose behavior and diagnose or report failures precisely. |
| [06 - Projects, solutions, and builds](course/06-projects-solutions-and-builds/) | Move from one file to the SDK project, dependency, and build model. |
| [07 - Modeling data and behavior](course/07-modeling-data-and-behavior/) | Design small types with clear invariants and ownership. |
| [08 - Abstractions, generics, and delegates](course/08-abstractions-generics-and-delegates/) | Reuse and substitute behavior through explicit contracts. |
| [09 - LINQ and transformations](course/09-linq-and-transformations/) | Build readable, deliberate data transformation pipelines. |
| [10 - Testing and dependency boundaries](course/10-testing-and-dependency-boundaries/) | Test normal, boundary, and failure behavior with controlled dependencies. |
| [11 - Files, streams, and JSON](course/11-files-streams-and-json/) | Cross representation and I/O boundaries safely. |
| [12 - Async, cancellation, and concurrency](course/12-async-cancellation-and-concurrency/) | Coordinate asynchronous work with explicit ownership and cancellation. |
| [13 - HTTP clients and Minimal APIs](course/13-http-clients-and-minimal-apis/) | Implement and test a local HTTP/JSON boundary. |
| [14 - Application composition](course/14-application-composition/) | Compose projects, configuration, diagnostics, and delivery commands. |

After all modules, complete the
[personal reading-log capstone](capstone/reading-log/).

## Practice convention

Every module uses the same discoverable roles:

```text
README.md
Samples/
Practice/
  Starter/   code you change
  Solution/  one reference approach
  Tests/     shared behavioral feedback
```

Run a module's solution feedback from the repository root:

```console
dotnet test --project course/04-collections-and-iteration/Practice/Tests/CollectionsAndIteration.Tests.csproj
```

Select your starter explicitly while working:

```console
dotnet test --project course/04-collections-and-iteration/Practice/Tests/CollectionsAndIteration.Tests.csproj \
  -p:CourseImplementation=Starter
```

On PowerShell, place the command on one line or use PowerShell's backtick
continuation character instead of `\`.

The untouched starter is expected to have focused failing tests for unfinished
behavior, but it must restore and compile. A solution is an example, not a
requirement to use identical syntax. Compare contracts, edge cases, clarity,
and diagnostics.

## Developer feedback loop

Start with the smallest sample or test connected to your change. Then widen the
feedback from the repository root:

```console
dotnet restore LearningCSharp.slnx --locked-mode
dotnet format LearningCSharp.slnx --verify-no-changes --no-restore
dotnet build LearningCSharp.slnx --configuration Release --no-restore
dotnet test --solution LearningCSharp.slnx --configuration Release --no-build
dotnet run --project tools/CourseVerifier --configuration Release --no-build -- verify
```

The commands have separate jobs:

- restore resolves the exact recorded dependency graph;
- format checks whitespace and configured code-style rules;
- build compiles and runs SDK analyzers with warnings treated as errors;
- test checks observable behavior;
- CourseVerifier checks the course map, declared artifacts, samples, and links.

The capstone has an independent 85% branch-coverage gate so unrelated course
tests cannot hide its gaps:

```console
dotnet test --project capstone/reading-log/Tests/ReadingLog.Tests.csproj --results-directory capstone/reading-log/Tests/TestResults -- --coverage --coverage-output solution-coverage.cobertura.xml --coverage-output-format cobertura --coverage-settings capstone/reading-log/Tests/coverage.runsettings
dotnet run --project tools/CourseVerifier -- coverage capstone/reading-log/Tests 0.85
```

GitHub Actions runs the same underlying configuration in a clean environment.
See [setup and daily use](docs/SETUP.md) for the first restore and
[troubleshooting](docs/TROUBLESHOOTING.md) for common failures. The
[quality-contract evidence](docs/CONFORMANCE.md) records the verified course
surfaces and current delivery state.

## Capstone

The capstone is a local personal book catalog and reading journal. Its staged
starter grows into:

- a core domain and application service;
- asynchronous JSON persistence;
- an ASP.NET Core Minimal API;
- a command-line `HttpClient` client;
- unit, storage-contract, API-integration, transport, and end-to-end tests.

Start with the [capstone guide](capstone/reading-log/README.md). It introduces
no required architecture that has not appeared in the modules.

## Course conventions

- Terminal examples use `console`; C# examples use `csharp`.
- `...` means omitted context only when the text says so. It is not invented
  runnable syntax.
- Examples use English identifiers and official C# naming conventions.
- Nullable reference types and SDK analyzers are enabled.
- Expected exceptions are labeled as observations; accidental crashes are not
  presented as successful examples.
- File and network examples use controlled local paths, UTF-8, finite
  operations, and no credentials.
- Educational comments explain a boundary, trade-off, or surprising behavior;
  they do not narrate obvious syntax.

## Scope and next steps

This course builds a strong C# and general .NET application foundation. It does
not teach desktop or mobile UI, Blazor, MVC controllers, Entity Framework,
authentication, cloud deployment, unsafe code, Native AOT, source generators,
or production operations. The [source guide](docs/SOURCES.md) links to official
documentation for these and for continued language study.

Use [CHEATSHEET.md](CHEATSHEET.md) only as recall material after learning the
underlying concepts.
