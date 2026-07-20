# 🧭 Lesson 14 · HTTP clients and Minimal APIs

## 🎯 Objectives

By the end of this lesson you will be able to:

- explain the ASP.NET Core host, its request pipeline, and how middleware
  chains through a `RequestDelegate`;
- read and write requests/responses at the level of bytes, streams, headers,
  and status codes, before any routing helper does it for you;
- dispatch on HTTP method and path manually, and enforce content-type and
  body-size boundaries yourself;
- explain why Minimal APIs are the preferred default for a normal
  application, and what they do for you under the hood;
- issue raw `HttpClient` requests and own their disposal, then contrast that
  with a typed client resolved through `IHttpClientFactory`;
- configure finite `HttpClient` timeouts, cancellation, and DI-owned
  lifetimes, including in tests;
- treat HTTP as a contract with routes, status codes, headers, and JSON
  bodies, and validate request bodies before trusting them;
- read a checked-in OpenAPI 3.1 contract as independent evidence, and explain
  what it can and cannot verify;
- run deterministic in-process/loopback integration tests with no external
  services.

## ✅ Prerequisites

Finish Lessons 1-13 first. You should be comfortable with async/await, JSON,
records, exceptions, dependency injection, and the difference between text,
bytes, and objects. This lesson leans on Lesson 12's task/cancellation model and
Lesson 10's testing habits; nothing here assumes any specific
[Lesson 13](../13_sql_and_sqlite/README.md) (SQL and SQLite) content,
but this lesson does assume you can read and write to a boundary (a file, a
socket) without panicking about what "async" is doing there.

## 🧠 Causal mental model

### The host and its pipeline

An ASP.NET Core app is a **host** that owns startup, shutdown, and one
**request pipeline**. Every incoming request runs through a chain of
**middleware**; each piece of middleware receives the current `HttpContext`
and a `RequestDelegate` (`next`) that calls the rest of the chain. Middleware
can inspect or rewrite the request, short-circuit before calling `next`, or
wrap `next` in a `try`/`catch` to react to whatever happens deeper in the
chain. This is exactly how centralized error handling and logging work: they
are middleware that wraps everything after them.

```csharp
app.Use(async (HttpContext context, RequestDelegate next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Unhandled failure");     // full detail, logged only
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync("""{"title":"An unexpected error occurred."}"""); // sanitized, sent to the client
    }
});
```

That split - log everything, tell the client almost nothing - is what
"sanitized failure" means for the rest of this lesson.

### Requests and responses are bytes and streams first

`HttpContext.Request` and `HttpContext.Response` are not JSON objects; they
are method/path/header metadata plus a `Body` **stream**. Reading a request
body means reading bytes (typically UTF-8-encoded JSON) off that stream, and
writing a response means writing bytes back onto a different stream, with a
status code and headers you set explicitly. `System.Text.Json`'s
`SerializeAsync`/`DeserializeAsync` overloads that take a `Stream` exist
specifically so you never have to buffer a whole payload into one `string`
just to inspect it.

Two boundaries matter every time you read a request body:

- **Content-Type**: does the client claim this is JSON at all
  (`HasJsonContentType()`)? If not, `415 Unsupported Media Type` is the
  honest answer, not a parsing attempt.
- **Size**: a declared `Content-Length` that already exceeds your limit lets
  you reject before reading a single byte - but `Content-Length` can be
  absent (chunked transfer encoding) or simply wrong. The only boundary you
  can really trust is the one you enforce while reading: count bytes as you
  go and stop once you exceed the limit, regardless of what any header
  claimed.

Method and path routing, done manually, is nothing more than string
comparisons against `HttpContext.Request.Method` and `.Path`:

```csharp
if (HttpMethods.IsGet(context.Request.Method) && context.Request.Path == "/books")
{
    // ...
}
```

`CancellationToken` still matters here: `HttpContext.RequestAborted` fires
when the client disconnects, and every read/write you perform should honor
it, exactly like the async work you owned in Lesson 12.

