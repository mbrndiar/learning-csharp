# 🧪 Exercise 09 · LINQ and transformations

## 🎯 Goal
Implement four `CourseRunReports` query helpers over `CourseRun` data —
mixing deferred pipelines and materialized snapshots — so the shared tests
pass against the starter project.

## 📎 Related lesson
Read [Lesson 09 · LINQ and transformations](../../lessons/09-linq-and-transformations/README.md)
first, especially its 🧪 Practice contract section, before you start coding.

## 🗂️ Your task
`starter/CourseRun.cs` and `starter/TrackSummary.cs` are already complete
records — they need no changes. All work happens in
`starter/CourseRunReports.cs`.

### `GetCompletedLearners(IEnumerable<CourseRun> runs)`
- Reject a `null` sequence with `ArgumentNullException`.
- Return a deferred sequence: the null check and the filtering/ordering must
  not run until the caller enumerates the result (for example with
  `.ToArray()` or `foreach`).
- The result contains only completed runs' learner names, ordered
  alphabetically.

### `GetTopRunsForTrack(IEnumerable<CourseRun> runs, string track, int count)`
- Reject a `null` sequence and a missing/blank `track`; like above, this
  validation is deferred until enumeration.
- Return a deferred sequence of runs for the given track, ordered by `Score`
  descending then `Minutes` ascending, limited to `count` entries.
- Return an empty sequence when `count <= 0`.

### `BuildTrackSummaries(IEnumerable<CourseRun> runs)`
- Reject a `null` sequence with `ArgumentNullException`.
- Group only completed runs by `Track`.
- Materialize a stable snapshot — a `IReadOnlyList<TrackSummary>` with
  `CompletedCount` and `AverageScore` per track — ordered by track name; the
  snapshot must not change if the source list is mutated afterward.

### `TotalCompletedMinutes(IEnumerable<CourseRun> runs)`
- Reject a `null` sequence with `ArgumentNullException`.
- Sum `Minutes` from only the completed runs, without changing the input
  sequence.

### Edge cases the shared tests exercise
- `count <= 0` passed to `GetTopRunsForTrack`.
- A track with zero completed runs.
- Enumerating a deferred query (`GetCompletedLearners`) after adding to the
  mutable source list used to build it — the addition must show up.
- Confirming a materialized `BuildTrackSummaries` snapshot is unaffected by
  a later mutation of the source list.

## 🏁 Done when
- `dotnet test` against `tests/` passes fully with the `starter/`
  implementation selected (the default), with no changes to the tests,
  member signatures, or exception messages.
- The same tests still pass when the reference `solution/` project is
  selected instead.

## ▶️ Feedback commands
Work against the starter first:

```bash
dotnet build exercises/09-linq-and-transformations/starter/LinqTransformationsPractice.csproj
dotnet test --project exercises/09-linq-and-transformations/tests/LinqTransformationsPractice.Tests.csproj
dotnet watch test --project exercises/09-linq-and-transformations/tests/LinqTransformationsPractice.Tests.csproj
```

Compare with the reference solution only after a genuine attempt:

```bash
dotnet build exercises/09-linq-and-transformations/solution/LinqTransformationsPractice.csproj
dotnet test --project exercises/09-linq-and-transformations/tests/LinqTransformationsPractice.Tests.csproj -p:CourseImplementation=Solution
```
