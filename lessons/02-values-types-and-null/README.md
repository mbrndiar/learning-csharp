# 🧭 Lesson 02 · Values, types, and null

## 🎯 Measurable objectives
By the end of this lesson, you can:
- declare variables with beginner-friendly built-in C# types such as `int`, `double`, `decimal`, `char`, `bool`, and `string`;
- explain why the type of a variable changes what operations are possible;
- use conversions and arithmetic operators to compute a percentage;
- build readable output with string interpolation;
- describe the difference between a missing nullable value and a present value.

## ✅ Explicit prerequisites
- Lesson 01 or equivalent comfort running `dotnet path/to/file.cs`.
- Ability to edit a text file and rerun it from the terminal.

## 🧠 Plain-language causal mental model
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

Every integer type also has a fixed range. `int` silently wraps past
`int.MaxValue` by default, which hides a real bug. Wrap the arithmetic in a
`checked` block when you want C# to raise `OverflowException` instead of
wrapping silently:

```csharp
try
{
    checked
    {
        int pagesReadAllTime = int.MaxValue;
        pagesReadAllTime += 1;
    }
}
catch (OverflowException exception)
{
    Console.WriteLine($"Overflow caught: {exception.Message}");
}
```

`double` has its own edge case: `double.NaN` ("not a number") is a real
`double` value, but it is not a usable rating. A rating of `0.0` through `5.0`
excludes `double.NaN` even though `NaN >= 0.0` and `NaN <= 5.0` both evaluate
to `false` - comparisons with `NaN` are never `true`, so range checks must
treat it as invalid explicitly rather than assuming a failed comparison means
"in range."

## ▶️ Demonstrations
- `01-ValuesAndTypes.cs`
- `02-NullableValues.cs`

## ▶️ Exact run commands from repository root
```bash
dotnet lessons/02-values-types-and-null/01-ValuesAndTypes.cs
dotnet lessons/02-values-types-and-null/02-NullableValues.cs
dotnet build exercises/02-values-types-and-null/starter/ValuesTypesAndNull.csproj
dotnet test --project exercises/02-values-types-and-null/tests/ValuesTypesAndNull.Tests.csproj
dotnet test --project exercises/02-values-types-and-null/tests/ValuesTypesAndNull.Tests.csproj -p:CourseImplementation=Solution
```

## 👀 Expected observable behavior
`01-ValuesAndTypes.cs` prints a few values, including a decimal money amount and Unicode text such as `Café ☕`. `02-NullableValues.cs` shows one line with a fallback title and one line with a real rating.

## 🧩 Learner experiment
1. Run `dotnet lessons/02-values-types-and-null/01-ValuesAndTypes.cs`.
2. Change the `double` division so the cast disappears.
3. Run again.
4. Compare the percentage before and after the change.
5. Write down why the type change affected the result.
6. Run the `checked` overflow fragment above, then remove the `checked` keyword and rerun.
7. Explain in one sentence why the unchecked version produces a wrapped, wrong-looking number instead of an exception.

## ⚠️ Common mistakes with diagnosis
- **Mistake:** Dividing two `int` values when you expected decimals.
  **Diagnosis:** Integer division drops the fractional part before you ever format the result.
- **Mistake:** Forgetting that `null` is not the same as an empty string.
  **Diagnosis:** Your fallback logic never runs when the value is empty-but-present.
- **Mistake:** Using `double` for money values.
  **Diagnosis:** Repeated calculations can show surprising precision artifacts; `decimal` is usually the better money type.
- **Mistake:** Letting `int` arithmetic wrap past `int.MaxValue` unnoticed.
  **Diagnosis:** Without a `checked` context, the result silently becomes a small or negative number instead of failing loudly.
- **Mistake:** Assuming a rating that is not less than `0.0` and not greater than `5.0` is automatically valid.
  **Diagnosis:** `double.NaN` fails both comparisons yet is not a real number; range validation must reject it explicitly, for example with `double.IsNaN(rating)`.

## 🧪 Practice contract
Implement `ReadingProgressFormatter.DescribeProgress(string? title, int pagesRead, int totalPages, double? rating)`.

- **Inputs:** nullable `title`, `pagesRead`, `totalPages`, nullable `rating`
- **Output:** one descriptive line like `Café Notes: 12/30 pages (40.0%), 4.5★`
- **Constraints:** use `(untitled)` when `title` is `null` or whitespace; use `unrated` when `rating` is missing
- **Edge cases:** `pagesRead == 0`, `totalPages == 0`, Unicode titles, rating `null`
- **Invalid cases:** negative page counts, `pagesRead > totalPages`, ratings outside `0.0` through `5.0`, or `rating == double.NaN` (`double.IsNaN(rating)` must be checked explicitly)

See [exercises/02-values-types-and-null/README.md](../../exercises/02-values-types-and-null/README.md) for the complete task, edge cases, and Starter-first build/test/watch commands.

## 🔁 Feedback instructions
- Build your starter project: `dotnet build exercises/02-values-types-and-null/starter/ValuesTypesAndNull.csproj`
- Run the shared tests against starter code: `dotnet test --project exercises/02-values-types-and-null/tests/ValuesTypesAndNull.Tests.csproj`
- When a test fails, check the exact punctuation, numeric formatting, and fallback text first.
- Compare your output to the demonstration format before changing the algorithm.

## 📝 Concise summary
Types shape behavior. Conversions affect arithmetic, interpolation turns values into readable text, and `null` means you must choose a fallback or handle the absence explicitly.

## ❓ Review questions
1. Why does `(double)pagesRead / totalPages` behave differently from `pagesRead / totalPages`?
2. When should you use a nullable type?
3. What does the `?` mean in `string?` and `double?`?
4. Why is string interpolation useful for learners?
5. What is a simple sign that a calculation used the wrong numeric type?
6. Why does `int.MaxValue + 1` wrap silently, and what does wrapping it in `checked` change?
7. Why is `double.NaN` outside the valid `0.0`-`5.0` rating range even though the direct comparisons look like they might pass?

## 📚 Authoritative Microsoft Learn references
- <https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/built-in-types>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/nullable-value-types>
- <https://learn.microsoft.com/dotnet/csharp/nullable-references>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/tokens/interpolated>
- <https://learn.microsoft.com/dotnet/csharp/language-reference/statements/checked-and-unchecked>
- <https://learn.microsoft.com/dotnet/api/system.double.isnan>
