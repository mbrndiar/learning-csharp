# 🧭 Lesson 01 · First program

## 🎯 Measurable objectives
By the end of this lesson, you can:
- point at the top-level statements in a `.cs` file and explain that they are the first code C# runs;
- run a file-based C# program from the repository root with `dotnet path/to/file.cs`;
- predict the exact text that a small program will write to standard output;
- make one small change, run again, and describe what changed on screen.

## ✅ Explicit prerequisites
- A working .NET 10 SDK installation.
- A terminal opened at the repository root.
- No prior C# knowledge is required.

## 🧠 Plain-language causal mental model
Think of a C# source file as a recipe. `dotnet` reads the recipe, turns it into a runnable program, and then the program sends text to **standard output** (the terminal). Top-level statements are simply the first recipe steps. If you change one printed line, the observable output changes because the computer follows the updated steps exactly.

A tiny authentic example:

```csharp
Console.WriteLine("Hello, learner!");
Console.WriteLine("I am running from a single .cs file.");
```

Each `Console.WriteLine` call causes one line of visible output.

## ▶️ Demonstrations
- `01_hello_first_program.cs`
- `02_predict_run_modify.cs`

## ▶️ Exact run commands from repository root
```bash
dotnet lessons/01_first_program/01_hello_first_program.cs
dotnet lessons/01_first_program/02_predict_run_modify.cs
dotnet build exercises/01_first_program/starter/FirstProgram.csproj
dotnet test --project exercises/01_first_program/tests/FirstProgram.Tests.csproj
dotnet test --project exercises/01_first_program/tests/FirstProgram.Tests.csproj -p:CourseImplementation=Starter
```

## 👀 Expected observable behavior
Running `01_hello_first_program.cs` prints exactly:

```text
Hello from C#!
This program lives in one file.
Edit a line, run again, and compare the output.
```

Running `02_predict_run_modify.cs` prints three numbered lines. If you edit one string literal and rerun, only that line changes.

## 🧩 Learner experiment
1. Run `dotnet lessons/01_first_program/02_predict_run_modify.cs`.
2. Before changing anything, write down what you expect line 2 to say.
3. Change line 2 in the file.
4. Run the same command again.
5. Explain in one sentence why the terminal output changed.

## ⚠️ Common mistakes with diagnosis
- **Mistake:** Running `dotnet hello-first-program.cs` from the wrong folder.
  **Diagnosis:** `dotnet` cannot find the file. Use the full repository-relative path shown above.
- **Mistake:** Forgetting the semicolon after `Console.WriteLine(...)`.
  **Diagnosis:** The build fails before the program runs, because C# cannot parse the statement boundary.
- **Mistake:** Editing the starter project but running tests without `-p:CourseImplementation=Starter`.
  **Diagnosis:** You keep testing the solution project by default, so your changes appear to have no effect.

## 🧪 Practice contract
Implement `FirstProgramExercise.BuildCelebrationMessage(string learnerName)`.

- **Input:** `learnerName`
- **Output:** one string containing exactly three lines
- **Constraints:** trim the learner name; reject `null`, empty, or whitespace-only names
- **Edge cases:** a one-letter name like `A` is valid; surrounding spaces should not appear in the final output

## 🔁 Feedback instructions
- Build your starter project first: `dotnet build exercises/01_first_program/starter/FirstProgram.csproj`
- Then run the shared tests against your work: `dotnet test --project exercises/01_first_program/tests/FirstProgram.Tests.csproj -p:CourseImplementation=Starter`
- If a test fails, read the test name first, then compare the expected line breaks and exact text.
- When you want a reference answer, inspect the solution project or run the default solution tests.

## 📝 Concise summary
A first C# program is just source text that `dotnet` builds and runs. Top-level statements execute in order, and `Console.WriteLine` makes observable terminal output.

## ❓ Review questions
1. What does `dotnet lessons/01_first_program/01_hello_first_program.cs` do?
2. Why does changing one string literal change the terminal output?
3. What is standard output in plain language?
4. What does “top-level statements” mean?
5. Why might a program fail before printing anything?

## 📚 Authoritative Microsoft Learn references
- <https://learn.microsoft.com/dotnet/csharp/fundamentals/program-structure/top-level-statements>
- <https://learn.microsoft.com/dotnet/api/system.console.writeline>
- <https://learn.microsoft.com/dotnet/core/tools/dotnet>
