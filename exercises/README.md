# 🧪 Exercises

Every lesson has a matching test-driven exercise. Most exercises ask you to
implement production code while supplied tests provide focused feedback. The
testing lesson also asks you to complete tests so you practice choosing normal,
boundary, and failure scenarios yourself.

## 🗂️ Exercise roles

```text
exercises/<lesson>/
├── starter/   # learner-owned implementation
├── solution/  # one complete reference approach
└── tests/     # shared behavioral feedback
```

The untouched starter must restore and compile. Its selected behavioral tests
fail intentionally and name unfinished work; accidental wiring or compile
failures are not accepted feedback. The solution passes the same public
contract. Tests assert observable behavior rather than requiring identical
implementation syntax.

Some exercises (currently Lesson 10) also ask you to complete a
learner-authored test scaffold, such as `OrderServiceLearnerTests.cs`. Those
scaffold files use class/method comments to explain the *shape* of the tests
you must add (for example, one enabled `[Fact]` plus one enabled `[Theory]`
with several `[InlineData]` rows) without prescribing the exact scenario, fake
implementation, or assertions — you choose those yourself.

## 🔁 Working loop

1. Read the matching lesson and exercise contract.
2. Build the starter.
3. Run the shared tests. Each shared test project selects the `Starter`
   implementation by default, so no `-p:CourseImplementation` property is
   needed, for example:

   ```bash
   dotnet test --project exercises/01-first-program/tests/FirstProgram.Tests.csproj
   ```

4. Fix one failing behavior at a time.
5. Add your own boundary checks where the lesson asks for them.
6. Compare with `solution/` only after a genuine attempt, by passing an
   explicit `-p:CourseImplementation=Solution`, for example:

   ```bash
   dotnet test --project exercises/01-first-program/tests/FirstProgram.Tests.csproj -p:CourseImplementation=Solution
   ```

## 🔄 Continuous feedback with `dotnet watch test`

While you edit a starter implementation (or, for Lesson 10, while you write
your own tests), run the shared tests in watch mode instead of re-typing
`dotnet test` after every change:

```bash
dotnet watch test --project exercises/01-first-program/tests/FirstProgram.Tests.csproj
```

`dotnet watch` reruns this command automatically each time it detects a
change in a tracked source file — the test project itself or the referenced
starter project — so you get fresh pass/fail feedback on every save without
leaving the terminal. Because `Starter` is the default, this always exercises
your in-progress work; stop the watcher with `Ctrl+C` when you are done. Add
`-p:CourseImplementation=Solution` only if you deliberately want to watch the
reference solution instead.

## 🗺️ Exercise index

| Exercise | Focus |
| --- | --- |
| [01 · First program](01-first-program/) | Input validation and exact observable output |
| [02 · Values, types, and null](02-values-types-and-null/) | Numeric/null representation and formatting |
| [03 · Decisions and repetition](03-decisions-and-repetition/) | Branches, loops, and termination |
| [04 · Collections and iteration](04-collections-and-iteration/) | Cleaning, counting, lookup, and duplicates |
| [05 · Methods, errors, and debugging](05-methods-errors-and-debugging/) | Method contracts and specific exceptions |
| [06 · Projects, solutions, and builds](06-projects-solutions-and-builds/) | Project/build metadata and deterministic commands |
| [07 · Modeling data and behavior](07-modeling-data-and-behavior/) | Types, invariants, equality, and strict dates |
| [08 · Abstractions, generics, and delegates](08-abstractions-generics-and-delegates/) | Interfaces, constraints, callbacks, and closures |
| [09 · LINQ and transformations](09-linq-and-transformations/) | Deferred pipelines and materialized reports |
| [10 · Testing and dependency boundaries](10-testing-and-dependency-boundaries/) | Production behavior plus learner-written tests |
| [11 · Files, streams, and JSON](11-files-streams-and-json/) | Safe paths, encoding, malformed data, and atomic save |
| [12 · Async, cancellation, and concurrency](12-async-cancellation-and-concurrency/) | Owned tasks, cancellation, and bounded work |
| [13 · SQL and SQLite](13-sql-and-sqlite/) | Schema, parameters, row mapping, and transactions |
| [14 · HTTP clients and Minimal APIs](14-http-clients-and-minimal-apis/) | Middleware/Minimal API and raw/typed client contracts |
| [15 · Application composition](15-application-composition/) | Boundaries, configuration, CLI exits, and adapter wiring |
