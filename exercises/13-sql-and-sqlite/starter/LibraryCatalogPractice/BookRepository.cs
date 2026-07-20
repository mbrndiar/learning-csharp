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
        throw new NotImplementedException("TODO: Insert a book inside one transaction and return its generated id.");
    }

    public Book? GetById(int id)
    {
        throw new NotImplementedException("TODO: Read one book by id, or return null when no row matches.");
    }

    public IReadOnlyList<Book> List(int? minimumPublicationYear = null, int limit = 100)
    {
        throw new NotImplementedException("TODO: List books ordered by title, honoring the year filter and the limit.");
    }

    public bool UpdateRating(int id, int? rating)
    {
        throw new NotImplementedException("TODO: Update the rating inside one transaction and report whether a row matched.");
    }

    public void Dispose()
    {
        throw new NotImplementedException("TODO: Dispose the owned connection exactly once, even if Dispose is called twice.");
    }

    private void InitializeSchema()
    {
        throw new NotImplementedException("TODO: Create the Books table with its primary key, NOT NULL, UNIQUE, and CHECK constraints.");
    }
}
