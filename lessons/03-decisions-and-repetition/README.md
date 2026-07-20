# 🧭 Lesson 03 · Decisions and repetition

## 🎯 Measurable objectives
By the end of this lesson, you can:
- explain that Boolean expressions drive choices in a program;
- use `if` and `switch` patterns to choose between multiple outcomes;
- use `for`, `foreach`, and `while` loops to repeat work;
- trace changing state step by step while a loop runs.

## ✅ Explicit prerequisites
- Lessons 01-02.
- Comfort reading simple variables, arithmetic, and printed output.

## 🧠 Plain-language causal mental model
A decision is a fork in the road: the program evaluates a Boolean condition and follows one path or another. A loop is a repeated checkpoint: C# keeps running the same block while the condition still says “continue.” State tracing matters because each pass can change a variable, and that changed value affects the next pass.

Authentic fragment:

```csharp
int current = 3;
while (current > 0)
{
    Console.WriteLine(current);
    current--;
}
Console.WriteLine("Lift off!");
```

The loop stops because `current` changes on each pass. Without that change, the condition would never become false.

## ▶️ Demonstrations
- `01-Decisions.cs`
- `02-Repetition.cs`

## ▶️ Exact run commands from repository root
```bash
dotnet lessons/03-decisions-and-repetition/01-Decisions.cs
dotnet lessons/03-decisions-and-repetition/02-Repetition.cs
dotnet build exercises/03-decisions-and-repetition/starter/DecisionsAndRepetition.csproj
dotnet test --project exercises/03-decisions-and-repetition/tests/DecisionsAndRepetition.Tests.csproj
dotnet test --project exercises/03-decisions-and-repetition/tests/DecisionsAndRepetition.Tests.csproj -p:CourseImplementation=Solution
```

## 👀 Expected observable behavior
`01-Decisions.cs` prints a label for each score. `02-Repetition.cs` prints a countdown and then prints one character at a time from `GO`, showing that loops can repeat over changing state or over each item in a sequence.

## 🧩 Learner experiment
1. Run `dotnet lessons/03-decisions-and-repetition/02-Repetition.cs`.
2. Predict what happens if `current--` becomes `current -= 2`.
3. Make the change and rerun.
4. Restore the original code.
5. Explain how the state change altered the output.

## ⚠️ Common mistakes with diagnosis
- **Mistake:** Writing a condition that never becomes false.
  **Diagnosis:** The loop keeps repeating because the tracked state never moves toward the stop condition.
- **Mistake:** Checking ranges in the wrong order.
  **Diagnosis:** A broad condition captures values before a more specific one gets a chance.
- **Mistake:** Forgetting that `switch` patterns are tested top to bottom.
  **Diagnosis:** The first matching arm wins, even if a later arm also matches.

## 🧪 Practice contract
Implement:
- `ControlFlowPractice.DescribeScore(int score)`
- `ControlFlowPractice.BuildCountdown(int start)`

Contract details:
- **Inputs:** one score or one countdown start value
- **Outputs:** a category string or a comma-separated countdown string
- **Constraints:** scores outside `0` through `100` should return `invalid`; countdown start must be zero or greater
- **Edge cases:** score boundaries `50`, `70`, `90`, `100`; countdown start `0` and `1`
- **Invalid case:** negative countdown start throws `ArgumentOutOfRangeException`

## 🔁 Feedback instructions
- Build the starter: `dotnet build exercises/03-decisions-and-repetition/starter/DecisionsAndRepetition.csproj`
- Test your implementation: `dotnet test --project exercises/03-decisions-and-repetition/tests/DecisionsAndRepetition.Tests.csproj`
- If countdown tests fail, trace the string after each loop pass on paper.
- If score tests fail, read your conditions from top to bottom and check which one matches first.

## 📝 Concise summary
Decisions choose one path based on Boolean results. Repetition keeps running steps until the program reaches a stopping condition or finishes each item.

## ❓ Review questions
1. What makes a loop stop?
2. Why can the order of `switch` arms matter?
3. What is state tracing?
4. How is `while` different from `foreach` in plain language?
5. What result should `DescribeScore(90)` return?

## 📚 Authoritative Microsoft Learn references
- <https://learn.microsoft.com/dotnet/csharp/language-reference/statements/selection-statements>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/operators/patterns>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/statements/iteration-statements>
