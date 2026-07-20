# 🧭 Lesson 12 · Async, cancellation, and concurrency

## 🎯 Objectives

By the end of this lesson you will be able to:

- explain what a `Task` represents;
- use `async` and `await` without guessing;
- choose async file I/O when work waits on the operating system;
- propagate `CancellationToken` through every owned async operation;
- understand how exceptions move through awaited tasks;
- own task lifetimes so no background work is abandoned;
- run bounded parallel work safely;
- protect shared state while several operations finish out of order.

## ✅ Prerequisites

You should already be comfortable with methods, loops, exceptions, collections, and the file/JSON pipeline from Lesson 11.
If `Task` still feels magical, read this lesson slowly and run the demonstration several times.

## 🧠 Causal mental model

A `Task` is a promise for a future result.
`await` says: **pause this method here, let other work happen, then continue when the promise completes**.

Concurrency is not the same as speed.

- **Async I/O** helps when you are waiting for disk or network operations.
- **Parallel work** helps when several independent operations can be in flight together.
- **Bounded** parallelism matters because unlimited work can overload memory, file handles, sockets, or remote services.

Cancellation is a contract, not an interruption spell.
You must pass the token into the operations you own.

## 🔤 Authentic fragments

Async file read:

```csharp
await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
using StreamReader reader = new(stream);
string json = await reader.ReadToEndAsync(cancellationToken);
```

Bounded concurrency with a semaphore:

```csharp
await semaphore.WaitAsync(cancellationToken);
try
{
    int value = await processAsync(item, cancellationToken);
}
finally
{
    semaphore.Release();
}
```

Owning task lifetimes:

```csharp
Task[] tasks = items.Select(item => ProcessOneAsync(item, cancellationToken)).ToArray();
await Task.WhenAll(tasks);
```

Protecting shared state:

```csharp
lock (gate)
{
    completedCount++;
    totalValue += value;
    completionOrder.Add(item.Id);
}
```

## ▶️ Demonstration project

Run the demonstration from the repository root:

```bash
dotnet run --project lessons/12-async-cancellation-and-concurrency/AsyncWorkSchedulerSample/AsyncWorkSchedulerSample.csproj
```

Expected behavior:

- writes a tiny work plan file;
- loads it asynchronously;
- processes several items with a concurrency limit of 2;
- prints the completion order and total value.

## 🧪 Practice contract

Default solution tests:

```bash
dotnet test --project exercises/12-async-cancellation-and-concurrency/tests/AsyncWorkSchedulerPractice.Tests/AsyncWorkSchedulerPractice.Tests.csproj
```

Starter feedback:

```bash
dotnet test --project exercises/12-async-cancellation-and-concurrency/tests/AsyncWorkSchedulerPractice.Tests/AsyncWorkSchedulerPractice.Tests.csproj -p:CourseImplementation=Starter
```

Your implementation must make these statements true:

1. `WorkPlanLoader.LoadAsync` reads a JSON work plan from disk using async I/O.
2. Malformed JSON becomes `InvalidDataException`, not fake success.
3. `WorkCoordinator.RunAsync` validates `maxConcurrency` and awaits all owned tasks.
4. `RunAsync` never exceeds the requested concurrency limit.
5. `RunAsync` aggregates completed values and completion order safely.
6. Processor exceptions flow back to the caller.
7. Cancellation is honored through `CancellationToken`.

Deterministic feedback:

- parsing mistakes fail the loader tests;
- broken cancellation fails the cancellation test;
- lost exceptions fail the exception-flow test;
- missing semaphore logic fails the concurrency-limit test.

## 🧩 Experiment

1. Change `maxConcurrency` from `2` to `1` and compare completion order.
2. Cancel the demonstration after 150 ms and watch the exception flow.
3. Remove the `lock` around shared state and see why race conditions are hard to reason about.
4. Replace `Task.WhenAll` with forgotten tasks and observe how results become incomplete.

## ⚠️ Common mistakes and diagnosis

- **Mistake:** calling an async method without awaiting its returned task.
  **Diagnosis:** work continues after the method already reported success.

- **Mistake:** creating unlimited tasks.
  **Diagnosis:** CPU, memory, or I/O usage spikes and behavior becomes noisy.

- **Mistake:** forgetting to pass the `CancellationToken`.
  **Diagnosis:** cancellation appears to do nothing.

- **Mistake:** catching every exception and continuing.
  **Diagnosis:** real failures disappear and summaries lie.

- **Mistake:** mutating shared lists and counters without coordination.
  **Diagnosis:** counts and order become flaky across runs.

## 📝 Summary

Async code is about honest ownership of future work.
Start only the work you can finish, pass cancellation through it, and await it before returning.

## ❓ Review questions

1. What does a `Task` represent?
2. Why does `await` preserve exception flow better than manual callbacks?
3. When is async file I/O useful?
4. Why is bounded parallelism safer than starting one task per item without limits?
5. What shared state in your code needs protection?

## 📚 Official Microsoft Learn links

- [Asynchronous programming with async and await](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/)
- [Cancel async tasks after a period of time](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/cancel-async-tasks-after-a-period-of-time)
- [Task-based asynchronous pattern (TAP)](https://learn.microsoft.com/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap)
- [Use asynchronous file I/O](https://learn.microsoft.com/dotnet/standard/io/asynchronous-file-i-o)