### Middleware first, then Minimal APIs

This lesson deliberately builds the **same tiny Books contract twice**: once as
hand-written middleware (`MiddlewareBookApiSample`), and once as a Minimal
API (`ReadingListApiSample`). Reading the middleware version first is the
point - it is what a route mapping call is doing for you underneath:
matching a method and path, binding a body, choosing a status code, writing
a response. Once you have felt that manually, `app.MapPost(...)` stops
looking like magic.

For a normal application, prefer the Minimal API style. It gives you the
same pipeline with far less ceremony: automatic model binding and
validation-friendly `TypedResults`, `ProblemDetails` wiring via
`AddProblemDetails()`/`UseExceptionHandler()` instead of hand-rolled
try/catch middleware, and route parameters bound straight from the URL. Drop
to raw middleware only when you need behavior a route mapping cannot express
(a catch-all proxy, a protocol adapter, a cross-cutting concern that has to
run before routing even happens).

### Raw `HttpClient`, then typed client + `IHttpClientFactory`

The simplest way to call an HTTP API is also the easiest one to get wrong at
scale:

```csharp
using HttpClient client = new() { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(5) };
using HttpResponseMessage response = await client.GetAsync(path, cancellationToken);
```

This "raw" pattern - create, use, `using`-dispose - is honest and easy to
reason about for one call. It is also the anti-pattern under load: a new
`HttpClient` per call means a new connection/socket per call, because
nothing shares or reuses the underlying handler. Under enough concurrent
traffic that exhausts sockets (`SocketException`s, port exhaustion) even
though every individual call disposes cleanly.

A **typed client** resolved through `IHttpClientFactory` fixes this by
handing lifetime ownership to DI instead of to the calling method:

```csharp
services.AddHttpClient<ReadingListApiClient>(client =>
{
    client.BaseAddress = baseAddress;
    client.Timeout = TimeSpan.FromSeconds(5);
});
```

`IHttpClientFactory` pools and reuses the underlying `HttpMessageHandler`,
recycling it periodically so DNS changes are eventually picked up (a single
long-lived `HttpClient` would otherwise cache a stale DNS resolution
forever), while still handing your code a plain `HttpClient` to call. The
**timeout stays finite either way** - "infinite" is never a safe default,
and it must cover slow bodies, not just slow headers. In tests, you can swap
the real transport for a fake `HttpMessageHandler` (`ConfigurePrimaryHttpMessageHandler`)
so client tests never touch a real network.

Either way, **read status before you deserialize**:

```csharp
if (response.StatusCode == HttpStatusCode.NotFound)
{
    return null;
}

response.EnsureSuccessStatusCode();
BookDto? book = await response.Content.ReadFromJsonAsync<BookDto>(options, cancellationToken);
```

### A checked-in OpenAPI contract is independent evidence

`reading-list-api.openapi.json` in this folder is a hand-authored OpenAPI
3.1 document describing the Books contract: its paths, methods, request/
response shapes, and status codes. It is **not generated** from the running
server. That distinction matters:

- **Generation** (for example `Microsoft.AspNetCore.OpenApi` + `MapOpenApi()`)
  produces a document *from* the code, so a bug that changes behavior also
  silently changes the generated document - they can never disagree.
- A **checked-in, hand-authored contract** is independent evidence: it was
  written by reasoning about the intended API, not by reflecting over
  whatever the code currently does. It can disagree with the running server,
  and when it does, that disagreement is the whole point - it surfaces a
  real bug or a real intentional change that needs a matching contract
  update.

The exercise's `OpenApiContractTests` performs **semantic validation**: it
parses the JSON with `System.Text.Json` (already stable, already used
everywhere else in this course) and asserts the declared paths/operations/
schema properties are present and match the DTO shape - no third-party
OpenAPI parser is added for this. A separate test then calls the *live*
Minimal API and confirms its status codes match what the contract claims.

