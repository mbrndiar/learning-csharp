# 🌐 Exercise 14 · HTTP clients and Minimal APIs

## 🎯 Goal

Implement the same small Books contract across four surfaces - a low-level
middleware app, a Minimal API app, a raw `HttpClient` helper, and a typed
client resolved through `IHttpClientFactory` - then confirm the running
Minimal API matches the checked-in OpenAPI contract.

## 🧩 Your task

### `ReadingListApi.Core` — `InMemoryBookRepository.cs`

Both API projects share this repository. It must:

- Start seeded with exactly two books, matching `SeedBookIds.CleanCode`
  ("Clean Code") and `SeedBookIds.Refactoring` ("Refactoring").
- **`ListAsync(author, maxResults, cancellationToken)`**: return the
  seeded/added books, filtered by author (case-insensitive) when `author`
  is provided, and never more than `maxResults` books.
- **`GetAsync(id, cancellationToken)`**: return the matching book, or
  `null` when no book has that id.
- **`AddAsync(request, cancellationToken)`**: store a new book with a
  freshly generated id and return the stored book.
- Stay safe to call concurrently: many callers may list, get, and add at
  the same time without lost updates or exceptions.

### `ReadingListApi.Middleware` — `Program.cs` (`BookRequestPipeline.HandleAsync`)

Replace the placeholder (currently returns `501`) so the handler dispatches
on `context.Request.Method`/`context.Request.Path` directly - no Minimal
API routing helpers - to implement:

- `GET /books` → `200` with the seeded/added books as JSON.
- `GET /books/{id}` → `400` for `Guid.Empty`, `404` for a missing id, `200`
  with the book otherwise.
- `POST /books` → reject a non-JSON content type with `415`, reject a body
  over 16 KiB with `413`, reject an invalid body with `400`, otherwise create
  the book and return `201` with a `Location` header and the created book.

### `ReadingListApi` — `Program.cs` (Minimal API)

Replace both placeholder endpoints:

- **`GET /books/{id:guid}`**: currently always returns `404`. Validate
  `Guid.Empty` as `400` with a message, return `404` when no book matches,
  and `200` with the book otherwise.
- **`POST /books`**: currently always returns `501`. Validate the request
  body and return `201` with the created book (and its location) on
  success, or `400` with validation details naming `Title`, `Author`, and
  `YearPublished` on failure.

### `ReadingListClient` — `RawBookHttpClient.cs`

- **`GetBooksAsync(baseAddress, author, cancellationToken)`**: create a
  disposable `HttpClient` scoped to this call, issue `GET /books`
  (including the `author` filter only when provided), check the status
  before reading, and deserialize the buffered JSON body.
- **`TryGetBookAsync(baseAddress, id, cancellationToken)`**: create a
  disposable `HttpClient` scoped to this call, return `null` for `404`,
  otherwise ensure success and deserialize the book.

### `ReadingListClient` — `ReadingListApiClient.cs`

- **`GetBooksAsync(author, cancellationToken)`**: issue `GET /books`
  (include the author filter only when provided); check the status before
  reading, dispose the response, and treat a missing body as invalid data.
- **`TryGetBookAsync(id, cancellationToken)`**: issue `GET /books/{id}`;
  return `null` on `404`; ensure success for other statuses; dispose the
  response; treat a missing body as invalid data.
- **`CreateBookAsync(request, cancellationToken)`**: reject a `null`
  request; `POST` it as JSON; ensure success; dispose the response; treat a
  missing body as invalid data.

## ✅ Done when

- All tests in `ReadingListPractice.Tests` pass against your starter
  implementation - both API projects, both client shapes, and the OpenAPI
  contract tests.
- Both API projects return identical status codes for the same
  seeded/missing/invalid scenarios.
- Every client path checks the response status before reading the body and
  disposes what it creates.

## 🔗 Related lesson

[Lesson 14 · HTTP clients and Minimal APIs](../../lessons/14-http-clients-and-minimal-apis/README.md)

## ▶️ Build, test, and watch

Build the starter first:

```bash
dotnet build exercises/14-http-clients-and-minimal-apis/starter/ReadingListApi/ReadingListApi.csproj
dotnet build exercises/14-http-clients-and-minimal-apis/starter/ReadingListApi.Middleware/ReadingListApi.Middleware.csproj
```

Run the shared tests against your starter implementation (the default):

```bash
dotnet test --project exercises/14-http-clients-and-minimal-apis/tests/ReadingListPractice.Tests/ReadingListPractice.Tests.csproj
```

Get continuous feedback while you edit:

```bash
dotnet watch test --project exercises/14-http-clients-and-minimal-apis/tests/ReadingListPractice.Tests/ReadingListPractice.Tests.csproj
```

## 🆚 Compare with the solution

After a genuine attempt, run the same tests against the reference solution:

```bash
dotnet test --project exercises/14-http-clients-and-minimal-apis/tests/ReadingListPractice.Tests/ReadingListPractice.Tests.csproj -p:CourseImplementation=Solution
```
