# 🧭 Lesson 06 · Projects, solutions, and builds

## 🎯 Objectives
- Explain what an SDK-style project, assembly, namespace, and solution file each do.
- Restore, build, and run a multi-project .NET application from the repository root.
- Read the difference between source code, generated output, and package restore artifacts.
- Add a project reference so one project can use code from another.
- Convert a tiny file-based program idea into a normal multi-file project layout.

## ✅ Prerequisites
Before this lesson, you should already be comfortable with:
- running `dotnet --version`
- variables, methods, conditionals, and collections in C#
- using the terminal from the repository root

## 🧠 Causal mental model
A **project** (`.csproj`) is the build recipe for one compiled output.

1. The .NET SDK reads the project file.
2. `dotnet restore` resolves package dependencies.
3. `dotnet build` compiles all included `.cs` files into an **assembly** like `MyApp.dll`.
4. `dotnet run` builds if needed, then executes that assembly.
5. A **solution file** (`.slnx`) is only an organizer: it points at one or more projects so you can work with a set of projects together.
6. A **project reference** tells one project to build another first and then use its compiled assembly.
7. A **namespace** is a code-level name container. It helps your source files agree on where types live; it is not the same thing as a folder, although matching folders and namespaces is a common habit.

Think of a solution as a playlist, a project as a recipe card, source files as ingredients, and the assembly in `bin/` as the finished dish.

## 🔤 Authentic minimal fragments
A project file can be tiny:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\ProjectWorkbench.Text\ProjectWorkbench.Text.csproj" />
  </ItemGroup>
</Project>
```

A normal code file declares a namespace and uses another project through its reference:

```csharp
using ProjectWorkbench.Text;

namespace ProjectWorkbench.App;

Console.WriteLine(GreetingComposer.Create("Ada"));
```

## ▶️ Demonstration project
The demonstration lives here:
- `lessons/06-projects-solutions-and-builds/ProjectWorkbench/ProjectWorkbench.slnx`

It contains:
- `ProjectWorkbench.App` - a console application
- `ProjectWorkbench.Text` - a class library referenced by the app

### Commands from the repository root
Restore the whole demonstration solution:

```bash
dotnet restore lessons/06-projects-solutions-and-builds/ProjectWorkbench/ProjectWorkbench.slnx
```

Build it:

```bash
dotnet build lessons/06-projects-solutions-and-builds/ProjectWorkbench/ProjectWorkbench.slnx -c Debug
```

Run the app project:

```bash
dotnet run --project lessons/06-projects-solutions-and-builds/ProjectWorkbench/ProjectWorkbench.App/ProjectWorkbench.App.csproj --configuration Debug
```

### Expected output

```text
Project: ProjectWorkbench.App
Own assembly: ProjectWorkbench.App
Referenced assembly: ProjectWorkbench.Text
Message: Hello from a referenced class library, Ada.
Output folder pattern: bin/Debug/net10.0/
```

### What to notice
- `obj/` holds intermediate generated files used during compilation.
- `bin/Debug/net10.0/` holds the runnable output.
- The app project can call `GreetingComposer` only because the project reference exists.
- If you remove the reference, the namespace import still compiles in the file editor, but the build fails because the assembly dependency is gone.

## 🧠 Restore, build, run, and generated output
- `dotnet restore` creates or updates `packages.lock.json` for projects that need packages.
- `dotnet build` produces assemblies in `bin/<Configuration>/<TargetFramework>/`.
- `dotnet run` is convenient while learning, but it still depends on a valid project.
- `Debug` and `Release` are build **configurations**. They are separate output folders.

The file-based demonstrations in Lessons 01-05 deliberately do **not** commit
`packages.lock.json`. They declare no external packages, while their virtual
projects contain SDK-injected runtime packs whose exact patch and runtime
identifier vary by installed SDK and operating system. Normal SDK projects
remain locked; locking those generated file-app details would make an otherwise
portable demonstration restore only on the machine that produced the lock.

## 🔤 NuGet in this lesson
NuGet is the package manager for .NET. In this lesson's offline demonstration, we intentionally avoid external packages so the behavior stays deterministic. The command you will use later looks like this:

The general command form is shown below. `<PACKAGE_ID>` is a placeholder, not a
package to install:

```text
dotnet add lessons/06-projects-solutions-and-builds/ProjectWorkbench/ProjectWorkbench.App/ProjectWorkbench.App.csproj package <PACKAGE_ID>
```

The important idea is: package references come from NuGet, while project references point at code you own in this repository.

## 🧠 Converting a file-based app into a multi-file project
If you started with one experimental file, the usual upgrade path is:
1. create a project folder
2. add a `.csproj`
3. move the entry point into `Program.cs`
4. move reusable logic into named `.cs` files
5. add project references when code belongs in a separate assembly

A tiny before/after idea:

```csharp
// file-based sketch
Console.WriteLine(BuildMessage("Ada"));

