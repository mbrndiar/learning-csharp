# 🧪 Exercise 06 · Projects, solutions, and builds

## 🎯 Goal
Implement `BuildLayout`, a static helper that describes deterministic build
metadata (output/intermediate folders, a normalized reference list, a build
summary, and a run command), so the shared tests pass against the starter
project.

## 📎 Related lesson
Read [Lesson 06 · Projects, solutions, and builds](../../lessons/06-projects-solutions-and-builds/README.md)
first, especially its 🧪 Practice contract section, before you start coding.

## 🗂️ Your task
All work happens in `starter/BuildLayout.cs`.

### `GetOutputDirectory(string configuration, string targetFramework)`
- Reject a `null`, empty, or whitespace-only `configuration` or `targetFramework`
  with `ArgumentException`.
- Return the exact deterministic path `bin/<Configuration>/<TargetFramework>/`
  using the trimmed argument values.

### `GetIntermediateDirectory(string configuration, string targetFramework)`
- Same validation as `GetOutputDirectory`.
- Return the exact deterministic path `obj/<Configuration>/<TargetFramework>/`.

### `NormalizeProjectReferences(IEnumerable<string> projectReferences)`
- Reject a `null` sequence with `ArgumentNullException`.
- Do not mutate the caller's sequence.
- Trim each entry, drop blank/whitespace-only entries, remove duplicates
  case-insensitively, and return the remaining values sorted alphabetically.

### `CreateBuildSummary(string projectName, string configuration, string targetFramework, IEnumerable<string> sourceFiles, IEnumerable<string> projectReferences)`
- Reject `null`/blank required strings with `ArgumentException` and `null`
  collections with `ArgumentNullException`.
- Do not mutate either input collection.
- Normalize both `sourceFiles` and `projectReferences` the same way as
  `NormalizeProjectReferences` (trim, drop blanks, de-duplicate
  case-insensitively, sort) before composing the summary.
- Return exactly this line shape, joined with `Environment.NewLine`:

  ```text
  Project: <project name>
  Assembly: <project name>.dll
  Configuration: <configuration>
  TargetFramework: <target framework>
  Sources(<count>): <comma-separated sorted source files>
  ProjectReferences(<count>): <comma-separated sorted references or (none)>
  Output: <output directory>
  Intermediate: <intermediate directory>
  ```

- When there are no project references, the line must read
  `ProjectReferences(0): (none)`.

### `CreateRunCommand(string projectPath, string configuration, bool noBuild)`
- Reject a `null`/blank `projectPath` or `configuration` with `ArgumentException`.
- Return `dotnet run --project <projectPath> --configuration <configuration>`,
  appending ` --no-build` only when `noBuild` is `true`.

### Edge cases the shared tests exercise
- Duplicate references or source files that differ only by casing.
- Blank entries mixed into a reference or source file list.
- No project references at all (must render `(none)`).
- Project paths that already include nested folders.

## 🏁 Done when
- `dotnet test` against `tests/` passes fully with the `starter/` implementation
  selected (the default), with no changes to the tests, method signatures, or
  `NotImplementedException` messages.
- `BuildLayout` still builds and passes the same tests when the reference
  `solution/` project is selected instead.

## ▶️ Feedback commands
Work against the starter first:

```bash
dotnet build exercises/06-projects-solutions-and-builds/starter/ProjectsSolutionsBuildsPractice.csproj
dotnet test --project exercises/06-projects-solutions-and-builds/tests/ProjectsSolutionsBuildsPractice.Tests.csproj
dotnet watch test --project exercises/06-projects-solutions-and-builds/tests/ProjectsSolutionsBuildsPractice.Tests.csproj
```

Compare with the reference solution only after a genuine attempt:

```bash
dotnet build exercises/06-projects-solutions-and-builds/solution/ProjectsSolutionsBuildsPractice.csproj
dotnet test --project exercises/06-projects-solutions-and-builds/tests/ProjectsSolutionsBuildsPractice.Tests.csproj -p:CourseImplementation=Solution
```
