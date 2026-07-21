using Microsoft.Data.Sqlite;

namespace LearningCSharp.Course.Unit13.Practice;

/// <summary>
/// Owns one SQLite connection to a local library-catalog database. Every
/// mutating method should run inside its own transaction; every read should
/// use a parameterized command and an explicit, ordinal-based row mapping.
/// </summary>
public sealed class BookRepository : IDisposable
{
    private readonly SqliteConnection _connection;

    public BookRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();
        InitializeSchema();
    }

    public int Create(string title, string author, string isbn, int publicationYear, int? rating = null)
    {
        // TODO: Implement Create so it runs the insert inside its own transaction,
        // passing every value as a parameter, returns the newly generated row id,
        // and lets constraint violations (duplicate ISBN, out-of-range year or
        // rating) surface from the database.
        throw new NotImplementedException("TODO: Insert a book inside one transaction and return its generated id.");
    }

    public Book? GetById(int id)
    {
        // TODO: Implement GetById so it queries by id with a parameter, maps the
        // columns by ordinal, and returns null when no row matches.
        throw new NotImplementedException("TODO: Read one book by id, or return null when no row matches.");
    }

    public IReadOnlyList<Book> List(int? minimumPublicationYear = null, int limit = 100)
    {
        // TODO: Implement List so it orders results by title, applies the optional
        // minimum-year filter and the row limit through parameters, maps each row
        // explicitly, and returns the collection.
        throw new NotImplementedException("TODO: List books ordered by title, honoring the year filter and the limit.");
    }

    public bool UpdateRating(int id, int? rating)
    {
        // TODO: Implement UpdateRating so it updates the rating inside a
        // transaction using parameters, reports whether a row actually matched,
        // and lets an invalid rating be rejected by the database.
        throw new NotImplementedException("TODO: Update the rating inside one transaction and report whether a row matched.");
    }

    public void Dispose()
    {
        // TODO: Implement Dispose so it disposes the owned connection and makes
        // repeated Dispose calls safe (idempotent).
        throw new NotImplementedException("TODO: Dispose the owned connection exactly once, even if Dispose is called twice.");
    }

    private void InitializeSchema()
    {
        // TODO: Implement InitializeSchema so it creates the Books table with a
        // generated primary key, NOT NULL columns, a UNIQUE ISBN, and CHECK
        // constraints that bound the publication year and the rating range.
        throw new NotImplementedException("TODO: Create the Books table with its primary key, NOT NULL, UNIQUE, and CHECK constraints.");
    }
}
