# 🗄️ Exercise 13 · SQL and SQLite

## 🎯 Goal

Make `BookRepository` create a constrained SQLite schema on first use, and
perform parameterized create/read/list/update operations, wrapping the
transactional mutations (`Create` and `UpdateRating`) each in their own
transaction, with safe, idempotent disposal.

## 🧩 Your task

`BookRepository` (`LibraryCatalogPractice/BookRepository.cs`):

- **`InitializeSchema()`** (called from the constructor)
  - Create the `Books` table the first time the database file opens, with a
    generated primary key, `NOT NULL` columns, a `UNIQUE` constraint on
    ISBN, and `CHECK` constraints that bound the publication year and the
    rating range.
- **`Create(title, author, isbn, publicationYear, rating)`**
  - Run the insert inside its own transaction, passing every value as a
    parameter (never string-concatenated SQL).
  - Return the newly generated, monotonically increasing row id.
  - Let constraint violations (duplicate ISBN, out-of-range publication
    year, out-of-range rating) surface as `SqliteException` instead of
    being caught or worked around.
- **`GetById(id)`**
  - Query by id with a parameter and map columns explicitly by ordinal.
  - Return `null` when no row matches; never throw for a missing id.
- **`List(minimumPublicationYear, limit)`**
  - Order results by title.
  - Honor the optional minimum-year filter and the row limit through
    parameters, not by filtering in memory.
  - Never return more rows than `limit`.
- **`UpdateRating(id, rating)`**
  - Run the update inside its own transaction using parameters.
  - Return whether a row actually matched (`true`/`false`) instead of
    silently doing nothing.
  - Let an out-of-range rating be rejected by the `CHECK` constraint
    (`SqliteException`), leaving the previous rating untouched.
- **`Dispose()`**
  - Release the owned connection.
  - Make repeated `Dispose()` calls safe: a second call must not throw, but
    any further repository call after disposal must raise
    `ObjectDisposedException`.

## ✅ Done when

- All tests in `LibraryCatalogPractice.Tests` pass against your starter
  implementation.
- Duplicate ISBNs, out-of-range publication years, and out-of-range ratings
  are all rejected by the database itself, not by application-level checks.
- Calling `Dispose()` twice never throws, and using the repository
  afterward raises `ObjectDisposedException`.

## 🔗 Related lesson

[Lesson 13 · SQL and SQLite](../../lessons/13-sql-and-sqlite/README.md)

## ▶️ Build, test, and watch

Build the starter first:

```bash
dotnet build exercises/13-sql-and-sqlite/starter/LibraryCatalogPractice/LibraryCatalogPractice.csproj
```

Run the shared tests against your starter implementation (the default):

```bash
dotnet test --project exercises/13-sql-and-sqlite/tests/LibraryCatalogPractice.Tests/LibraryCatalogPractice.Tests.csproj
```

Get continuous feedback while you edit:

```bash
dotnet watch test --project exercises/13-sql-and-sqlite/tests/LibraryCatalogPractice.Tests/LibraryCatalogPractice.Tests.csproj
```

## 🆚 Compare with the solution

After a genuine attempt, run the same tests against the reference solution:

```bash
dotnet test --project exercises/13-sql-and-sqlite/tests/LibraryCatalogPractice.Tests/LibraryCatalogPractice.Tests.csproj -p:CourseImplementation=Solution
```
