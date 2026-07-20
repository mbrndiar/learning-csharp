using Microsoft.Data.Sqlite;

// A deterministic, disposable database file next to the build output. Every
// run starts from the same clean state and every run deletes what it
// created, so nothing generated ends up tracked in source control.
string databaseDirectory = Path.Combine(AppContext.BaseDirectory, "generated");
string databasePath = Path.Combine(databaseDirectory, "library-catalog.db");
Directory.CreateDirectory(databaseDirectory);
DeleteDatabaseFiles(databasePath);

try
{
    using SqliteConnection connection = new($"Data Source={databasePath}");
    connection.Open();

    // SQLite enforces foreign keys only when a connection asks it to, and it
    // is a local single-file database: a second writer waits for the first,
    // which is why a busy timeout matters even without a server involved.
    RunPragma(connection, "PRAGMA foreign_keys = ON;");
    RunPragma(connection, "PRAGMA busy_timeout = 2000;");

    CreateSchema(connection);
    SeedCatalog(connection);

    Console.WriteLine("== Active loans (filter + join + order + limit) ==");
    PrintActiveLoans(connection, limit: 3);

    Console.WriteLine();
    Console.WriteLine("== Active loans per member (grouped aggregate) ==");
    PrintActiveLoanCountsByMember(connection);

    Console.WriteLine();
    Console.WriteLine("== Transaction rollback: a canceled checkout ==");
    DemonstrateManualRollback(connection);

    Console.WriteLine();
    Console.WriteLine("== Transaction rollback: a rejected checkout ==");
    DemonstrateConstraintRollback(connection);

    Console.WriteLine();
    Console.WriteLine("== EXPLAIN QUERY PLAN before and after an index ==");
    DemonstrateQueryPlan(connection);
}
finally
{
    DeleteDatabaseFiles(databasePath);
    Console.WriteLine();
    Console.WriteLine($"Cleaned up the temporary database at {databasePath}");
}

static void RunPragma(SqliteConnection connection, string pragmaText)
{
    using SqliteCommand command = connection.CreateCommand();
    command.CommandText = pragmaText;
    command.ExecuteNonQuery();
}

static void CreateSchema(SqliteConnection connection)
{
    // Primary keys identify rows, foreign keys tie a loan to exactly one
    // book and one member, NOT NULL/CHECK/UNIQUE stop broken rows before they
    // are ever stored, and INTEGER PRIMARY KEY gives every insert a fresh,
    // monotonically increasing id.
    const string CreateBooks = """
        CREATE TABLE IF NOT EXISTS Books (
            BookId INTEGER PRIMARY KEY AUTOINCREMENT,
            Title  TEXT NOT NULL,
            Author TEXT NOT NULL,
            Isbn   TEXT NOT NULL UNIQUE
        );
        """;

    const string CreateMembers = """
        CREATE TABLE IF NOT EXISTS Members (
            MemberId INTEGER PRIMARY KEY AUTOINCREMENT,
            Name     TEXT NOT NULL
        );
        """;

    const string CreateLoans = """
        CREATE TABLE IF NOT EXISTS Loans (
            LoanId        INTEGER PRIMARY KEY AUTOINCREMENT,
            BookId        INTEGER NOT NULL REFERENCES Books (BookId),
            MemberId      INTEGER NOT NULL REFERENCES Members (MemberId),
            LoanedOnUtc   TEXT NOT NULL,
            ReturnedOnUtc TEXT NULL,
            CHECK (ReturnedOnUtc IS NULL OR ReturnedOnUtc >= LoanedOnUtc)
        );
        """;

    foreach (string statement in new[] { CreateBooks, CreateMembers, CreateLoans })
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = statement;
        command.ExecuteNonQuery();
    }
}

static void SeedCatalog(SqliteConnection connection)
{
    // Seeding the whole catalog is one mutation, so it runs inside exactly
    // one transaction: every insert commits together, or none of them do.
    using SqliteTransaction transaction = connection.BeginTransaction();
    try
    {
        long clockworkId = InsertBook(connection, transaction, "The Clockwork Library", "R. Adeyemi", "978-0-00-000001-1");
        long tidesId = InsertBook(connection, transaction, "Tides of Pearl Bay", "S. Marchetti", "978-0-00-000002-2");
        long groveId = InsertBook(connection, transaction, "Notes from the Silent Grove", "J. Okafor", "978-0-00-000003-3");

        long avaId = InsertMember(connection, transaction, "Ava Novak");
        long benId = InsertMember(connection, transaction, "Ben Ortiz");

        InsertLoan(connection, transaction, clockworkId, avaId, "2024-01-05", returnedOnUtc: "2024-01-19");
        InsertLoan(connection, transaction, tidesId, avaId, "2024-02-01", returnedOnUtc: null);
        InsertLoan(connection, transaction, groveId, benId, "2024-02-10", returnedOnUtc: null);

        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}

static long InsertBook(SqliteConnection connection, SqliteTransaction transaction, string title, string author, string isbn)
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
    return (long)command.ExecuteScalar()!;
}

static long InsertMember(SqliteConnection connection, SqliteTransaction transaction, string name)
{
    using SqliteCommand command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = """
        INSERT INTO Members (Name) VALUES ($name);
        SELECT last_insert_rowid();
        """;
    command.Parameters.AddWithValue("$name", name);
    return (long)command.ExecuteScalar()!;
}

