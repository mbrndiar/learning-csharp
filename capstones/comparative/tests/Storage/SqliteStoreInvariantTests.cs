using ComparativeKv.Core;
using ComparativeKv.Storage.Sqlite;
using ComparativeKv.Tests.Support;
using Microsoft.Data.Sqlite;

namespace ComparativeKv.Tests.Storage;

public sealed class SqliteStoreInvariantTests
{
    [Fact]
    public void OpeningAPathWithNoParentMapsToOpenStorageFailure()
    {
        using var scenario = new ScenarioDirectory("missing-storage-parent");
        var path = Path.Combine(scenario.MissingParentPath, "store.db");

        AssertStorageError(path, "storage_error", "storage_failure");
    }

    [Fact]
    public void OpeningV1RejectsInvalidKeysAndNonNormalizedValues()
    {
        using (var invalidKey = new ScenarioDirectory("invalid-v1-key"))
        {
            CreateV1(
                invalidKey.DatabasePath,
                """
                INSERT INTO store_metadata(singleton, schema_version, global_revision)
                VALUES (1, 1, 1);
                INSERT INTO entries(key, value_json, revision)
                VALUES ('-bad', 'null', 1);
                """);
            AssertStorageError(invalidKey.DatabasePath, "invalid_storage", "invalid_key");
        }

        using var invalidValue = new ScenarioDirectory("invalid-v1-value");
        CreateV1(
            invalidValue.DatabasePath,
            """
            INSERT INTO store_metadata(singleton, schema_version, global_revision)
            VALUES (1, 1, 1);
            INSERT INTO entries(key, value_json, revision)
            VALUES ('good', '1.0', 1);
            """);
        AssertStorageError(invalidValue.DatabasePath, "invalid_storage", "invalid_value");
    }

    [Fact]
    public void OpeningV1RejectsRevisionAndPragmaInvariants()
    {
        using (var revision = new ScenarioDirectory("invalid-v1-revision"))
        {
            CreateV1(
                revision.DatabasePath,
                """
                PRAGMA ignore_check_constraints = ON;
                INSERT INTO store_metadata(singleton, schema_version, global_revision)
                VALUES (1, 1, 0);
                INSERT INTO entries(key, value_json, revision)
                VALUES ('good', 'null', 1);
                """);
            AssertStorageError(revision.DatabasePath, "invalid_storage", "revision_invariant");
        }

        using var pragma = new ScenarioDirectory("invalid-v1-pragma");
        CreateV1(
            pragma.DatabasePath,
            """
            INSERT INTO store_metadata(singleton, schema_version, global_revision)
            VALUES (1, 1, 0);
            PRAGMA user_version = 1;
            """);
        AssertStorageError(pragma.DatabasePath, "invalid_storage", "malformed_schema");
    }

    [Fact]
    public void DisposedStoresRejectFurtherOperationsAndDisposeIdempotently()
    {
        using var scenario = new ScenarioDirectory("disposed-store");
        var store = SqliteConfigurationStore.Open(scenario.DatabasePath);
        store.Dispose();
        store.Dispose();

        Assert.Throws<ObjectDisposedException>(() => store.ListEntries());
    }

    private static void CreateV1(string path, string seedSql)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        Execute(
            connection,
            """
            CREATE TABLE store_metadata (
                singleton       INTEGER PRIMARY KEY CHECK (singleton = 1),
                schema_version  INTEGER NOT NULL CHECK (schema_version = 1),
                global_revision INTEGER NOT NULL
                                CHECK (global_revision BETWEEN 0 AND 9007199254740991)
            )
            """);
        Execute(
            connection,
            """
            CREATE TABLE entries (
                key        TEXT PRIMARY KEY COLLATE BINARY,
                value_json TEXT NOT NULL CHECK (json_valid(value_json)),
                revision   INTEGER NOT NULL
                           CHECK (revision BETWEEN 1 AND 9007199254740991)
            )
            """);
        foreach (var statement in seedSql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            Execute(connection, statement);
        }
    }

    private static void AssertStorageError(string path, string category, string reason)
    {
        var exception = Assert.Throws<KvException>(() => SqliteConfigurationStore.Open(path));
        Assert.Equal(category, exception.Category);
        Assert.Equal(reason, exception.Details["reason"]);
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
