using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using ComparativeKv.Application;
using ComparativeKv.Core;
using Microsoft.Data.Sqlite;

namespace ComparativeKv.Storage.Sqlite;

public sealed class SqliteConfigurationStore : IConfigurationStore
{
    private const string CreateMetadata = """
        CREATE TABLE store_metadata (
            singleton       INTEGER PRIMARY KEY CHECK (singleton = 1),
            schema_version  INTEGER NOT NULL CHECK (schema_version = 1),
            global_revision INTEGER NOT NULL
                            CHECK (global_revision BETWEEN 0 AND 9007199254740991)
        )
        """;

    private const string CreateEntries = """
        CREATE TABLE entries (
            key        TEXT PRIMARY KEY COLLATE BINARY,
            value_json TEXT NOT NULL CHECK (json_valid(value_json)),
            revision   INTEGER NOT NULL
                       CHECK (revision BETWEEN 1 AND 9007199254740991)
        )
        """;

    private const string InsertMetadata = """
        INSERT INTO store_metadata(singleton, schema_version, global_revision)
        VALUES (1, 1, 0)
        """;

    private const string V0Entries = "createtableentries(keytextprimarykeycollatebinary,value_jsontextnotnull)";
    private const string V1Entries = "createtableentries(keytextprimarykeycollatebinary,value_jsontextnotnullcheck(json_valid(value_json)),revisionintegernotnullcheck(revisionbetween1and9007199254740991))";
    private const string V1Metadata = "createtablestore_metadata(singletonintegerprimarykeycheck(singleton=1),schema_versionintegernotnullcheck(schema_version=1),global_revisionintegernotnullcheck(global_revisionbetween0and9007199254740991))";

    private readonly SqliteConnection connection;
    private bool transactionOpen;
    private bool disposed;

    private SqliteConfigurationStore(SqliteConnection connection)
    {
        this.connection = connection;
    }

