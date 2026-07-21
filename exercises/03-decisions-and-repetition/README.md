# 🧪 Exercise 03 · Decisions and repetition

## 🎯 Goal
Implement the two `ControlFlowPractice` methods so score categorization uses
clear boundaries and countdown building uses correct loop termination.

## 📌 Your task

### `ControlFlowPractice.DescribeScore(int score)`
- **Input:** one integer `score`.
- **Output:** a category string.
- **Validation:** return `"invalid"` when `score` is outside `0` through `100`
  inclusive.
- **Categories:** return `"needs work"` for `0..49`, `"pass"` for `50..69`,
  `"good"` for `70..89`, and `"excellent"` for `90..100`.
- **Edge cases:** `100` is valid and returns `"excellent"`.

### `ControlFlowPractice.BuildCountdown(int start)`
- **Input:** one integer `start`.
- **Validation:** throw `ArgumentOutOfRangeException` when `start` is
  negative.
- **Output:** a single comma-separated string that counts down from `start`
  to `1` and always ends with the exact text `Lift off!`.
- **Edge cases:** `start == 0` produces only the lift-off text with no leading
  numbers; `start == 1` produces exactly one number before lift-off.

## ✅ Done when
- `dotnet build exercises/03-decisions-and-repetition/starter/DecisionsAndRepetition.csproj`
  succeeds.
- `dotnet test --project exercises/03-decisions-and-repetition/tests/DecisionsAndRepetition.Tests.csproj`
  passes every test with no `-p:CourseImplementation` property.

## 📖 Matching lesson
Read [Lesson 03 · Decisions and repetition](../../lessons/03-decisions-and-repetition/README.md)
first, especially its Practice contract section, before you start coding.

## ▶️ Build, test, and watch (starter first)
```bash
dotnet build exercises/03-decisions-and-repetition/starter/DecisionsAndRepetition.csproj
dotnet test --project exercises/03-decisions-and-repetition/tests/DecisionsAndRepetition.Tests.csproj
dotnet watch test --project exercises/03-decisions-and-repetition/tests/DecisionsAndRepetition.Tests.csproj
```
`dotnet watch` reruns the shared tests every time you save the starter file,
so keep it running while you iterate. Stop it with `Ctrl+C` when you are done.

## 🔍 Comparing with the solution
Only after a genuine attempt, compare your work against the reference
implementation by passing an explicit `-p:CourseImplementation=Solution`, or
read `solution/DecisionsAndRepetition.cs` directly:
```bash
dotnet test --project exercises/03-decisions-and-repetition/tests/DecisionsAndRepetition.Tests.csproj -p:CourseImplementation=Solution
```
