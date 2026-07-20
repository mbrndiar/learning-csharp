# 🚀 learning-csharp

A hands-on introduction to modern C# on .NET for people who have never
programmed before. You will predict and run small programs, change them,
complete focused practice, and finish by building a tested book catalog and
reading journal with a command-line client and a local HTTP API.

Jump to the [course map](#-course-map), or start with the audience and setup
requirements below.

## 🧭 Who this course is for

You need basic file and terminal skills, but no programming experience. The
course explains concepts before it relies on them. Follow the lessons in order
unless you can already complete their practice without consulting the solution.

The supported platform is:

- .NET 10 LTS and C# 14;
- Linux, macOS, or Windows;
- the editor-independent `dotnet` command-line interface.

The manual CI workflow is configured to verify all three operating-system
families. Remote execution is deliberately deferred; the complete local gate is
the current delivery evidence. Newer SDKs, older .NET versions, .NET Framework,
and preview language features are outside the tested support boundary.

## 🎯 What you will learn

By the end of the course, you will be able to:

- explain how C# source becomes a running .NET program;
- model values, text, numbers, nullability, collections, and custom types;
- control program flow and decompose behavior into methods and abstractions;
- use records, classes, interfaces, generics, delegates, lambdas, and LINQ;
- read compiler diagnostics, debug failures, and handle expected errors;
- create SDK projects and solutions, manage packages, and use the build tools;
- model relational data and use parameterized, transactional SQLite through
  Microsoft.Data.Sqlite;
- test behavior with xUnit v3 and Microsoft Testing Platform;
- cross the object/JSON/UTF-8/stream/file boundary without confusing the
  representation with the I/O mechanism;
- distinguish calendar dates, UTC instants, and durations with `DateOnly`,
  `DateTimeOffset`, and `TimeSpan`, and recognize `TimeProvider` as the
  testable clock boundary;
- write asynchronous, cancellable I/O and reason about concurrency ownership;
- explain ASP.NET Core middleware mechanics, then build and test Minimal APIs
  plus raw and typed `HttpClient` clients;
- compose a multi-project application and run its complete quality workflow;
- complete the shared Tasks applied project and both staged capstone tracks.

## ▶️ Set up and run your first program

Install the supported SDK and verify it:

```console
dotnet --version
```

The command must report a compatible `10.0.x` SDK. Then, from the repository
root:

```console
dotnet lessons/01-first-program/01-HelloFirstProgram.cs
```

See [the setup guide](docs/SETUP.md) if the command is unavailable or selects
another SDK.

## 🧠 How to study

For every lesson:

1. Read its objectives and prerequisites.
2. Predict a sample's behavior before running it.
3. Run it and compare the result with the guide.
4. Change the suggested value or condition and explain the new result.
5. Complete the matching `exercises/<lesson>/starter` before reading its
   `solution`.
6. Run the supplied tests and use each failure as feedback.
7. Answer the review questions without looking back.

Do not memorize punctuation in isolation. Explain what value or object exists,
who owns it, which operation transforms it, and where an error can cross a
boundary.

Every lesson, project, and capstone guide uses the same emoji section markers
(🎯 objectives, ✅ prerequisites, 🧠 mental model, 🔤 fragments, ▶️
samples/commands, 👀 expected output, 🧩 experiments, ⚠️ mistakes, 🧪
practice, 🔁 feedback, 📝 summary, ❓ review questions, 📚 references) so you
can scan the repository's learning surfaces the same way.

## 🗺️ Course map

| Module | Main outcome |
| --- | --- |
| [01 - First program](lessons/01-first-program/) | Run a file-based C# app and use the predict-run-modify loop. |
| [02 - Values, types, and null](lessons/02-values-types-and-null/) | Choose and inspect values, conversions, text, numbers, and nullable types. |
| [03 - Decisions and repetition](lessons/03-decisions-and-repetition/) | Express branches and repeat work while tracing changing state. |
| [04 - Collections and iteration](lessons/04-collections-and-iteration/) | Store, find, and iterate values with safe boundary handling. |
| [05 - Methods, errors, and debugging](lessons/05-methods-errors-and-debugging/) | Decompose behavior and diagnose or report failures precisely. |
| [06 - Projects, solutions, and builds](lessons/06-projects-solutions-and-builds/) | Move from one file to the SDK project, dependency, and build model. |
| [07 - Modeling data and behavior](lessons/07-modeling-data-and-behavior/) | Design small types with clear invariants and ownership, including calendar dates, UTC instants, and durations. |
| [08 - Abstractions, generics, and delegates](lessons/08-abstractions-generics-and-delegates/) | Reuse and substitute behavior through explicit contracts. |
| [09 - LINQ and transformations](lessons/09-linq-and-transformations/) | Build readable, deliberate data transformation pipelines. |
| [10 - Testing and dependency boundaries](lessons/10-testing-and-dependency-boundaries/) | Write behavioral tests and control volatile dependencies. |
| [11 - Files, streams, and JSON](lessons/11-files-streams-and-json/) | Cross representation and I/O boundaries safely. |
| [12 - Async, cancellation, and concurrency](lessons/12-async-cancellation-and-concurrency/) | Coordinate asynchronous work with explicit ownership and cancellation. |
| [13 - SQL and SQLite](lessons/13-sql-and-sqlite/) | Model relational data and use parameterized, transactional SQLite safely. |
| [14 - HTTP clients and Minimal APIs](lessons/14-http-clients-and-minimal-apis/) | Understand middleware/HTTP mechanics and build Minimal API plus raw/typed clients. |
| [15 - Application composition](lessons/15-application-composition/) | Compose selectable adapters, configuration, diagnostics, and delivery commands. |

After all lessons, complete the required [Tasks applied
project](projects/tasks/), then both [capstone tracks](capstones/).

## 🧪 Practice convention

Lessons and learner work use separate discoverable roles:

```text
lessons/<lesson>/        complete explanation and runnable demonstrations
exercises/<lesson>/
  starter/               code you change
  solution/              one reference approach
  tests/                 shared behavioral feedback
```

Run a lesson's tests against your starter work from the repository root. The
test projects default to the starter, so the shortest command exercises the
code you are changing:

```console
dotnet test --project exercises/04-collections-and-iteration/tests/CollectionsAndIteration.Tests.csproj
```

Check the same tests against the finished reference solution by selecting it
explicitly:

```console
dotnet test --project exercises/04-collections-and-iteration/tests/CollectionsAndIteration.Tests.csproj \
  -p:CourseImplementation=Solution
```

On PowerShell, place the command on one line or use PowerShell's backtick
continuation character instead of `\`.

The untouched starter is expected to have focused failing tests for unfinished
behavior, but it must restore and compile. A solution is an example, not a
requirement to use identical syntax. Compare contracts, edge cases, clarity,
and diagnostics.

## 🔁 Developer feedback loop

Start with the smallest lesson runnable or test connected to your change. Then widen the
feedback from the repository root:

```bash
dotnet restore LearningCSharp.slnx --locked-mode
dotnet restore LearningCSharp.slnx --locked-mode -p:CourseImplementation=Solution
dotnet format LearningCSharp.slnx --verify-no-changes --no-restore
dotnet build LearningCSharp.slnx --configuration Release --no-restore -p:CourseImplementation=Solution
dotnet test --solution LearningCSharp.slnx --configuration Release --no-build -p:CourseImplementation=Solution
dotnet run --project tools/CourseVerifier --configuration Release --no-build -- verify
dotnet run --project tools/CourseVerifier --configuration Release --no-build -- starters
```

The commands have separate jobs:

- the two restores resolve the exact recorded Starter and Solution dependency
  graphs;
- format checks whitespace and configured code-style rules;
- build compiles and runs SDK analyzers with warnings treated as errors;
- test checks observable behavior;
- CourseVerifier runs declared lesson artifacts, checks role integrity and links,
  and verifies every untouched lesson/project/capstone starter.

Each applied destination has an independent 85% branch-coverage gate so mature
code elsewhere cannot hide its gaps:

| Destination | Exact local coverage commands |
| --- | --- |
| Tasks applied project | [`projects/tasks/README.md`](projects/tasks/README.md) |
| Comparative capstone | [`capstones/comparative/README.md`](capstones/comparative/README.md) |
| Idiomatic capstone | [`capstones/idiomatic/README.md`](capstones/idiomatic/README.md) |

The manual GitHub Actions workflow is configured to run the same underlying
validation in a clean three-OS matrix.
See [setup and daily use](docs/SETUP.md) for the first restore and
[troubleshooting](docs/TROUBLESHOOTING.md) for common failures. The
[quality-contract evidence](docs/CONFORMANCE.md) records the verified course
surfaces and current delivery state.

## 🧩 Applied project and capstones

The required [Tasks project](projects/tasks/) first combines the Task domain,
SQLite and versioned Markdown repositories, low-level middleware and Minimal API
servers, raw and typed HttpClient transports, and one CLI contract. Its
seven-project topology separates domain Core, shared HTTP protocol,
server-side infrastructure, leaf server adapters, reusable client logic, and
the CLI process host.

Then complete both [capstones](capstones/):

- `comparative/` implements the frozen cross-language SQLite key/value contract;
- `idiomatic/` evolves Reading Log through C#-specific domain, JSON persistence,
  Minimal API, typed client/CLI, and integration-test patterns.

Neither destination introduces required architecture that has not appeared in
the lessons and Tasks bridge.

## 📝 Course conventions

- Shell commands throughout the lessons use `bash`, including multi-command
  quality-loop sequences; this file's short one-off verification and test
  commands use `console`; C# examples use `csharp`.
- `...` means omitted context only when the text says so. It is not invented
  runnable syntax.
- Examples use English identifiers and official C# naming conventions.
- Numbered lesson/exercise taxonomy uses `NN-kebab-case`; ordered file-based
  lesson apps use `NN-PascalCase.cs`; buildable lesson project directories use
  the same PascalCase identity as their `.csproj`.
- Repository role directories such as `lessons`, `exercises`, `starter`,
  `solution`, and `tests` remain lowercase.
- Nullable reference types and SDK analyzers are enabled.
- Expected exceptions are labeled as observations; accidental crashes are not
  presented as successful examples.
- File and network examples use controlled local paths, UTF-8, finite
  operations, and no credentials.
- Educational comments explain a boundary, trade-off, or surprising behavior;
  they do not narrate obvious syntax.
- Applied project and capstone starter/solution code uses one top-level type per
  matching `.cs` file (apart from entry points, deliberate partial files, and
  private nested implementation details). Lessons may co-locate tiny related
  types when that materially improves the teaching narrative.

## 🔭 Scope and next steps

This course builds a strong C# and general .NET application foundation,
including relational SQLite, HTTP, and layered local applications. It does
not teach desktop or mobile UI, Blazor, MVC controllers, Entity Framework,
authentication, cloud deployment, unsafe code, Native AOT, source generators,
or production operations. The [source guide](docs/SOURCES.md) links to official
documentation for these and for continued language study.

Use [CHEATSHEET.md](CHEATSHEET.md) only as recall material after learning the
underlying concepts.