static string BuildMessage(string name) => $"Hello, {name}.";
```

```csharp
// multi-file project
using ProjectWorkbench.Text;

Console.WriteLine(GreetingComposer.Create("Ada"));
```

The second version is easier to test, reuse, and extend because the logic has a home outside the entry file.

## 🧩 Experiment
Try one change at a time:
1. Run the demonstration in `Release` instead of `Debug`.
2. Add another method to `GreetingComposer` and call it from `Program.cs`.
3. Open `bin/` and `obj/` after a build and compare what changed.
4. Remove the project reference temporarily and observe the compiler error, then put it back.

## ⚠️ Common mistakes and diagnosis
- **Mistake:** editing files inside `bin/` or `obj/`.
  - **Diagnosis:** your changes disappear after the next build because those folders are generated output.
- **Mistake:** forgetting a project reference.
  - **Diagnosis:** the compiler says the namespace or type cannot be found.
- **Mistake:** confusing namespaces with folders.
  - **Diagnosis:** a file can live in one folder and still declare a different namespace; the compiler trusts the namespace declaration, not the folder name.
- **Mistake:** running `dotnet run` against the wrong project.
  - **Diagnosis:** you get the wrong startup behavior or an error about an output type that is not executable.
- **Mistake:** assuming restore and build are the same step.
  - **Diagnosis:** package-related errors usually happen during restore, while C# syntax/type errors happen during build.

## 🧪 Practice contract
Implement `BuildLayout` in the practice library.

### Required inputs and outputs
- `GetOutputDirectory(configuration, targetFramework)` returns `bin/<Configuration>/<TargetFramework>/`.
- `GetIntermediateDirectory(configuration, targetFramework)` returns `obj/<Configuration>/<TargetFramework>/`.
- `NormalizeProjectReferences(projectReferences)` trims names, removes blanks, removes duplicates case-insensitively, and returns alphabetical results.
- `CreateBuildSummary(projectName, configuration, targetFramework, sourceFiles, projectReferences)` returns this exact line shape:

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

- `CreateRunCommand(projectPath, configuration, noBuild)` returns a root-friendly `dotnet run --project ... --configuration ...` command and appends `--no-build` only when requested.

### Constraints
- Reject `null`, empty, or whitespace-only required strings with `ArgumentException`.
- Reject `null` collections with `ArgumentNullException`.
- Do not mutate the caller's collections.
- Keep the output deterministic.

### Edge cases to handle
- duplicate references with different casing
- blank entries in reference or source file lists
- no project references at all
- project paths that already include nested folders

### Feedback commands
Build the starter implementation:

```bash
dotnet build exercises/06-projects-solutions-and-builds/starter/ProjectsSolutionsBuildsPractice.csproj
```

Build the completed reference solution:

```bash
dotnet build exercises/06-projects-solutions-and-builds/solution/ProjectsSolutionsBuildsPractice.csproj
```

Run the shared tests against the starter while you work:

```bash
dotnet test --project exercises/06-projects-solutions-and-builds/tests/ProjectsSolutionsBuildsPractice.Tests.csproj
```

Run the shared tests against the finished solution:

```bash
dotnet test --project exercises/06-projects-solutions-and-builds/tests/ProjectsSolutionsBuildsPractice.Tests.csproj -p:CourseImplementation=Solution
```

## 📝 Summary
A solution groups projects. A project tells the SDK how to build one assembly. Project references connect your own assemblies, package references connect NuGet packages, and `bin/` plus `obj/` are generated output rather than source. Once you understand that flow, multi-project C# code stops feeling magical and starts feeling mechanical.

## ❓ Review questions
1. What is the difference between a solution file and a project file?
2. Why does a project reference matter even if a `using` statement already exists in the code?
3. What belongs in `bin/` versus `obj/`?
4. When would you choose a project reference instead of a NuGet package?
5. What changes when you switch from `Debug` to `Release`?

## 📚 Microsoft Learn links
- https://learn.microsoft.com/dotnet/core/tools/
- https://learn.microsoft.com/dotnet/core/tools/dotnet-build
- https://learn.microsoft.com/dotnet/core/tools/dotnet-run
- https://learn.microsoft.com/dotnet/core/tools/dotnet-restore
- https://learn.microsoft.com/dotnet/csharp/fundamentals/types/namespaces
