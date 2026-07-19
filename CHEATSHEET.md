# C# and .NET quick reference

Use this after learning a concept; it is not a substitute for the course.

## CLI

```console
dotnet --info
dotnet File.cs
dotnet restore LearningCSharp.slnx --locked-mode
dotnet build LearningCSharp.slnx --configuration Release
dotnet test --solution LearningCSharp.slnx --configuration Release
dotnet format LearningCSharp.slnx
dotnet run --project path/to/App.csproj
```

## Values and control flow

```csharp
string title = "A Wizard of Earthsea";
int pages = 205;
decimal price = 9.95m;
bool finished = false;
string? optionalNote = null;

if (pages > 200)
{
    Console.WriteLine($"{title} is a longer read.");
}

string shelf = pages switch
{
    < 100 => "short",
    <= 300 => "standard",
    _ => "long",
};
```

## Collections and methods

```csharp
var titles = new List<string> { "Kindred", "Piranesi" };
var pagesByTitle = new Dictionary<string, int>
{
    ["Kindred"] = 288,
};

static bool IsLong(int pages) => pages > 300;

foreach (string item in titles)
{
    Console.WriteLine(item);
}
```

## Types

```csharp
public sealed record Book(Guid Id, string Title, string Author);

public interface IBookStore
{
    Task<IReadOnlyList<Book>> LoadAsync(CancellationToken cancellationToken);
}
```

Prefer immutable data when mutation has no clear owner. Validate at the
boundary where invalid state enters.

## LINQ

```csharp
string[] longTitles = books
    .Where(book => book.Pages > 300)
    .OrderBy(book => book.Title)
    .Select(book => book.Title)
    .ToArray();
```

LINQ pipelines are deferred until enumerated unless a terminal operation such
as `ToArray`, `ToList`, `Count`, or `First` produces a result.

## Errors and resources

```csharp
if (string.IsNullOrWhiteSpace(title))
{
    throw new ArgumentException("A title is required.", nameof(title));
}

await using FileStream stream = File.OpenRead(path);
```

Catch only exceptions you can handle meaningfully. `using` and `await using`
express ownership and deterministic cleanup.

## JSON, async, and cancellation

```csharp
await JsonSerializer.SerializeAsync(
    stream,
    books,
    cancellationToken: cancellationToken);

Book[] books = await JsonSerializer.DeserializeAsync<Book[]>(
    stream,
    cancellationToken: cancellationToken) ?? [];
```

Async methods performing I/O conventionally end in `Async`. Pass
`CancellationToken` to the operation that can stop; do not merely check it once
at the entry point.

## Tests

```csharp
[Theory]
[InlineData("", false)]
[InlineData("Dune", true)]
public void IsValidTitle_returns_expected_result(string title, bool expected)
{
    Assert.Equal(expected, BookRules.IsValidTitle(title));
}
```

Name the behavior, arrange controlled inputs, perform one action, and assert
the observable contract. Include normal, boundary, and representative failure
cases.

## HTTP boundaries

```csharp
using HttpResponseMessage response =
    await client.GetAsync("/books", cancellationToken);

response.EnsureSuccessStatusCode();
Book[] books =
    await response.Content.ReadFromJsonAsync<Book[]>(cancellationToken)
    ?? throw new InvalidDataException("The response contained no book list.");
```

Check status before trusting a payload, use a finite timeout or cancellation
policy, and manage `HttpClient` handlers for reuse.

## Naming

- `PascalCase`: types, methods, properties, public members.
- `camelCase`: parameters and local variables.
- `_camelCase`: private instance fields.
- `IName`: interfaces.
- `Async` suffix: methods whose contract returns awaitable asynchronous work.

Continue with the official references in [docs/SOURCES.md](docs/SOURCES.md).
