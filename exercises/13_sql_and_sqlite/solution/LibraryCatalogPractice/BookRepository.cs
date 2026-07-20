using Microsoft.Data.Sqlite;

namespace LearningCSharp.Course.Unit13.Practice;

/// <summary>
/// Owns one SQLite connection to a local library-catalog database. Every
/// mutating method runs inside its own transaction; every read uses a
/// parameterized command and an explicit, ordinal-based row mapping.
/// </summary>
public sealed class BookRepository : IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public BookRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();
        InitializeSchema();
    }

    public int Create(string title, string author, string isbn, int publicationYear, int? rating = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(author);
        ArgumentException.ThrowIfNullOrWhiteSpace(isbn);

        using SqliteTransaction transaction = _connection.BeginTransaction();
        try
        {
            using SqliteCommand command = _connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Books (Title, Author, Isbn, PublicationYear, Rating)
                VALUES ($title, $author, $isbn, $publicationYear, $rating);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$title", title);
            command.Parameters.AddWithValue("$author", author);
            command.Parameters.AddWithValue("$isbn", isbn);
            command.Parameters.AddWithValue("$publicationYear", publicationYear);
            command.Parameters.AddWithValue("$rating", (object?)rating ?? DBNull.Value);

            long newId = (long)command.ExecuteScalar()!;
            transaction.Commit();
            return checked((int)newId);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Book? GetById(int id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = """
            SELECT BookId, Title, Author, Isbn, PublicationYear, Rating
            FROM Books
            WHERE BookId = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read() ? MapBook(reader) : null;
    }

    public IReadOnlyList<Book> List(int? minimumPublicationYear = null, int limit = 100)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(limit, 0);

        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = """
            SELECT BookId, Title, Author, Isbn, PublicationYear, Rating
            FROM Books
            WHERE $minimumPublicationYear IS NULL OR PublicationYear >= $minimumPublicationYear
            ORDER BY Title
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$minimumPublicationYear", (object?)minimumPublicationYear ?? DBNull.Value);
        command.Parameters.AddWithValue("$limit", limit);

        List<Book> books = [];
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            books.Add(MapBook(reader));
        }

        return books;
    }

    public bool UpdateRating(int id, int? rating)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using SqliteTransaction transaction = _connection.BeginTransaction();
        try
        {
            using SqliteCommand command = _connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE Books
                SET Rating = $rating
                WHERE BookId = $id;
                """;
            command.Parameters.AddWithValue("$rating", (object?)rating ?? DBNull.Value);
            command.Parameters.AddWithValue("$id", id);

            int affectedRows = command.ExecuteNonQuery();
            transaction.Commit();
            return affectedRows > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _connection.Dispose();
        _disposed = true;
    }

    private static Book MapBook(SqliteDataReader reader)
    {
        int id = reader.GetInt32(reader.GetOrdinal("BookId"));
        string title = reader.GetString(reader.GetOrdinal("Title"));
        string author = reader.GetString(reader.GetOrdinal("Author"));
        string isbn = reader.GetString(reader.GetOrdinal("Isbn"));
        int publicationYear = reader.GetInt32(reader.GetOrdinal("PublicationYear"));

        int ratingOrdinal = reader.GetOrdinal("Rating");
        int? rating = reader.IsDBNull(ratingOrdinal) ? null : reader.GetInt32(ratingOrdinal);

        return new Book(id, title, author, isbn, publicationYear, rating);
    }

    private void InitializeSchema()
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Books (
                BookId          INTEGER PRIMARY KEY AUTOINCREMENT,
                Title           TEXT NOT NULL,
                Author          TEXT NOT NULL,
                Isbn            TEXT NOT NULL UNIQUE,
                PublicationYear INTEGER NOT NULL CHECK (PublicationYear >= 1450),
                Rating          INTEGER NULL CHECK (Rating IS NULL OR Rating BETWEEN 0 AND 5)
            );
            """;
        command.ExecuteNonQuery();
    }
}
