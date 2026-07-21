# 🧪 Exercise 05 · Methods, errors, and debugging

## 🎯 Goal
Implement the `ScoreCalculator` methods so validation and averaging logic
live in one place and the two-argument overload reuses it, then build a
category description on top.

## 📌 Your task

### `ScoreCalculator.Average(int first, int second)`
- **Input:** two integer scores.
- **Output:** the fractional average of the two scores.
- **Constraint:** delegate to `Average(int[])` instead of duplicating its
  validation or averaging behavior, so both overloads share one contract.

### `ScoreCalculator.Average(int[] scores)`
- **Input:** `scores`, an array of integers.
- **Validation:**
  - throw `ArgumentNullException` when `scores` is `null`;
  - throw `ArgumentException` (with `ParamName` equal to `"scores"`) when
    `scores` is empty;
  - throw `ArgumentOutOfRangeException` when any score is outside `0`
    through `100` inclusive.
- **Output:** the fractional (not truncated) average of all scores in the
  array.
- **Edge cases:** a single-item array averages to that item's value; boundary
  scores `0` and `100` are valid.

### `ScoreCalculator.DescribeAverage(int[] scores)`
- **Input:** `scores`, an array of integers.
- **Constraint:** reuse `Average(int[])` to compute the average instead of
  recomputing it separately.
- **Output:** return `"needs work"` below `50`, `"pass"` from `50` through
  less than `70`, `"good"` from `70` through less than `90`, and
  `"excellent"` from `90` upward.

## ✅ Done when
- `dotnet build exercises/05-methods-errors-and-debugging/starter/MethodsErrorsAndDebugging.csproj`
  succeeds.
- `dotnet test --project exercises/05-methods-errors-and-debugging/tests/MethodsErrorsAndDebugging.Tests.csproj`
  passes every test with no `-p:CourseImplementation` property.

## 📖 Matching lesson
Read [Lesson 05 · Methods, errors, and debugging](../../lessons/05-methods-errors-and-debugging/README.md)
first, especially its Practice contract section, before you start coding.

## ▶️ Build, test, and watch (starter first)
```bash
dotnet build exercises/05-methods-errors-and-debugging/starter/MethodsErrorsAndDebugging.csproj
dotnet test --project exercises/05-methods-errors-and-debugging/tests/MethodsErrorsAndDebugging.Tests.csproj
dotnet watch test --project exercises/05-methods-errors-and-debugging/tests/MethodsErrorsAndDebugging.Tests.csproj
```
`dotnet watch` reruns the shared tests every time you save the starter file,
so keep it running while you iterate. Stop it with `Ctrl+C` when you are done.

## 🔍 Comparing with the solution
Only after a genuine attempt, compare your work against the reference
implementation by passing an explicit `-p:CourseImplementation=Solution`, or
read `solution/MethodsErrorsAndDebugging.cs` directly:
```bash
dotnet test --project exercises/05-methods-errors-and-debugging/tests/MethodsErrorsAndDebugging.Tests.csproj -p:CourseImplementation=Solution
```
