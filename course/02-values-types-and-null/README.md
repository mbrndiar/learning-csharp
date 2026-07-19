# Unit 02: Values, types, and null

## Measurable objectives
By the end of this unit, you can:
- declare variables with beginner-friendly built-in C# types such as `int`, `double`, `decimal`, `char`, `bool`, and `string`;
- explain why the type of a variable changes what operations are possible;
- use conversions and arithmetic operators to compute a percentage;
- build readable output with string interpolation;
- describe the difference between a missing nullable value and a present value.

## Explicit prerequisites
- Unit 01 or equivalent comfort running `dotnet path/to/file.cs`.
- Ability to edit a text file and rerun it from the terminal.

## Plain-language causal mental model
A variable is a labeled box that holds a value. The **type** says what kind of value fits in the box and what operations C# allows. Operators combine values to produce new values. `null` is a deliberate “there is no value here” marker, so your code must decide what to do when a box is empty.

Authentic fragment:

```csharp
string? title = null;
int pagesRead = 12;
int totalPages = 30;
double percent = (double)pagesRead / totalPages * 100;
Console.WriteLine($"{title ?? "(untitled)"}: {percent:0.0}%");
```

The cast to `double` changes integer division into fractional division, which changes the observable result.

## Samples
- `Samples/values-and-types.cs`
- `Samples/nullable-values.cs`

## Exact run commands from repository root
```bash
dotnet course/02-values-types-and-null/Samples/values-and-types.cs
dotnet course/02-values-types-and-null/Samples/nullable-values.cs
dotnet build course/02-values-types-and-null/Practice/Starter/ValuesTypesAndNull.csproj
dotnet test --project course/02-values-types-and-null/Practice/Tests/ValuesTypesAndNull.Tests.csproj
dotnet test --project course/02-values-types-and-null/Practice/Tests/ValuesTypesAndNull.Tests.csproj -p:CourseImplementation=Starter
```

## Expected observable behavior
`values-and-types.cs` prints a few values, including a decimal money amount and Unicode text such as `Café ☕`. `nullable-values.cs` shows one line with a fallback title and one line with a real rating.

## Learner experiment
1. Run `dotnet course/02-values-types-and-null/Samples/values-and-types.cs`.
2. Change the `double` division so the cast disappears.
3. Run again.
4. Compare the percentage before and after the change.
5. Write down why the type change affected the result.

## Common mistakes with diagnosis
- **Mistake:** Dividing two `int` values when you expected decimals.
  **Diagnosis:** Integer division drops the fractional part before you ever format the result.
- **Mistake:** Forgetting that `null` is not the same as an empty string.
  **Diagnosis:** Your fallback logic never runs when the value is empty-but-present.
- **Mistake:** Using `double` for money values.
  **Diagnosis:** Repeated calculations can show surprising precision artifacts; `decimal` is usually the better money type.

## Practice contract
Implement `ReadingProgressFormatter.DescribeProgress(string? title, int pagesRead, int totalPages, double? rating)`.

- **Inputs:** nullable `title`, `pagesRead`, `totalPages`, nullable `rating`
- **Output:** one descriptive line like `Café Notes: 12/30 pages (40.0%), 4.5★`
- **Constraints:** use `(untitled)` when `title` is `null` or whitespace; use `unrated` when `rating` is missing
- **Edge cases:** `pagesRead == 0`, `totalPages == 0`, Unicode titles, rating `null`
- **Invalid cases:** negative page counts, `pagesRead > totalPages`, or ratings outside `0.0` through `5.0`

## Feedback instructions
- Build your starter project: `dotnet build course/02-values-types-and-null/Practice/Starter/ValuesTypesAndNull.csproj`
- Run the shared tests against starter code: `dotnet test --project course/02-values-types-and-null/Practice/Tests/ValuesTypesAndNull.Tests.csproj -p:CourseImplementation=Starter`
- When a test fails, check the exact punctuation, numeric formatting, and fallback text first.
- Compare your output to the sample format before changing the algorithm.

## Concise summary
Types shape behavior. Conversions affect arithmetic, interpolation turns values into readable text, and `null` means you must choose a fallback or handle the absence explicitly.

## Review questions
1. Why does `(double)pagesRead / totalPages` behave differently from `pagesRead / totalPages`?
2. When should you use a nullable type?
3. What does the `?` mean in `string?` and `double?`?
4. Why is string interpolation useful for learners?
5. What is a simple sign that a calculation used the wrong numeric type?

## Authoritative Microsoft Learn references
- <https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/built-in-types>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/nullable-value-types>
- <https://learn.microsoft.com/dotnet/csharp/nullable-references>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/tokens/interpolated>