That combination also shows the contract's **boundary limitation**: a
static document can tell you a shape is declared, but it cannot execute
anything. It cannot catch a concurrency bug, a wrong validation message, or
a timeout that is too short - only running, behavioral tests can. Keep both:
the contract is fast, independent, and diffable; the tests are the only
thing that can prove the server actually behaves the way the contract says
it should.

## 🔤 Authentic fragments

Terminal middleware replacing all route mapping (see `MiddlewareBookApiSample`):

```csharp
app.Run(context => BookRequestPipeline.HandleAsync(context, repository));
```

Enforcing a body-size boundary against bytes actually read, not just a
declared header:

```csharp
while ((bytesRead = await body.ReadAsync(chunk, cancellationToken)) > 0)
{
    totalRead += bytesRead;
    if (totalRead > maxBytes)
    {
        return null; // reject, regardless of what Content-Length claimed
    }

    buffer.Write(chunk, 0, bytesRead);
}
```

The same contract as a Minimal API endpoint (see `ReadingListApiSample`):

```csharp
app.MapGet("/books/{id:guid}", (Guid id, SampleBookRepository repository) =>
{
    if (id == Guid.Empty)
    {
        return Results.BadRequest("Use a non-empty GUID.");
    }

    SampleBook? book = repository.Get(id);
    return book is null ? Results.NotFound() : Results.Ok(book);
});
```

Typed client resolved through `IHttpClientFactory`, contrasted with a raw,
self-disposing call in the same sample:

```csharp
services.AddHttpClient<SampleTypedBookClient>(client =>
{
    client.BaseAddress = new Uri(baseAddress);
    client.Timeout = TimeSpan.FromSeconds(5);
});
```

## ▶️ Sample projects

Both samples build the same Books resource so you can compare them directly.

### 1. Low-level middleware (read this one first)

```bash
dotnet run --no-launch-profile --project lessons/14_http_clients_and_minimal_apis/middleware_book_api/MiddlewareBookApiSample.csproj -- --smoke
```

Expected smoke output:

- starts the app on loopback with an ephemeral port;
- exercises a seeded list, a missing book (404), a rejected content-type
  (415), and a successful create (201) with a `Location` header;
- prints `SMOKE TEST PASSED books=2 missing=404 badContentType=415 created=201` and exits on its own.

Start it locally instead:

```bash
dotnet run --project lessons/14_http_clients_and_minimal_apis/middleware_book_api/MiddlewareBookApiSample.csproj
```

```bash
curl http://127.0.0.1:5175/books
curl -i -X POST http://127.0.0.1:5175/books -H "Content-Type: application/json" -d "{\"title\":\"Deep Work\",\"author\":\"Cal Newport\",\"yearPublished\":2016}"
```

### 2. Minimal API (the preferred default)

```bash
dotnet run --no-launch-profile --project lessons/14_http_clients_and_minimal_apis/reading_list_api/ReadingListApiSample.csproj -- --smoke
```

Expected smoke output:

- starts the API on loopback with an ephemeral port;
- exercises seeded-book, missing-book, and validation scenarios with a raw
  `HttpClient`, then repeats the book count through a typed client resolved
  from its own small `IHttpClientFactory` registration;
- prints `SMOKE TEST PASSED books=2 seeded=Clean Code missing=404 invalidPost=400 typedClientBooks=2` and exits on its own.

Start it locally instead:

```bash
dotnet run --project lessons/14_http_clients_and_minimal_apis/reading_list_api/ReadingListApiSample.csproj
```

```bash
curl http://127.0.0.1:5074/books
curl http://127.0.0.1:5074/books/11111111-1111-1111-1111-111111111111
```

Expected behavior for either sample:

- `/books` returns JSON for seeded books;
- `/books/{id}` returns one book or 404;
- POSTing an invalid body returns validation feedback or a rejected
  content-type/size, never a silent success;
- everything runs locally with no external dependency.

## 📄 Checked-in contract