    public static SqliteConfigurationStore Open(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        EnsureExistingParent(path);

        SqliteConnection? connection = null;
        try
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Default,
                DefaultTimeout = 10,
                Pooling = false,
            };
            connection = new SqliteConnection(builder.ConnectionString);
            connection.Open();
        }
        catch (SqliteException exception)
        {
            connection?.Dispose();
            throw MapSqliteException(exception, "open");
        }
        catch (Exception) when (connection is null)
        {
            throw KvException.StorageFailure("open");
        }

        var store = new SqliteConfigurationStore(connection);
        try
        {
            store.Configure();
            store.PrepareSchema();
            return store;
        }
        catch
        {
            store.Dispose();
            throw;
        }
    }

    public SetResult SetValue(string key, JsonValue value, SetExpectation expectation)
    {
        ThrowIfDisposed();
        const string operation = "write";
        try
        {
            BeginImmediate();
            var current = ReadCurrentRevision(key);
            if (expectation.Kind == ExpectationKind.Absent && current is not null)
            {
                throw KvException.Conflict(key, "absent", current);
            }

            if (expectation.Kind == ExpectationKind.ExactRevision && current != expectation.Revision)
            {
                throw KvException.Conflict(key, expectation.Revision!.Value, current);
            }

            var revision = NextRevision();
            using (var command = CreateCommand(
                       """
                       INSERT INTO entries(key, value_json, revision)
                       VALUES ($key, $value, $revision)
                       ON CONFLICT(key) DO UPDATE SET
                           value_json = excluded.value_json,
                           revision = excluded.revision
                       """))
            {
                command.Parameters.AddWithValue("$key", key);
                command.Parameters.AddWithValue("$value", value.ToCompactJson());
                command.Parameters.AddWithValue("$revision", revision);
                command.ExecuteNonQuery();
            }

            UpdateGlobalRevision(revision);
            Commit();
            return new SetResult(new Entry(key, value, revision), current is null);
        }
        catch (KvException)
        {
            RollbackIfNeeded();
            throw;
        }
        catch (SqliteException exception)
        {
            RollbackIfNeeded();
            throw MapSqliteException(exception, operation);
        }
        catch (Exception)
        {
            RollbackIfNeeded();
            throw KvException.StorageFailure(operation);
        }
    }

    public Entry GetValue(string key)
    {
        ThrowIfDisposed();
        try
        {
            using var command = CreateCommand(
                """
                SELECT value_json, revision
                FROM entries
                WHERE key = $key COLLATE BINARY
                """);
            command.Parameters.AddWithValue("$key", key);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                throw KvException.NotFound(key);
            }

            var value = ReadStoredValue(reader, ordinal: 0, key);
            var revision = RequirePositiveRevision(ReadInteger(reader, ordinal: 1));
            return new Entry(key, value, revision);
        }
        catch (KvException)
        {
            throw;
        }
        catch (SqliteException exception)
        {
            throw MapSqliteException(exception, "read");
        }
        catch (Exception)
        {
            throw KvException.StorageFailure("read");
        }
    }

    public DeleteResult DeleteValue(string key, DeleteExpectation expectation)
    {
        ThrowIfDisposed();
        const string operation = "write";
        try
        {
            BeginImmediate();
            var current = ReadCurrentRevision(key);
            if (current is null)
            {
                throw KvException.NotFound(key);
            }

            if (expectation.Kind == ExpectationKind.ExactRevision && current != expectation.Revision)
            {
                throw KvException.Conflict(key, expectation.Revision!.Value, current);
            }

            var revision = NextRevision();
            using (var command = CreateCommand("DELETE FROM entries WHERE key = $key COLLATE BINARY"))
            {
                command.Parameters.AddWithValue("$key", key);
                command.ExecuteNonQuery();
            }

            UpdateGlobalRevision(revision);
            Commit();
            return new DeleteResult(key, current.Value, revision);
        }
        catch (KvException)
        {
            RollbackIfNeeded();
            throw;
        }
        catch (SqliteException exception)
        {
            RollbackIfNeeded();
            throw MapSqliteException(exception, operation);
        }
        catch (Exception)
        {
            RollbackIfNeeded();
            throw KvException.StorageFailure(operation);
        }
    }

    public ListResult ListEntries()
    {
        ThrowIfDisposed();
        try
        {
            BeginDeferred();
            var globalRevision = ReadGlobalRevision();
            var entries = ReadEntries();
            Commit();
            return new ListResult(entries, globalRevision);
        }
        catch (KvException)
        {
            RollbackIfNeeded();
            throw;
        }
        catch (SqliteException exception)
        {
            RollbackIfNeeded();
            throw MapSqliteException(exception, "read");
        }
        catch (Exception)
        {
            RollbackIfNeeded();
            throw KvException.StorageFailure("read");
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        RollbackIfNeeded();
        connection.Dispose();
        disposed = true;
        GC.SuppressFinalize(this);
    }

    private void Configure()
    {
        try
        {
            ExecuteNonQuery($"PRAGMA busy_timeout = {KvLimits.BusyTimeoutMilliseconds}");
            ExecuteNonQuery("PRAGMA foreign_keys = ON");
        }
        catch (KvException)
        {
            throw;
        }
        catch (SqliteException exception)
        {
            throw MapSqliteException(exception, "configure");
        }
        catch (Exception)
        {
            throw KvException.StorageFailure("configure");
        }
    }

    private void ConfigureJournalMode()
    {
        try
        {
            var currentMode = ExecuteScalar("PRAGMA journal_mode");
            if (string.Equals(Convert.ToString(currentMode, CultureInfo.InvariantCulture), "wal", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var result = ExecuteScalar("PRAGMA journal_mode = WAL");
            if (!string.Equals(Convert.ToString(result, CultureInfo.InvariantCulture), "wal", StringComparison.OrdinalIgnoreCase))
            {
                throw KvException.StorageFailure("configure");
            }
        }
        catch (SqliteException exception) when (IsBusy(exception))
        {
            throw KvException.Busy();
        }
        catch (SqliteException)
        {
            throw KvException.StorageFailure("configure");
        }
    }

    private void PrepareSchema()
    {
        var operation = "initialize";
        try
        {
            // Reserve once before touching journal mode. When another process holds
            // a writer, this consumes the one normative busy interval instead of
            // waiting once for WAL configuration and once again for schema work.
            BeginImmediate();
            RollbackIfNeeded();
            ConfigureJournalMode();
            BeginImmediate();
            var objects = ReadApplicationObjects();
            var futureVersion = FindFutureSchemaVersion(objects);
            if (futureVersion is > KvLimits.SchemaVersion)
            {
                throw KvException.UnsupportedSchema(futureVersion.Value);
            }

            EnsureIntegrity();
            ValidateDefaultPragmas();
            switch (ClassifySchema(objects))
            {
                case SchemaKind.Fresh:
                    Initialize();
                    break;
                case SchemaKind.V0:
                    operation = "migrate";
                    MigrateV0();
                    break;
                case SchemaKind.V1:
                    operation = "read";
                    ValidateV1();
                    break;
                default:
                    throw KvException.InvalidStorage("malformed_schema");
            }

            operation = "commit";
            Commit();
        }
        catch (KvException)
        {
            RollbackIfNeeded();
            throw;
        }
        catch (SqliteException exception)
        {
            RollbackIfNeeded();
            throw MapSqliteException(exception, operation);
        }
        catch (Exception)
        {
            RollbackIfNeeded();
            throw KvException.StorageFailure(operation);
        }
    }

    private void Initialize()
    {
        ExecuteNonQuery(CreateMetadata);
        ExecuteNonQuery(CreateEntries);
        ExecuteNonQuery(InsertMetadata);
    }

    private void MigrateV0()
    {
        var normalized = new List<(string Key, string ValueJson)>();
        using (var command = CreateCommand(
                   """
                   SELECT key, value_json
                   FROM entries
                   ORDER BY key COLLATE BINARY
                   """))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var key = ReadText(reader, ordinal: 0);
                try
                {
                    KeyValueValidation.ValidateKey(key);
                }
                catch (KvException)
                {
                    throw KvException.InvalidStoredKey(key);
                }

                try
                {
                    var value = RestrictedJson.Parse(ReadText(reader, ordinal: 1));
                    normalized.Add((key, value.ToCompactJson()));
                }
                catch (KvException)
                {
                    throw KvException.InvalidStoredValue(key);
                }
            }
        }

        ExecuteNonQuery("ALTER TABLE entries RENAME TO entries_v0_migration");
        ExecuteNonQuery(CreateMetadata);
        ExecuteNonQuery(CreateEntries);
        ExecuteNonQuery(InsertMetadata);
        for (var index = 0; index < normalized.Count; index++)
        {
            var revision = index + 1L;
            using var insert = CreateCommand(
                "INSERT INTO entries(key, value_json, revision) VALUES ($key, $value, $revision)");
            insert.Parameters.AddWithValue("$key", normalized[index].Key);
            insert.Parameters.AddWithValue("$value", normalized[index].ValueJson);
            insert.Parameters.AddWithValue("$revision", revision);
            insert.ExecuteNonQuery();
        }

        using (var metadata = CreateCommand(
                   "UPDATE store_metadata SET global_revision = $revision WHERE singleton = 1"))
        {
            metadata.Parameters.AddWithValue("$revision", (long)normalized.Count);
            if (metadata.ExecuteNonQuery() != 1)
            {
                throw KvException.InvalidStorage("malformed_schema");
            }
        }

        ExecuteNonQuery("DROP TABLE entries_v0_migration");
    }

    private void ValidateV1()
    {
        var metadata = new List<(object Singleton, object Version, object Revision)>();
        using (var command = CreateCommand(
                   "SELECT singleton, schema_version, global_revision FROM store_metadata"))
        using (var metadataReader = command.ExecuteReader())
        {
            while (metadataReader.Read())
            {
                metadata.Add((metadataReader.GetValue(0), metadataReader.GetValue(1), metadataReader.GetValue(2)));
            }
        }

        if (metadata.Count != 1 ||
            metadata[0].Singleton is not long singleton ||
            metadata[0].Version is not long schemaVersion ||
            singleton != 1 ||
            schemaVersion != KvLimits.SchemaVersion)
        {
            throw KvException.InvalidStorage("malformed_schema");
        }

        if (metadata[0].Revision is not long globalRevision ||
            globalRevision is < 0 or > KvLimits.MaximumSafeInteger)
        {
            throw KvException.InvalidStorage("revision_invariant");
        }

        var seenRevisions = new HashSet<long>();
        using var entries = CreateCommand(
            """
            SELECT key, value_json, revision
            FROM entries
            ORDER BY key COLLATE BINARY
            """);
        using var reader = entries.ExecuteReader();
        while (reader.Read())
        {
            var key = ReadText(reader, ordinal: 0);
            try
            {
                KeyValueValidation.ValidateKey(key);
            }
            catch (KvException)
            {
                throw KvException.InvalidStoredKey(key);
            }

            _ = ReadStoredValue(reader, ordinal: 1, key);
            var revision = ReadInteger(reader, ordinal: 2);
            if (revision < 1 || revision > globalRevision || !seenRevisions.Add(revision))
            {
                throw KvException.InvalidStorage("revision_invariant");
            }
        }
    }

    private void EnsureIntegrity()
    {
        using var command = CreateCommand("PRAGMA integrity_check");
        using var reader = command.ExecuteReader();
        var results = new List<string>();
        while (reader.Read())
        {
            results.Add(ReadText(reader, ordinal: 0));
        }

        if (results.Count != 1 || !string.Equals(results[0], "ok", StringComparison.Ordinal))
        {
            throw KvException.InvalidStorage("integrity_check_failed");
        }
    }

    private void ValidateDefaultPragmas()
    {
        if (ExecuteScalar("PRAGMA user_version") is not long userVersion ||
            ExecuteScalar("PRAGMA application_id") is not long applicationId ||
            userVersion != 0 ||
            applicationId != 0)
        {
            throw KvException.InvalidStorage("malformed_schema");
        }
    }

    private List<SchemaObject> ReadApplicationObjects()
    {
        using var command = CreateCommand(
            """
            SELECT type, name, sql
            FROM sqlite_schema
            WHERE name NOT LIKE 'sqlite_%'
            ORDER BY type COLLATE BINARY, name COLLATE BINARY
            """);
        using var reader = command.ExecuteReader();
        var objects = new List<SchemaObject>();
        while (reader.Read())
        {
            objects.Add(
                new SchemaObject(
                    ReadText(reader, ordinal: 0),
                    ReadText(reader, ordinal: 1),
                    reader.IsDBNull(2) ? null : ReadText(reader, ordinal: 2)));
        }

        return objects;
    }

    private long? FindFutureSchemaVersion(IReadOnlyList<SchemaObject> objects)
    {
        if (!objects.Any(static item => item.Type == "table" && item.Name == "store_metadata"))
        {
            return null;
        }

        try
        {
            using var command = CreateCommand("SELECT schema_version FROM store_metadata");
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetValue(0) is long version && version > KvLimits.SchemaVersion)
                {
                    return version;
                }
            }
        }
        catch (SqliteException)
        {
            return null;
        }

        return null;
    }

    private static SchemaKind ClassifySchema(IReadOnlyList<SchemaObject> objects)
    {
        if (objects.Count == 0)
        {
            return SchemaKind.Fresh;
        }

        if (objects.Count == 1)
        {
            var v0 = objects[0];
            if (v0.Type == "table" &&
                v0.Name == "entries" &&
                v0.Sql is not null &&
                string.Equals(CanonicalSql(v0.Sql), V0Entries, StringComparison.Ordinal))
            {
                return SchemaKind.V0;
            }
        }

        if (objects.Count == 2)
        {
            var byName = objects.ToDictionary(static item => item.Name, StringComparer.Ordinal);
            if (byName.TryGetValue("entries", out var entries) &&
                byName.TryGetValue("store_metadata", out var metadata) &&
                entries.Type == "table" &&
                metadata.Type == "table" &&
                entries.Sql is not null &&
                metadata.Sql is not null &&
                string.Equals(CanonicalSql(entries.Sql), V1Entries, StringComparison.Ordinal) &&
                string.Equals(CanonicalSql(metadata.Sql), V1Metadata, StringComparison.Ordinal))
            {
                return SchemaKind.V1;
            }
        }

        return SchemaKind.Malformed;
    }

    private static string CanonicalSql(string sql)
    {
        var builder = new StringBuilder(sql.Length);
        foreach (var character in sql)
        {
            if (char.IsWhiteSpace(character) || character is '"' or '`' or '[' or ']')
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    private long? ReadCurrentRevision(string key)
    {
        using var command = CreateCommand("SELECT revision FROM entries WHERE key = $key COLLATE BINARY");
        command.Parameters.AddWithValue("$key", key);
        using var reader = command.ExecuteReader();
        return reader.Read() ? RequirePositiveRevision(ReadInteger(reader, ordinal: 0)) : null;
    }

    private long ReadGlobalRevision()
    {
        using var command = CreateCommand("SELECT global_revision FROM store_metadata WHERE singleton = 1");
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw KvException.InvalidStorage("malformed_schema");
        }

        var revision = ReadInteger(reader, ordinal: 0);
        if (revision is < 0 or > KvLimits.MaximumSafeInteger)
        {
            throw KvException.InvalidStorage("revision_invariant");
        }

        return revision;
    }

    private ImmutableArray<Entry> ReadEntries()
    {
        using var command = CreateCommand(
            """
            SELECT key, value_json, revision
            FROM entries
            ORDER BY key COLLATE BINARY
            """);
        using var reader = command.ExecuteReader();
        var entries = ImmutableArray.CreateBuilder<Entry>();
        while (reader.Read())
        {
            var key = ReadText(reader, ordinal: 0);
            entries.Add(
                new Entry(
                    key,
                    ReadStoredValue(reader, ordinal: 1, key),
                    RequirePositiveRevision(ReadInteger(reader, ordinal: 2))));
        }

        return entries.ToImmutable();
    }

    private long NextRevision()
    {
        var current = ReadGlobalRevision();
        if (current >= KvLimits.MaximumSafeInteger)
        {
            throw KvException.RevisionExhausted();
        }

        return current + 1;
    }

    private void UpdateGlobalRevision(long revision)
    {
        using var command = CreateCommand(
            "UPDATE store_metadata SET global_revision = $revision WHERE singleton = 1");
        command.Parameters.AddWithValue("$revision", revision);
        if (command.ExecuteNonQuery() != 1)
        {
            throw KvException.InvalidStorage("malformed_schema");
        }
    }

    private static JsonValue ReadStoredValue(SqliteDataReader reader, int ordinal, string key)
    {
        try
        {
            return RestrictedJson.ParseStored(ReadText(reader, ordinal));
        }
        catch (KvException)
        {
            throw KvException.InvalidStoredValue(key);
        }
    }

    private static string ReadText(SqliteDataReader reader, int ordinal)
    {
        return reader.GetValue(ordinal) is string value
            ? value
            : throw KvException.InvalidStorage("malformed_schema");
    }

    private static long ReadInteger(SqliteDataReader reader, int ordinal)
    {
        return reader.GetValue(ordinal) is long value
            ? value
            : throw KvException.InvalidStorage("revision_invariant");
    }

    private static long RequirePositiveRevision(long revision)
    {
        if (revision is < 1 or > KvLimits.MaximumSafeInteger)
        {
            throw KvException.InvalidStorage("revision_invariant");
        }

        return revision;
    }

    private void BeginImmediate()
    {
        ExecuteNonQuery("BEGIN IMMEDIATE");
        transactionOpen = true;
    }

    private void BeginDeferred()
    {
        ExecuteNonQuery("BEGIN");
        transactionOpen = true;
    }

    private void Commit()
    {
        ExecuteNonQuery("COMMIT");
        transactionOpen = false;
    }

    private void RollbackIfNeeded()
    {
        if (!transactionOpen)
        {
            return;
        }

        try
        {
            ExecuteNonQuery("ROLLBACK");
        }
        catch (SqliteException)
        {
        }
        finally
        {
            transactionOpen = false;
        }
    }

    private SqliteCommand CreateCommand(string commandText)
    {
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        return command;
    }

    private void ExecuteNonQuery(string commandText)
    {
        using var command = CreateCommand(commandText);
        command.ExecuteNonQuery();
    }

    private object? ExecuteScalar(string commandText)
    {
        using var command = CreateCommand(commandText);
        return command.ExecuteScalar();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private static void EnsureExistingParent(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var parent = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
            {
                throw KvException.StorageFailure("open");
            }
        }
        catch (KvException)
        {
            throw;
        }
        catch (Exception)
        {
            throw KvException.StorageFailure("open");
        }
    }

    private static KvException MapSqliteException(SqliteException exception, string operation)
    {
        if (IsBusy(exception))
        {
            return KvException.Busy();
        }

        if (exception.SqliteErrorCode is 11 or 26)
        {
            return KvException.InvalidStorage("integrity_check_failed");
        }

        return KvException.StorageFailure(operation);
    }

    private static bool IsBusy(SqliteException exception) =>
        exception.SqliteErrorCode is 5 or 6;

    private sealed record SchemaObject(string Type, string Name, string? Sql);

    private enum SchemaKind
    {
        Fresh,
        V0,
        V1,
        Malformed,
    }
}
