# 🧭 Lesson 13 · SQL and SQLite

## 🎯 Objectives

By the end of this lesson you will be able to:

- describe relational data as tables, rows, and columns with a declared shape;
- design a primary key and a foreign key that turn "this loan is about that book" into an enforced fact;
- use NOT NULL, UNIQUE, and CHECK constraints so the database rejects broken rows instead of trusting every caller;
- explain SQLite type affinity and why it is a hint toward a type, not a guarantee like a strict column type;
- open a `SqliteConnection`, run `SqliteCommand`s with named parameters, and never build SQL by concatenating values;
- read rows with a `SqliteDataReader`, map them explicitly by column, and handle `NULL` columns without throwing;
- perform parameterized create/read/update/delete, filter/sort/limit a result set, join related tables, and aggregate with `GROUP BY`;
- read a beginner-level `EXPLAIN QUERY PLAN` and see a table scan become an index search;
- generate monotonic integer ids and wrap every mutation in exactly one transaction with an explicit rollback path;
- recognize SQLite's single local file, one-writer-at-a-time boundary, and initialize schema safely on every startup;
- state why `Microsoft.Data.Sqlite`'s async ADO.NET methods still run synchronously, and choose the honest synchronous API on purpose.

## ✅ Prerequisites

You should already be comfortable with methods and exceptions ([Lesson 05](../05-methods-errors-and-debugging/README.md)), designing types that protect their own invariants ([Lesson 07](../07-modeling-data-and-behavior/README.md)), safe file paths and the file/JSON pipeline ([Lesson 11](../11-files-streams-and-json/README.md)), and owning `IDisposable` resources. [Lesson 12](../12-async-cancellation-and-concurrency/README.md)'s async/await material is assumed background, because this lesson explicitly contrasts SQLite with it.

## 🧠 Causal mental model

A relational database stores facts as **rows** inside **tables**. A table is a fixed set of named, typed columns, and every row must satisfy the table's declared shape - that shape is the schema.

A **primary key** identifies exactly one row. A **foreign key** in another table points back at that primary key, which is how "this loan belongs to that book and that member" becomes something the database enforces, not a comment you hope stays true. `NOT NULL`, `UNIQUE`, and `CHECK` push validation into the database itself: no code path, present or future, can quietly write a row that violates them.

SQLite is dynamically typed per value. A column declares a **type affinity** (`TEXT`, `INTEGER`, `REAL`, `NUMERIC`, or `BLOB`) that nudges a stored value toward that type, but SQLite will still store a value of a different type if your program lets it slip through. Affinity is a hint the engine applies during storage, not a runtime guarantee the way a strict column type is in many other database engines - the discipline still has to come from your own parameter types and constraints.

A `SqliteConnection` is a handle to one local database file. A `SqliteCommand` is one SQL statement plus its parameters. A `SqliteDataReader` is a forward-only cursor over the rows a query produced. All three - and any transaction you open - are `IDisposable`. You own their lifetime exactly like the streams from Lesson 11: open what you need, dispose it when you are done, and never leave a connection or reader hanging.

**Named parameters** (`$title`, `$isbn`) keep values completely out of the SQL text, so a title that happens to contain a quote or a semicolon can never be reinterpreted as SQL. Every mutation - insert, update, delete - belongs inside exactly **one transaction**: begin, perform the write(s), commit. If anything fails, roll back so the database never freezes in a half-written state. `INTEGER PRIMARY KEY` columns hand out fresh, monotonically increasing ids as you insert; you read the one you just created with `last_insert_rowid()` inside the same transaction.

SQLite is a local file, not a server: one connection writes at a time, and a second writer waits for the first to finish (a `busy_timeout` controls how long it waits before it reports `SQLITE_BUSY`, "database is locked"). There is no concurrent-writer scaling story here, and this lesson does not pretend otherwise - schema initialization, indexing, and locking behavior stay at a beginner depth, with no ORM, no migration tool, no admin console, and no production tuning advice.

