# 🧪 Exercise 02 · Values, types, and null

## 🎯 Goal
Implement `ReadingProgressFormatter.DescribeProgress` so it validates reading
progress inputs and formats one readable status line, correctly handling
missing title and rating values.

## 📌 Your task

### `ReadingProgressFormatter.DescribeProgress(string? title, int pagesRead, int totalPages, double? rating)`
- **Inputs:** nullable `title`, `pagesRead`, `totalPages`, and nullable
  `rating`.
- **Validation:** throw `ArgumentOutOfRangeException` when:
  - `pagesRead` is negative;
  - `totalPages` is negative;
  - `pagesRead` is greater than `totalPages`;
  - `rating` is provided and is outside `0.0` through `5.0` inclusive, or is
    `double.NaN` (`double.IsNaN(rating)` must be checked explicitly — `NaN`
    does not compare as less than or greater than any number).
- **Fallbacks:** use `(untitled)` when `title` is `null` or whitespace-only;
  use `unrated` when `rating` is `null`.
- **Output:** one line in the exact shape
  `<title>: <pagesRead>/<totalPages> pages (<percentage with one decimal>%), <rating with one decimal>★`.
  When the rating is missing, replace the final rating/star with `unrated`.
- **Examples:** `Café Notes: 12/30 pages (40.0%), 4.5★` and
  `(untitled): 0/0 pages (0.0%), unrated`.
- **Edge cases:** `pagesRead == 0`; `totalPages == 0` (percentage must still
  format cleanly instead of dividing by zero); Unicode titles such as
  `Café Notes`; a trimmed title with surrounding whitespace.

## ✅ Done when
- `dotnet build exercises/02-values-types-and-null/starter/ValuesTypesAndNull.csproj`
  succeeds.
- `dotnet test --project exercises/02-values-types-and-null/tests/ValuesTypesAndNull.Tests.csproj`
  passes every test with no `-p:CourseImplementation` property.

## 📖 Matching lesson
Read [Lesson 02 · Values, types, and null](../../lessons/02-values-types-and-null/README.md)
first, especially its Practice contract section, before you start coding.

## ▶️ Build, test, and watch (starter first)
```bash
dotnet build exercises/02-values-types-and-null/starter/ValuesTypesAndNull.csproj
dotnet test --project exercises/02-values-types-and-null/tests/ValuesTypesAndNull.Tests.csproj
dotnet watch test --project exercises/02-values-types-and-null/tests/ValuesTypesAndNull.Tests.csproj
```
`dotnet watch` reruns the shared tests every time you save the starter file,
so keep it running while you iterate. Stop it with `Ctrl+C` when you are done.

## 🔍 Comparing with the solution
Only after a genuine attempt, compare your work against the reference
implementation by passing an explicit `-p:CourseImplementation=Solution`, or
read `solution/ValuesTypesAndNull.cs` directly:
```bash
dotnet test --project exercises/02-values-types-and-null/tests/ValuesTypesAndNull.Tests.csproj -p:CourseImplementation=Solution
```
