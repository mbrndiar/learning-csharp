# ⏱️ Exercise 12 · Async, cancellation, and concurrency

## 🎯 Goal

Make `AsyncWorkSchedulerPractice` load a work plan asynchronously and run its
items with bounded concurrency, correct cancellation, and correct
result/exception handling.

## 🧩 Your task

### `WorkPlanLoader` (`AsyncWorkSchedulerPractice/WorkPlanLoader.cs`)

- **`LoadAsync(path, cancellationToken)`**
  - Reject a blank `path`.
  - Read the file with async I/O and honor `cancellationToken` while
    reading and deserializing.
  - Return the deserialized `WorkItem` list for valid JSON.
  - Surface malformed or missing JSON content as `InvalidDataException`.

### `WorkCoordinator` (`AsyncWorkSchedulerPractice/WorkCoordinator.cs`)

- **`RunAsync(items, maxConcurrency, processAsync, cancellationToken)`**
  - Reject `null` `items` or `processAsync`.
  - Reject a non-positive `maxConcurrency` with
    `ArgumentOutOfRangeException`.
  - Never let more than `maxConcurrency` items run at the same time.
  - Await every owned task; never abandon started work.
  - Aggregate completed values, completion order, and started/completed
    counts into the returned `WorkSummary` without a race between items
    completing concurrently.
  - Let a processor exception propagate to the caller unchanged.
  - Honor `cancellationToken`: a canceled run must surface as
    `OperationCanceledException` (or a derived type).

## ✅ Done when

- All tests in `AsyncWorkSchedulerPractice.Tests` pass against your starter
  implementation.
- Running with `maxConcurrency: 2` never lets more than two processor calls
  run at once, even when items complete out of order.
- A processor exception and a cancellation each surface to the caller
  instead of being swallowed.

## 🔗 Related lesson

[Lesson 12 · Async, cancellation, and concurrency](../../lessons/12-async-cancellation-and-concurrency/README.md)

## ▶️ Build, test, and watch

Build the starter first:

```bash
dotnet build exercises/12-async-cancellation-and-concurrency/starter/AsyncWorkSchedulerPractice/AsyncWorkSchedulerPractice.csproj
```

Run the shared tests against your starter implementation (the default):

```bash
dotnet test --project exercises/12-async-cancellation-and-concurrency/tests/AsyncWorkSchedulerPractice.Tests/AsyncWorkSchedulerPractice.Tests.csproj
```

Get continuous feedback while you edit:

```bash
dotnet watch test --project exercises/12-async-cancellation-and-concurrency/tests/AsyncWorkSchedulerPractice.Tests/AsyncWorkSchedulerPractice.Tests.csproj
```

## 🆚 Compare with the solution

After a genuine attempt, run the same tests against the reference solution:

```bash
dotnet test --project exercises/12-async-cancellation-and-concurrency/tests/AsyncWorkSchedulerPractice.Tests/AsyncWorkSchedulerPractice.Tests.csproj -p:CourseImplementation=Solution
```
