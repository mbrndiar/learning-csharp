# 🧭 Lesson 09 · LINQ and transformations

## 🎯 Objectives
- Build readable `IEnumerable<T>` pipelines with filtering, projection, ordering, grouping, and aggregation.
- Explain deferred execution versus materialization.
- Recognize when side effects make a query harder to reason about.
- Prefer small named query steps when a single long pipeline becomes hard to read.

## ✅ Prerequisites
Before this lesson, you should be comfortable with:
- classes and records from Lesson 07
- generic collections and delegates from Lesson 08
- reading methods that return `IEnumerable<T>`

## 🧠 Causal mental model
LINQ treats a sequence like a stream of values moving through stages.

- `Where` filters values out.
- `Select` transforms each value.
- `OrderBy` changes sequence order.
- `GroupBy` creates buckets.
- `Sum`, `Count`, and `Average` aggregate many values into one answer.

Most LINQ operators are **deferred**: building the query does not run it yet. The work happens when you enumerate it with `foreach`, `ToArray()`, `ToList()`, `Count()`, and similar operations. Materialization is when you intentionally take a snapshot.

## 🔤 Authentic minimal fragments
A small readable query:

```csharp
var names = runs
    .Where(run => run.Completed)
    .OrderBy(run => run.Learner)
    .Select(run => run.Learner);
```

A grouping projection:

```csharp
var summaries = runs
    .Where(run => run.Completed)
    .GroupBy(run => run.Track)
    .Select(group => new { Track = group.Key, Count = group.Count() });
```

## ▶️ Demonstration project
The demonstration lives here:
- `lessons/09-linq-and-transformations/ReadingProgressReport/ReadingProgressReport.csproj`

It turns a list of in-memory study sessions into several reports.

### Commands from the repository root
Build the demonstration:

```bash
dotnet build lessons/09-linq-and-transformations/ReadingProgressReport/ReadingProgressReport.csproj
```

Run it:

```bash
dotnet run --project lessons/09-linq-and-transformations/ReadingProgressReport/ReadingProgressReport.csproj
```

### Expected output

```text
Completed learners: Ada, Grace, Linus
Backend sessions: 2
Track summaries:
- Backend: 2 completed, average score 94.0
- Web: 1 completed, average score 88.0
Total completed minutes: 165
```

## 👀 What to notice
- The pipeline that returns learner names is deferred until it is enumerated.
- The grouped summary materializes into an array because the report is meant to be a stable snapshot.
- The code does not mutate the source list inside the query.
- Breaking the report into separate methods keeps each transformation readable.

## 🧠 Side-effect avoidance
LINQ is easiest to trust when each step is a pure transformation. If your `Select` writes to a file, mutates outside state, or depends on timing, it becomes much harder to debug. A simple rule: use LINQ for describing data flow, and keep side effects at the edges before or after the query.

## 🧠 A beginner cost model for sequences
Correctness is not the only question worth asking about a pipeline; a small,
practical cost model helps you notice expensive habits early:

- **Repeated enumeration repeats work.** A deferred query re-runs its
  filtering and projection steps every time you enumerate it, so enumerating
  the same `IEnumerable<T>` in a loop redoes the work each time instead of
  reusing a previous result.
- **Materializing allocates a snapshot.** `ToArray()`, `ToList()`, and similar
  calls copy every matching element into new memory. That is the right choice
  when you need a stable snapshot, but it is not free.
- **A list scan is linear.** Finding an item in a `List<T>` by value or
  predicate looks at up to every element - the time grows with the list size.