[`reading-list-api.openapi.json`](reading-list-api.openapi.json) is the
hand-authored OpenAPI 3.1 contract described above. Open it directly - it is
plain JSON, which is valid OpenAPI - and compare its `paths`/`components`
against the samples' behavior.

## 🧪 Exercise

The matching exercise lives in
[`exercises/14_http_clients_and_minimal_apis/`](../../exercises/14_http_clients_and_minimal_apis/).
It asks you to implement the same small Books contract across **four**
surfaces - a low-level middleware app, a Minimal API app, a raw HTTP client,
and a typed client + `IHttpClientFactory` - plus semantic validation against
the checked-in contract above. Run its tests from the repository root:

```bash
dotnet test --project exercises/14_http_clients_and_minimal_apis/tests/ReadingListPractice.Tests/ReadingListPractice.Tests.csproj
dotnet test --project exercises/14_http_clients_and_minimal_apis/tests/ReadingListPractice.Tests/ReadingListPractice.Tests.csproj -p:CourseImplementation=Starter
```

## 🧩 Experiment

1. Change `Catalog:MaxResults` to `1` and query `/books` again.
2. Post a request with an empty title and inspect the validation payload.
3. Temporarily remove the 404 branch from a client and see why status-first
   handling matters.
4. Increase a client timeout and discuss why "infinite" is a dangerous
   default.
5. In the middleware sample, send a body larger than the limit with
   `Transfer-Encoding: chunked` (no `Content-Length`) and confirm it is still
   rejected.
6. Edit `reading-list-api.openapi.json` to remove a required property, then
   re-read why that change alone cannot break the running server - only a
   matching code change (or a test that catches the drift) can.

## ⚠️ Common mistakes and diagnosis

- **Mistake:** deserializing before checking the status code.
  **Diagnosis:** 404 or 400 responses cause confusing JSON errors.

- **Mistake:** constructing `HttpClient` per call in a hot path.
  **Diagnosis:** lifetime management becomes ad hoc, and enough concurrent
  traffic exhausts sockets.

- **Mistake:** trusting a declared `Content-Length` as the only size guard.
  **Diagnosis:** chunked requests have no `Content-Length` at all, and a
  client can always send an inaccurate one; only counting bytes while
  reading is trustworthy.

- **Mistake:** writing full exception details into an HTTP response body.
  **Diagnosis:** internal detail leaks to callers; log it instead and return
  a generic, sanitized failure.

- **Mistake:** assuming a generated OpenAPI document proves the API is
  correct.
  **Diagnosis:** a document generated from the code can never disagree with
  the code; only independent evidence (a hand-authored contract, or
  behavioral tests) can catch a real mismatch.

- **Mistake:** treating Minimal APIs as "the easy but lesser" option.
  **Diagnosis:** they are the preferred default for a normal application;
  drop to raw middleware only when you need something a route mapping
  cannot express.

## 📝 Summary

HTTP code becomes calmer when you separate transport handling, JSON
contracts, and application behavior. Writing the low-level middleware
version once demystifies what Minimal APIs do for you; typed clients over
`IHttpClientFactory` give DI the same kind of ownership over HTTP lifetimes
that it already has over everything else in this course. A checked-in
contract and behavioral tests check different things and neither replaces
the other.

## ❓ Review questions

1. What does a piece of middleware receive, and what does calling (or not
   calling) `next` do?
2. Why is `Content-Length` alone not a trustworthy size boundary?
3. When would you reach for raw middleware instead of a Minimal API?
4. What does a raw `HttpClient` own that a typed client over
   `IHttpClientFactory` does not?
5. Why does `IHttpClientFactory` matter for DNS behavior specifically?
6. What can a checked-in OpenAPI contract prove, and what can it never
   prove on its own?

## 📚 Official Microsoft Learn links

- [ASP.NET Core Middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/)
- [Build Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
- [Make HTTP requests with `HttpClient`](https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient)
- [Use `IHttpClientFactory` to implement resilient HTTP requests](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory)
- [Integration tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/integration-tests)
- [OpenAPI support in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0)