Finally, an honest limitation: **SQLite has no asynchronous I/O**, so `Microsoft.Data.Sqlite`'s `OpenAsync`, `ExecuteReaderAsync`, and similar methods run their synchronous counterparts to completion and wrap the result in an already-finished `Task`. Awaiting them does not free a thread the way awaiting a real async file or network operation does. This lesson therefore uses the synchronous `Open`, `ExecuteNonQuery`, `ExecuteScalar`, and `ExecuteReader` APIs throughout - that is the honest choice, not a shortcut. Do not "fix" this by wrapping the synchronous calls in `Task.Run`: that only spends a thread-pool thread to look async while blocking it the whole time, which is worse than simply being synchronous.

## 🔤 Authentic fragments

Parameterized insert, reading back a generated id, inside one transaction:

```csharp
using SqliteTransaction transaction = connection.BeginTransaction();
try
{
    using SqliteCommand command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = """
        INSERT INTO Books (Title, Author, Isbn) VALUES ($title, $author, $isbn);
        SELECT last_insert_rowid();
        """;
    command.Parameters.AddWithValue("$title", title);
    command.Parameters.AddWithValue("$author", author);
    command.Parameters.AddWithValue("$isbn", isbn);

    long newId = (long)command.ExecuteScalar()!;
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

Explicit row mapping with null handling:

```csharp
using SqliteDataReader reader = command.ExecuteReader();
while (reader.Read())
{
    int id = reader.GetInt32(reader.GetOrdinal("BookId"));
    string title = reader.GetString(reader.GetOrdinal("Title"));

    int ratingOrdinal = reader.GetOrdinal("Rating");
    int? rating = reader.IsDBNull(ratingOrdinal) ? null : reader.GetInt32(ratingOrdinal);
}
```

A join with a grouped aggregate:

```sql
SELECT m.Name, COUNT(*) AS ActiveLoans
FROM Loans l
JOIN Members m ON m.MemberId = l.MemberId
WHERE l.ReturnedOnUtc IS NULL
GROUP BY m.Name
ORDER BY ActiveLoans DESC;
```

Reading a query plan before and after an index:

```csharp
using SqliteCommand plan = connection.CreateCommand();
plan.CommandText = "EXPLAIN QUERY PLAN SELECT LoanId FROM Loans WHERE BookId = $bookId;";
plan.Parameters.AddWithValue("$bookId", 1);
using SqliteDataReader reader = plan.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine(reader.GetString(reader.GetOrdinal("detail")));
    // Before an index: "SCAN Loans"
    // After CREATE INDEX ix_Loans_BookId ON Loans (BookId):
    //   "SEARCH Loans USING COVERING INDEX ix_Loans_BookId (BookId=?)"
}
```

## ▶️ Demonstration project

Run the demonstration from the repository root:

```bash
dotnet run --project lessons/13-sql-and-sqlite/LibraryCatalogSample/LibraryCatalogSample.csproj
```

Expected behavior:

- creates `Books`, `Members`, and `Loans` tables with primary keys, foreign keys, `NOT NULL`, `UNIQUE`, and a `CHECK` constraint, in a fresh temporary database file next to the build output;
- seeds books, members, and loans inside one transaction;
- lists the most recent active (not yet returned) loans through a filtered, ordered, limited join query;
- prints active loan counts per member through a grouped aggregate;
- rolls back a canceled checkout on purpose, then rolls back a checkout rejected by a `CHECK` constraint, proving neither left a row behind;
- prints `EXPLAIN QUERY PLAN` for the same query before and after adding an index, so a table scan turns into an index search;
- deletes the temporary database file before exiting, so nothing generated is left on disk.

## 🧪 Practice contract

Starter feedback (the default):

```bash
dotnet test --project exercises/13-sql-and-sqlite/tests/LibraryCatalogPractice.Tests/LibraryCatalogPractice.Tests.csproj
```

Reference solution tests:

```bash
dotnet test --project exercises/13-sql-and-sqlite/tests/LibraryCatalogPractice.Tests/LibraryCatalogPractice.Tests.csproj -p:CourseImplementation=Solution
```

Your implementation must make these statements true:

1. `BookRepository` creates the `Books` table - with its primary key, `NOT NULL`, `UNIQUE`, and `CHECK` constraints - the first time it opens a database file.
2. `Create` runs inside one transaction, rejects invalid rows instead of storing them, and returns a fresh, monotonically increasing id.
3. `GetById` returns the matching book, or `null` when no row matches - never an exception for a missing id.
4. `List` orders by title, honors an optional minimum-year filter, and never returns more rows than the requested limit.
5. `UpdateRating` runs inside one transaction and reports whether a row actually matched, instead of silently doing nothing.
6. `Dispose` releases the owned connection exactly once, safely, even if it is called more than once.

Deterministic feedback:

- a missing or incomplete schema fails every test that touches the database;
- accepting a duplicate ISBN, an impossible publication year, or an out-of-range rating fails the constraint tests;
- treating a missing id as an error (or as silent success) fails the missing-row tests;
- forgetting `Dispose` idempotence fails the disposal test.

## 🧩 Experiment

1. Remove the `CHECK` constraint on `Rating` from the schema and try to store a rating of `10` - notice the database no longer stops you.
2. Change the manual rollback in the sample to a commit and rerun it - watch the "before/after" loan counts stop matching.
3. Drop the `CREATE INDEX` statement and rerun the query-plan demonstration - the plan goes back to a full table scan.
4. Try to build the sample's queries by concatenating a title string directly into `CommandText` instead of using a parameter, and explain to yourself why that is dangerous even though it "still works" for ordinary titles.

## ⚠️ Common mistakes and diagnosis

- **Mistake:** building SQL by concatenating parameter values into the command text.
  **Diagnosis:** ordinary-looking data with a quote or a keyword in it silently changes what the statement does; named parameters remove the whole category of bug.

- **Mistake:** forgetting to wrap an insert or update in a transaction, or committing several unrelated mutations together.
  **Diagnosis:** a crash partway through leaves the database with some writes applied and others missing.

- **Mistake:** reading a nullable column with `GetInt32` instead of checking `IsDBNull` first.
  **Diagnosis:** an `InvalidCastException` from `SqliteDataReader` the first time a real row has a `NULL` in that column.

- **Mistake:** treating a missing row from `GetById` as an exception instead of a `null` return value.
  **Diagnosis:** ordinary "not found" lookups start crashing the caller instead of letting it decide what "not found" means.

- **Mistake:** wrapping `Microsoft.Data.Sqlite`'s async methods in `Task.Run` to "get concurrency".
  **Diagnosis:** a thread-pool thread blocks for the entire synchronous call anyway - no throughput is gained, and one is arguably wasted.

- **Mistake:** never disposing the connection, or disposing it more than once without guarding for that.
  **Diagnosis:** the database file stays locked longer than it should, or a second `Dispose` call throws instead of doing nothing.

## 📝 Summary

A relational schema turns validation rules into enforced facts instead of hopeful comments. Parameters keep data out of SQL text, one transaction per mutation keeps the database honest when something fails, and an explicit row mapping keeps `NULL` from becoming a crash. `Microsoft.Data.Sqlite` is honest about being synchronous under the hood - use it that way.

## ❓ Review questions

1. What turns "this loan is about that book" from a comment into an enforced fact?
2. Why is a named parameter safer than building `CommandText` with string interpolation?
3. What should `GetById` return for a missing id, and why should it not throw?
4. Why does wrapping every mutation in one transaction matter even for a single `INSERT`?
5. Why do `Microsoft.Data.Sqlite`'s async methods not give you real asynchronous I/O, and what should you do instead?

## 📚 Official Microsoft Learn links

- [Microsoft.Data.Sqlite overview](https://learn.microsoft.com/dotnet/standard/data/sqlite/)
- [Async limitations in Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/async)
- [Connection strings for Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/connection-strings)
- [Data types - Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/types)
- [ADO.NET limitations - Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/adonet-limitations)
