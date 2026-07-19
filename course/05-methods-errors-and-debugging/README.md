# Unit 05: Methods, errors, and debugging

## Measurable objectives
By the end of this unit, you can:
- explain a method as a named block with parameters, local scope, and a return value;
- call an overloaded method and explain why C# chose that version;
- validate inputs before continuing;
- throw and recognize specific exceptions for invalid data;
- read the idea of a stack trace as “how the program got here.”

## Explicit prerequisites
- Units 01-04.
- Comfort reading arrays, loops, conditions, and formatted output.

## Plain-language causal mental model
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

## Samples
- `Samples/methods-and-overloads.cs`
- `Samples/exceptions-and-debugging.cs`

## Exact run commands from repository root
```bash
dotnet course/05-methods-errors-and-debugging/Samples/methods-and-overloads.cs
dotnet course/05-methods-errors-and-debugging/Samples/exceptions-and-debugging.cs
dotnet build course/05-methods-errors-and-debugging/Practice/Starter/MethodsErrorsAndDebugging.csproj
dotnet test --project course/05-methods-errors-and-debugging/Practice/Tests/MethodsErrorsAndDebugging.Tests.csproj
dotnet test --project course/05-methods-errors-and-debugging/Practice/Tests/MethodsErrorsAndDebugging.Tests.csproj -p:CourseImplementation=Starter
```

## Expected observable behavior
`methods-and-overloads.cs` prints two averages: one from two separate arguments and one from an array overload. `exceptions-and-debugging.cs` catches an exception, prints its type and message, and confirms that the stack trace mentions the validating method.

That sample catches `Exception` only to inspect the diagnostic information that
all exceptions share. Application code should catch a specific exception only
when it can handle that failure meaningfully; the broad diagnostic catch is not
the normal production pattern.

## Learner experiment
1. Run `dotnet course/05-methods-errors-and-debugging/Samples/exceptions-and-debugging.cs`.
2. Predict what happens if you remove the empty-array validation.
3. Read the code first, then make the change temporarily.
4. Run again and compare the exception or output.
5. Restore the validation and explain why the earlier failure was easier to diagnose.

## Common mistakes with diagnosis
- **Mistake:** Returning `total / count` as integers when you wanted a fractional average.
  **Diagnosis:** Integer division loses the fraction before the result becomes `double`.
- **Mistake:** Throwing a very general exception with no clear parameter name.
  **Diagnosis:** The failure becomes harder to diagnose because the signal is vague.
- **Mistake:** Assuming local variables from one method are visible in another.
  **Diagnosis:** Scope rules hide locals outside the method where they were declared.

## Practice contract
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

## Feedback instructions
- Build the starter project: `dotnet build course/05-methods-errors-and-debugging/Practice/Starter/MethodsErrorsAndDebugging.csproj`
- Test your implementation: `dotnet test --project course/05-methods-errors-and-debugging/Practice/Tests/MethodsErrorsAndDebugging.Tests.csproj -p:CourseImplementation=Starter`
- If an exception test fails, compare both the exception type and the parameter name.
- If average tests fail, inspect where integer division happens and whether you cast soon enough.

## Concise summary
Methods organize code into reusable units. Validation and specific exceptions make bugs easier to find, and stack traces show the chain of calls that led to a failure.

## Review questions
1. What is the difference between a parameter and a local variable?
2. Why are overloads useful?
3. Why should validation happen early?
4. What does a stack trace tell you?
5. What exception fits an out-of-range score best?

## Authoritative Microsoft Learn references
- <https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/methods>
- <https://learn.microsoft.com/dotnet/csharp/fundamentals/exceptions/>
- <https://learn.microsoft.com/dotnet/api/system.argumentexception>
- <https://learn.microsoft.com/dotnet/api/system.exception.stacktrace>
