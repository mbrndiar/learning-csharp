# 🧭 Lesson 05 · Methods, errors, and debugging

## 🎯 Measurable objectives
By the end of this lesson, you can:
- explain a method as a named block with parameters, local scope, and a return value;
- call an overloaded method and explain why C# chose that version;
- validate inputs before continuing;
- throw and recognize specific exceptions for invalid data;
- read the idea of a stack trace as “how the program got here.”

## ✅ Explicit prerequisites
- Lessons 01-04.
- Comfort reading arrays, loops, conditions, and formatted output.

## 🧠 Plain-language causal mental model
A method is a named machine: values go in through parameters, work happens inside local scope, and a result comes back through the return value. Validation belongs near the method entrance so bad data stops early. An exception is a structured stop signal. When one is thrown, C# records a stack trace, which is a breadcrumb trail of method calls that led to the failure.

Authentic fragment:

```csharp
static double Average(int[] scores)
{
    if (scores.Length == 0)
    {
        throw new ArgumentException("Provide at least one score.", nameof(scores));
    }

    int total = 0;
    for (int index = 0; index < scores.Length; index++)
    {
        total += scores[index];
    }

    return (double)total / scores.Length;
}
```

The exception changes control flow immediately, which is why validation is so helpful for debugging.

Parameters are a second place where beginners get surprised. C# passes
arguments **by value** by default: the method receives a copy of whatever you
passed in.

```csharp
static void TryDoubleValue(int value)
{
    value *= 2; // only changes this method's local copy
}

static void AppendNote(List<string> notes)
{
    notes.Add("reviewed"); // mutates the same list object the caller sees
}

static void ReplaceList(List<string> notes)
{
    notes = new List<string>(); // reassigns the local parameter only; the caller's list is untouched
}
```

For a reference type such as `List<string>`, the *copy* is a copy of the
reference, not a copy of the object. That is why `AppendNote` can mutate the
caller's list through the shared reference, while `ReplaceList` cannot make
the caller see a different list - reassigning the parameter only changes what
the local copy of the reference points to.

Sometimes a method genuinely needs to hand back more than one value, or needs
to say "this either worked and produced a value, or it did not." That is what
`out` parameters and the Try-pattern are for; you will use this shape again
when you build `TryParseExact`-style parsing later in the course:

```csharp
static bool TryParseScore(string text, out int score) => int.TryParse(text, out score);
```

## ▶️ Demonstrations
- `01-MethodsAndOverloads.cs`
- `02-ExceptionsAndDebugging.cs`

## ▶️ Exact run commands from repository root
```bash
dotnet lessons/05-methods-errors-and-debugging/01-MethodsAndOverloads.cs
dotnet lessons/05-methods-errors-and-debugging/02-ExceptionsAndDebugging.cs
dotnet build exercises/05-methods-errors-and-debugging/starter/MethodsErrorsAndDebugging.csproj
dotnet test --project exercises/05-methods-errors-and-debugging/tests/MethodsErrorsAndDebugging.Tests.csproj
dotnet test --project exercises/05-methods-errors-and-debugging/tests/MethodsErrorsAndDebugging.Tests.csproj -p:CourseImplementation=Solution
```

## 👀 Expected observable behavior
`01-MethodsAndOverloads.cs` prints two averages: one from two separate arguments and one from an array overload, then prints a pass-by-value observation - an `int` passed to a method that reassigns its parameter is unchanged by the caller, a `List<int>` passed to a method that mutates it shows the caller's list updated, and a `List<int>` passed to a method that reassigns its parameter is unchanged by the caller. `02-ExceptionsAndDebugging.cs` catches an exception, prints its type and message, and confirms that the stack trace mentions the validating method.

That demonstration catches `Exception` only to inspect the diagnostic information that
all exceptions share. Application code should catch a specific exception only
when it can handle that failure meaningfully; the broad diagnostic catch is not
the normal production pattern.

## 🧩 Learner experiment
1. Run `dotnet lessons/05-methods-errors-and-debugging/02-ExceptionsAndDebugging.cs`.
2. Predict what happens if you remove the empty-array validation.
3. Read the code first, then make the change temporarily.
4. Run again and compare the exception or output.
5. Restore the validation and explain why the earlier failure was easier to diagnose.
6. In `01-MethodsAndOverloads.cs`, predict whether mutating a `List<int>` parameter inside a method is visible to the caller, and whether reassigning that same parameter is visible too. Run the demonstration and check your prediction.
7. Explain in one sentence why "reference type" and "passed by reference" are not the same idea.

## ⚠️ Common mistakes with diagnosis
- **Mistake:** Returning `total / count` as integers when you wanted a fractional average.
  **Diagnosis:** Integer division loses the fraction before the result becomes `double`.
- **Mistake:** Throwing a very general exception with no clear parameter name.
  **Diagnosis:** The failure becomes harder to diagnose because the signal is vague.
- **Mistake:** Assuming local variables from one method are visible in another.
  **Diagnosis:** Scope rules hide locals outside the method where they were declared.
- **Mistake:** Assuming reassigning a reference-type parameter changes the caller's variable.
  **Diagnosis:** The parameter is a copy of the reference; reassigning it only changes what the local copy points to, not the object the caller still holds.

## 🧪 Practice contract
Implement:
- `ScoreCalculator.Average(int first, int second)`
- `ScoreCalculator.Average(int[] scores)`
- `ScoreCalculator.DescribeAverage(int[] scores)`

Contract details:
- **Inputs:** scores as two integers or an integer array
- **Outputs:** a numeric average or a descriptive category string
- **Constraints:** each score must be between `0` and `100` inclusive
- **Edge cases:** a single-item array, boundary scores `0` and `100`, repeated values
- **Invalid cases:** `null` array, empty array, or out-of-range score values

See [exercises/05-methods-errors-and-debugging/README.md](../../exercises/05-methods-errors-and-debugging/README.md) for the complete task, edge cases, and Starter-first build/test/watch commands.

## 🔁 Feedback instructions
- Build the starter project: `dotnet build exercises/05-methods-errors-and-debugging/starter/MethodsErrorsAndDebugging.csproj`
- Test your implementation: `dotnet test --project exercises/05-methods-errors-and-debugging/tests/MethodsErrorsAndDebugging.Tests.csproj`
- If an exception test fails, compare both the exception type and the parameter name.
- If average tests fail, inspect where integer division happens and whether you cast soon enough.

## 📝 Concise summary
Methods organize code into reusable units. Validation and specific exceptions make bugs easier to find, stack traces show the chain of calls that led to a failure, and understanding pass-by-value for both value types and references explains what a method can and cannot change for its caller.

## ❓ Review questions
1. What is the difference between a parameter and a local variable?
2. Why are overloads useful?
3. Why should validation happen early?
4. What does a stack trace tell you?
5. What exception fits an out-of-range score best?
6. Why can a method mutate the contents of a `List<T>` parameter but not make the caller's variable point at a different list?
7. When would you reach for an `out` parameter instead of returning a single value?

## 📚 Authoritative Microsoft Learn references
- <https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/methods>
- <https://learn.microsoft.com/dotnet/csharp/fundamentals/exceptions/>
- <https://learn.microsoft.com/dotnet/api/system.argumentexception>
- <https://learn.microsoft.com/dotnet/api/system.exception.stacktrace>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/method-parameters>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/out-parameter-modifier>
