# Unit 13 - HTTP Clients and Minimal APIs

## Objectives

By the end of this unit you will be able to:

- treat HTTP as a contract with routes, status codes, headers, and JSON bodies;
- handle status codes before assuming JSON exists;
- configure finite `HttpClient` timeouts and safe client lifetimes with DI;
- model request and response DTOs clearly;
- validate route values and request bodies in a Minimal API;
- wire configuration, logging, and services through dependency injection;
- run deterministic in-process integration tests with no external services.

## Prerequisites

You should understand async/await, JSON, records, exceptions, and the difference between text, bytes, and objects.
This unit assumes Unit 12-level comfort with tasks and cancellation.

## Causal mental model

An HTTP exchange has two separate truths:

1. **Transport truth** - did the server return 200, 201, 400, 404, 500, or time out?
2. **Payload truth** - if the status says a body exists, does the JSON match your DTO contract?

Read status first.
Only then deserialize.

For this beginner-friendly API, the client uses the default buffered completion behavior of `HttpClient`.
That means the configured `HttpClient.Timeout` covers the whole response download for small JSON payloads: headers **and** body bytes.

A safe `HttpClient` lifetime means **reuse the client infrastructure**, not `new HttpClient()` per request.
Minimal APIs are composition points: routes call application services that come from DI.

## Authentic fragments

Status-first client code with buffered completion:

```csharp
HttpResponseMessage response = await httpClient.GetAsync($"/books/{id:D}", cancellationToken);
if (response.StatusCode == HttpStatusCode.NotFound)
{
    return null;
}

response.EnsureSuccessStatusCode();
BookDto? book = await response.Content.ReadFromJsonAsync<BookDto>(options, cancellationToken);
```

Finite timeout with DI:

```csharp
services.AddHttpClient<ReadingListApiClient>(client =>
{
    client.BaseAddress = baseAddress;
    client.Timeout = TimeSpan.FromSeconds(5);
});
```

Minimal API validation:

```csharp
if (string.IsNullOrWhiteSpace(request.Title))
{
    errors.Add(nameof(request.Title), ["Title is required."]);
}

return errors.Count > 0
    ? TypedResults.ValidationProblem(errors)
    : TypedResults.Created($"/books/{created.Id:D}", created);
```

## Sample project

Run the deterministic offline smoke mode from the repository root:

```bash
dotnet run --no-launch-profile --project course/13-http-clients-and-minimal-apis/Samples/ReadingListApiSample/ReadingListApiSample.csproj -- --smoke
```

Expected smoke output:

- starts the API on loopback with an ephemeral port;
- exercises seeded-book, missing-book, and validation scenarios;
- prints `SMOKE TEST PASSED books=2 seeded=Clean Code missing=404 invalidPost=400` and exits on its own.

Start the local sample API from the repository root:

```bash
dotnet run --project course/13-http-clients-and-minimal-apis/Samples/ReadingListApiSample/ReadingListApiSample.csproj
```

While it is running, query it from another terminal:

```bash
curl http://127.0.0.1:5074/books
curl http://127.0.0.1:5074/books/11111111-1111-1111-1111-111111111111
```

Expected behavior:

- `/books` returns JSON for seeded books;
- `/books/{id}` returns one book or 404;
- POSTing invalid JSON contracts returns validation feedback;
- everything runs locally with no external dependency.

## Practice contract

Default solution tests:

```bash
dotnet test --project course/13-http-clients-and-minimal-apis/Practice/Tests/ReadingListPractice.Tests/ReadingListPractice.Tests.csproj
```

Starter feedback:

```bash
dotnet test --project course/13-http-clients-and-minimal-apis/Practice/Tests/ReadingListPractice.Tests/ReadingListPractice.Tests.csproj -p:CourseImplementation=Starter
```

Your implementation must make these statements true:

1. The Minimal API exposes `GET /books`, `GET /books/{id}`, and `POST /books`.
2. `GET /books/{id}` returns 400 for `Guid.Empty`, 404 for missing books, and 200 with JSON for existing books.
3. `POST /books` validates title, author, and year before creating anything.
4. `GET /books` respects the configured `Catalog:MaxResults` limit.
5. `ReadingListApiClient` checks status codes before deserializing.
6. `AddReadingListApiClient` configures a finite timeout and reuses `HttpClient` through DI.
7. Integration tests stay in-process and offline.

Deterministic feedback:

- broken routes fail integration tests immediately;
- bad validation fails the bad-request test;
- unsafe client setup fails the timeout test, including a slow-body download case;
- wrong JSON contracts fail round-trip tests.

## Experiment

1. Change `Catalog:MaxResults` to `1` and query `/books` again.
2. Post a request with an empty title and inspect the validation payload.
3. Temporarily remove the 404 branch from the client and see why status-first handling matters.
4. Increase the client timeout and discuss why "infinite" is a dangerous default.

## Common mistakes and diagnosis

- **Mistake:** deserializing before checking the status code.
  **Diagnosis:** 404 or 400 responses cause confusing JSON errors.

- **Mistake:** constructing `HttpClient` per call.
  **Diagnosis:** lifetime management becomes ad hoc and unsafe.

- **Mistake:** letting DTOs and domain rules drift apart.
  **Diagnosis:** requests compile but represent invalid states.

- **Mistake:** validating nothing at the API boundary.
  **Diagnosis:** bad data enters deeper layers and failures move farther from the cause.

## Summary

HTTP code becomes calmer when you separate transport handling, JSON contracts, and application behavior.
Minimal APIs and typed clients work well together when DI owns lifetimes and tests stay local.

## Review questions

1. Why should clients check status codes before JSON deserialization?
2. What does this unit's timeout actually cover?
3. Which rules belong at the API boundary instead of deeper inside the app?
4. Why are in-process integration tests valuable here?
5. What does DI own in your HTTP application?

## Official Microsoft Learn links

- [Make HTTP requests with `HttpClient`](https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient)
- [Use `IHttpClientFactory` to implement resilient HTTP requests](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory)
- [Build Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
- [Integration tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/integration-tests)
