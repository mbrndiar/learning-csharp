# 🧪 Exercise 01 · First program

## 🎯 Goal
Implement `FirstProgramExercise.BuildCelebrationMessage` so a learner name
turns into the exact three-line celebration message the shared tests expect,
while blank names are rejected.

## 📌 Your task

### `FirstProgramExercise.BuildCelebrationMessage(string learnerName)`
- **Input:** `learnerName`, which may be `null`, empty, whitespace-only, or
  padded with leading/trailing spaces.
- **Validation:** throw `ArgumentException` when `learnerName` is `null`,
  empty, or made up only of whitespace.
- **Output:** one string containing exactly these three lines, joined with
  `Environment.NewLine`:
  1. `Hello, <trimmed name>!`
  2. `You have a working C# program.`
  3. `Change one line, run again, and observe the difference.`
- **Edge cases:** a one-letter name such as `A` is valid; leading/trailing
  spaces around the name must never appear in the returned text.

## ✅ Done when
- `dotnet build exercises/01-first-program/starter/FirstProgram.csproj`
  succeeds.
- `dotnet test --project exercises/01-first-program/tests/FirstProgram.Tests.csproj`
  passes every test with no `-p:CourseImplementation` property.

## 📖 Matching lesson
Read [Lesson 01 · First program](../../lessons/01-first-program/README.md)
first, especially its Practice contract section, before you start coding.

## ▶️ Build, test, and watch (starter first)
```bash
dotnet build exercises/01-first-program/starter/FirstProgram.csproj
dotnet test --project exercises/01-first-program/tests/FirstProgram.Tests.csproj
dotnet watch test --project exercises/01-first-program/tests/FirstProgram.Tests.csproj
```
`dotnet watch` reruns the shared tests every time you save the starter file,
so keep it running while you iterate. Stop it with `Ctrl+C` when you are done.

## 🔍 Comparing with the solution
Only after a genuine attempt, compare your work against the reference
implementation by passing an explicit `-p:CourseImplementation=Solution`, or
read `solution/FirstProgram.cs` directly:
```bash
dotnet test --project exercises/01-first-program/tests/FirstProgram.Tests.csproj -p:CourseImplementation=Solution
```