- **Dictionaries and sets exist for lookup.** When you repeatedly ask "does
  this key exist?" or "give me the value for this key," a `Dictionary<TKey,
  TValue>` or `HashSet<T>` is the usual choice specifically because it avoids
  a linear scan for that question.

This lesson intentionally does not teach formal algorithm analysis (Big O
notation, amortized cost proofs, or complexity classes). The goal here is
just to ask "how many times will this run, and does it allocate a new
collection?" at the decision points where you write a pipeline.

## 🧩 Experiment
Try one change at a time:
1. Add another completed backend run and predict which outputs change.
2. Add an incomplete run and see which queries ignore it.
3. Replace `ToArray()` in a materialized result with a deferred return and observe the difference after mutating the source list.
4. Split a long pipeline into two named variables and compare readability.
5. Enumerate the same deferred query twice inside a loop and count how many times a `Where` predicate with a `Console.WriteLine` side effect actually runs. Then materialize it once with `ToArray()` and count again.

## ⚠️ Common mistakes and diagnosis
- **Mistake:** assuming a query runs as soon as you assign it to a variable.
  - **Diagnosis:** source changes still affect the result later because the query is deferred.
- **Mistake:** calling `ToList()` everywhere by habit.
  - **Diagnosis:** you lose laziness and may allocate work you did not need yet.
- **Mistake:** hiding side effects inside `Select` or `Where`.
  - **Diagnosis:** repeating enumeration changes behavior unexpectedly.
- **Mistake:** squeezing too much logic into one pipeline.
  - **Diagnosis:** the query becomes hard to explain out loud.
- **Mistake:** forgetting null argument checks in public query helpers.
  - **Diagnosis:** failures show up later and farther away from the real cause.
- **Mistake:** scanning a `List<T>` repeatedly for lookups that happen often.
  - **Diagnosis:** each scan costs time proportional to the list size; a `Dictionary<TKey, TValue>` or `HashSet<T>` answers the same question without rescanning everything.

## 🧪 Practice contract
Implement `CourseRunReports` in `LinqTransformationsPractice`.

### Required types
- `CourseRun` record with `Learner`, `Track`, `Score`, `Minutes`, and `Completed`
- `TrackSummary` record with `Track`, `CompletedCount`, and `AverageScore`
- `CourseRunReports` static class

### Required behavior
- `GetCompletedLearners(runs)` returns a deferred sequence of completed learner names ordered alphabetically
- `GetTopRunsForTrack(runs, track, count)` returns a deferred sequence for the named track, ordered by score descending then minutes ascending, and returns an empty sequence when `count <= 0`
- `BuildTrackSummaries(runs)` materializes summaries for completed runs grouped by track and ordered by track name
- `TotalCompletedMinutes(runs)` aggregates completed minutes into one integer

### Constraints
- reject `null` sequences and blank track names
- keep the source sequence unchanged
- use readable pipelines rather than manual side effects
- keep summary output deterministic

### Edge cases to handle
- zero requested items in `GetTopRunsForTrack`
- tracks with no completed runs
- source lists that change after a deferred query is created
- materialized summaries that should not change after later source mutations

### Feedback commands
Build the starter project:

```bash
dotnet build exercises/09-linq-and-transformations/starter/LinqTransformationsPractice.csproj
```

Build the reference solution:

```bash
dotnet build exercises/09-linq-and-transformations/solution/LinqTransformationsPractice.csproj
```

Run the tests against the starter implementation while you work:

```bash
dotnet test --project exercises/09-linq-and-transformations/tests/LinqTransformationsPractice.Tests.csproj -p:CourseImplementation=Starter
```

Run the tests against the finished solution:

```bash
dotnet test --project exercises/09-linq-and-transformations/tests/LinqTransformationsPractice.Tests.csproj
```

## 📝 Summary
LINQ lets you describe sequence transformations as a pipeline. The key habits are knowing which operators are deferred, materializing only when you need a snapshot, and keeping side effects outside the query so the pipeline stays readable and predictable.

## ❓ Review questions
1. What is deferred execution, and when does a deferred query actually run?
2. Why might you intentionally materialize a query result?
3. What is the difference between filtering and projection?
4. How can side effects make a LINQ query misleading?
5. When should you split a long pipeline into named steps?
6. Why does enumerating the same deferred query twice repeat its work, and what does materializing it once change?
7. Why would you reach for a `Dictionary<TKey, TValue>` instead of scanning a `List<T>` for repeated lookups?

## 📚 Microsoft Learn links
- https://learn.microsoft.com/dotnet/csharp/linq/
- https://learn.microsoft.com/dotnet/csharp/linq/get-started/introduction-to-linq-queries
- https://learn.microsoft.com/dotnet/csharp/linq/standard-query-operators/filtering-data
- https://learn.microsoft.com/dotnet/csharp/linq/standard-query-operators/projection-operations
- https://learn.microsoft.com/dotnet/csharp/linq/standard-query-operators/grouping-data
- https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2