static long InsertLoan(SqliteConnection connection, SqliteTransaction transaction, long bookId, long memberId, string loanedOnUtc, string? returnedOnUtc)
{
    using SqliteCommand command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = """
        INSERT INTO Loans (BookId, MemberId, LoanedOnUtc, ReturnedOnUtc) VALUES ($bookId, $memberId, $loanedOn, $returnedOn);
        SELECT last_insert_rowid();
        """;
    command.Parameters.AddWithValue("$bookId", bookId);
    command.Parameters.AddWithValue("$memberId", memberId);
    command.Parameters.AddWithValue("$loanedOn", loanedOnUtc);
    command.Parameters.AddWithValue("$returnedOn", (object?)returnedOnUtc ?? DBNull.Value);
    return (long)command.ExecuteScalar()!;
}

static void PrintActiveLoans(SqliteConnection connection, int limit)
{
    using SqliteCommand command = connection.CreateCommand();
    command.CommandText = """
        SELECT b.Title, m.Name, l.LoanedOnUtc
        FROM Loans l
        JOIN Books b ON b.BookId = l.BookId
        JOIN Members m ON m.MemberId = l.MemberId
        WHERE l.ReturnedOnUtc IS NULL
        ORDER BY l.LoanedOnUtc DESC
        LIMIT $limit;
        """;
    command.Parameters.AddWithValue("$limit", limit);

    using SqliteDataReader reader = command.ExecuteReader();
    while (reader.Read())
    {
        // Explicit row mapping by ordinal, with the reader's own column
        // lookup instead of guessing string names.
        string title = reader.GetString(reader.GetOrdinal("Title"));
        string memberName = reader.GetString(reader.GetOrdinal("Name"));
        string loanedOn = reader.GetString(reader.GetOrdinal("LoanedOnUtc"));
        Console.WriteLine($"- {title} -> {memberName} (loaned {loanedOn})");
    }
}

static void PrintActiveLoanCountsByMember(SqliteConnection connection)
{
    using SqliteCommand command = connection.CreateCommand();
    command.CommandText = """
        SELECT m.Name, COUNT(*) AS ActiveLoans
        FROM Loans l
        JOIN Members m ON m.MemberId = l.MemberId
        WHERE l.ReturnedOnUtc IS NULL
        GROUP BY m.Name
        ORDER BY ActiveLoans DESC, m.Name;
        """;

    using SqliteDataReader reader = command.ExecuteReader();
    while (reader.Read())
    {
        string memberName = reader.GetString(reader.GetOrdinal("Name"));
        long activeLoans = reader.GetInt64(reader.GetOrdinal("ActiveLoans"));
        Console.WriteLine($"- {memberName}: {activeLoans} active loan(s)");
    }
}

static void DemonstrateManualRollback(SqliteConnection connection)
{
    long countBefore = CountLoans(connection);

    using (SqliteTransaction transaction = connection.BeginTransaction())
    {
        InsertLoan(connection, transaction, bookId: 1, memberId: 2, loanedOnUtc: "2024-03-01", returnedOnUtc: null);

        // The member changed their mind before leaving the desk: roll back
        // instead of committing. No row from this transaction is kept.
        transaction.Rollback();
    }

    long countAfter = CountLoans(connection);
    Console.WriteLine($"Loan count before: {countBefore}, after a rolled-back checkout: {countAfter}");
}

static void DemonstrateConstraintRollback(SqliteConnection connection)
{
    long countBefore = CountLoans(connection);

    using SqliteTransaction transaction = connection.BeginTransaction();
    try
    {
        // ReturnedOnUtc earlier than LoanedOnUtc violates the CHECK
        // constraint, so this statement throws and nothing commits.
        InsertLoan(connection, transaction, bookId: 1, memberId: 2, loanedOnUtc: "2024-03-01", returnedOnUtc: "2024-02-01");
        transaction.Commit();
    }
    catch (SqliteException exception)
    {
        transaction.Rollback();
        Console.WriteLine($"Rejected by the database: {exception.Message}");
    }

    long countAfter = CountLoans(connection);
    Console.WriteLine($"Loan count before: {countBefore}, after a rejected checkout: {countAfter}");
}

static long CountLoans(SqliteConnection connection)
{
    using SqliteCommand command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM Loans;";
    return (long)command.ExecuteScalar()!;
}

static void DemonstrateQueryPlan(SqliteConnection connection)
{
    const string FindLoansForBook = "SELECT LoanId FROM Loans WHERE BookId = $bookId;";

    Console.WriteLine("Before an index:");
    PrintQueryPlan(connection, FindLoansForBook);

    using (SqliteCommand createIndex = connection.CreateCommand())
    {
        createIndex.CommandText = "CREATE INDEX IF NOT EXISTS ix_Loans_BookId ON Loans (BookId);";
        createIndex.ExecuteNonQuery();
    }

    Console.WriteLine("After an index on Loans(BookId):");
    PrintQueryPlan(connection, FindLoansForBook);
}

static void PrintQueryPlan(SqliteConnection connection, string commandText)
{
    using SqliteCommand command = connection.CreateCommand();
    command.CommandText = $"EXPLAIN QUERY PLAN {commandText}";
    command.Parameters.AddWithValue("$bookId", 1);

    using SqliteDataReader reader = command.ExecuteReader();
    while (reader.Read())
    {
        string detail = reader.GetString(reader.GetOrdinal("detail"));
        Console.WriteLine($"  {detail}");
    }
}

static void DeleteDatabaseFiles(string databasePath)
{
    foreach (string suffix in new[] { string.Empty, "-wal", "-shm", "-journal" })
    {
        string candidate = databasePath + suffix;
        if (File.Exists(candidate))
        {
            File.Delete(candidate);
        }
    }
}
