# Set up the course

## Required tools

- A supported .NET 10 SDK, not only the runtime.
- A terminal.
- Git if you clone instead of downloading an archive.
- Any text editor.

No IDE, container runtime, database, account, API key, or paid service is
required.

## 1. Install .NET 10

Use Microsoft's [.NET 10 download page](https://dotnet.microsoft.com/download/dotnet/10.0)
or your operating system's supported package instructions. Install an SDK.

Verify the installation:

```console
dotnet --version
dotnet --list-sdks
```

This repository's `global.json` accepts stable .NET 10 feature bands beginning
with 10.0.300. It does not accept preview SDKs or silently roll to .NET 11.

## 2. Get the repository

```console
git clone https://github.com/mbrndiar/learning-csharp.git
cd learning-csharp
```

If you downloaded an archive, extract it and open a terminal in the directory
that contains `LearningCSharp.slnx`.

## 3. Confirm SDK selection

```console
dotnet --info
```

Under `.NET SDK`, the version must begin with `10.0.`. The output also shows the
operating system and the `global.json` that selected the SDK.

## 4. Restore dependencies

```console
dotnet restore LearningCSharp.slnx
```

The first restore creates local NuGet caches. Later validation uses
`--locked-mode`, which refuses an unreviewed dependency-graph change:

```console
dotnet restore LearningCSharp.slnx --locked-mode
```

## 5. Run the first lesson

```console
dotnet lessons/01_first_program/01_hello_first_program.cs
```

File-based apps are compiled C# programs. .NET generates temporary project
metadata, builds the source, and runs the result.

## 6. Choose an editor

Any editor works. Common optional choices are:

- Visual Studio Code with C# Dev Kit;
- Visual Studio;
- JetBrains Rider.

The documented commands remain the source of truth so that editor tasks do not
hide required build or test steps.

## Daily workflow

Run the smallest relevant lesson or test first. For example:

```console
dotnet test --project exercises/07_modeling_data_and_behavior/tests/ModelingDataBehaviorPractice.Tests.csproj
```

Then widen the checks:

```console
dotnet format LearningCSharp.slnx
dotnet build LearningCSharp.slnx --configuration Release
dotnet test --solution LearningCSharp.slnx --configuration Release
dotnet run --project tools/CourseVerifier -- verify
dotnet run --project tools/CourseVerifier -- starters
```

`dotnet format` modifies files. The validation workflow uses
`--verify-no-changes` to prove formatting was already applied. Review formatter
changes; formatting is not a behavior test.

Build output is generated under `bin/` and `obj/`. It is ignored by Git and can
be removed safely with:

```console
dotnet clean LearningCSharp.slnx
```

## Run exercise starter feedback

Exercise tests target the reference solution by default. Select the starter:

```console
dotnet test --project exercises/09_linq_and_transformations/tests/LinqTransformationsPractice.Tests.csproj \
  -p:CourseImplementation=Starter
```

An untouched starter restores and compiles, then reports focused failures for
behavior you must implement. Work on one failure at a time.

## Platform notes

- Bash examples use `\` to continue a command. In PowerShell, put the command
  on one line or use a backtick.
- Paths passed to `dotnet` use `/`; .NET accepts them on all supported systems.
- Unix executable bits and shebangs are optional and are not required by the
  course.
- The course uses only local loopback HTTP. Firewall prompts are not expected
  for in-process API tests.

Continue with the [course entry point](../README.md).
