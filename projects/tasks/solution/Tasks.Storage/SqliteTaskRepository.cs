using System.Globalization;
using Microsoft.Data.Sqlite;
using Tasks.Core;

namespace Tasks.Storage;

/// <summary>
/// SQLite implementation of the shared task repository. Each operation uses one
/// short-lived connection so it clearly owns its transaction and cleanup, and
/// <c>AUTOINCREMENT</c> keeps identifiers monotonic and non-reusable.
/// </summary>
public sealed class SqliteTaskRepository : ITaskRepository
{
    private const string SelectColumns = "SELECT id, title, completed FROM tasks";

    private const string Schema = """
        CREATE TABLE IF NOT EXISTS tasks (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            title TEXT NOT NULL,
            completed INTEGER NOT NULL DEFAULT 0 CHECK (completed IN (0, 1))
        )
        """;

    private readonly string _connectionString;

    /// <summary>Open (creating if needed) the database and initialize the schema.</summary>
    public SqliteTaskRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(databasePath);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
        }.ToString();
        InitializeSchema();
    }

    /// <inheritdoc />
    public async Task<TaskItem> CreateAsync(CreateTaskInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        const string operation = "create";
        try
        {
            await using SqliteConnection connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            return await InTransactionAsync(connection, async () =>
            {
                await ExecuteAsync(
                        connection,
                        "INSERT INTO tasks (title, completed) VALUES (@title, 0)",
                        cancellationToken,
                        ("@title", input.Title))
                    .ConfigureAwait(false);

                long id;
                await using (SqliteCommand idCommand = connection.CreateCommand())
                {
                    idCommand.CommandText = "SELECT last_insert_rowid()";
                    object? scalar = await idCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    id = Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
                }

                return await GetWithConnectionAsync(connection, id, operation, cancellationToken)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch (Exception error) when (IsStorageFailure(error))
        {
            throw StorageError(operation, error);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TaskItem>> ListAsync(
        bool? completed = null,
        CancellationToken cancellationToken = default)
    {
        const string operation = "list";
        try
        {
            await using SqliteConnection connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using SqliteCommand command = connection.CreateCommand();
            if (completed is null)
            {
                command.CommandText = $"{SelectColumns} ORDER BY id";
            }
            else
            {
                command.CommandText = $"{SelectColumns} WHERE completed = @completed ORDER BY id";
                command.Parameters.AddWithValue("@completed", completed.Value ? 1 : 0);
            }

            var tasks = new List<TaskItem>();
            await using SqliteDataReader reader =
                await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                tasks.Add(MapRow(reader, operation));
            }

            return tasks;
        }
        catch (Exception error) when (IsStorageFailure(error))
        {
            throw StorageError(operation, error);
        }
    }

    /// <inheritdoc />
    public async Task<TaskItem> GetAsync(long taskId, CancellationToken cancellationToken = default)
    {
        const string operation = "get";
        try
        {
            await using SqliteConnection connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            return await GetWithConnectionAsync(connection, taskId, operation, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception error) when (IsStorageFailure(error))
        {
            throw StorageError(operation, error);
        }
    }

    /// <inheritdoc />
    public async Task<TaskItem> UpdateAsync(
        long taskId,
        UpdateTaskInput update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        const string operation = "update";
        try
        {
            await using SqliteConnection connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            return await InTransactionAsync(connection, async () =>
            {
                // Reserve the writer before reading current state so concurrent
                // partial updates cannot both read the old row and clobber a field.
                TaskItem current = await GetWithConnectionAsync(connection, taskId, operation, cancellationToken)
                    .ConfigureAwait(false);
                TaskItem updated = update.ApplyTo(current);
                await ExecuteAsync(
                        connection,
                        "UPDATE tasks SET title = @title, completed = @completed WHERE id = @id",
                        cancellationToken,
                        ("@title", updated.Title),
                        ("@completed", updated.Completed ? 1 : 0),
                        ("@id", taskId))
                    .ConfigureAwait(false);
                return await GetWithConnectionAsync(connection, taskId, operation, cancellationToken)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch (Exception error) when (IsStorageFailure(error))
        {
            throw StorageError(operation, error);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long taskId, CancellationToken cancellationToken = default)
    {
        const string operation = "delete";
        try
        {
            await using SqliteConnection connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM tasks WHERE id = @id";
            command.Parameters.AddWithValue("@id", taskId);
            int affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            if (affected == 0)
            {
                throw new TaskNotFoundException(taskId);
            }
        }
        catch (Exception error) when (IsStorageFailure(error))
        {
            throw StorageError(operation, error);
        }
    }

    private void InitializeSchema()
    {
        const string operation = "initialize";
        try
        {
            using SqliteConnection connection = new(_connectionString);
            connection.Open();
            ConfigureBusyTimeout(connection);
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = Schema;
            command.ExecuteNonQuery();
        }
        catch (Exception error) when (IsStorageFailure(error))
        {
            throw StorageError(operation, error);
        }
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        SqliteConnection connection = new(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            ConfigureBusyTimeout(connection);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static void ConfigureBusyTimeout(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout = 5000";
        command.ExecuteNonQuery();
    }

    private static async Task<TaskItem> InTransactionAsync(SqliteConnection connection, Func<Task<TaskItem>> body)
    {
        await RawAsync(connection, "BEGIN IMMEDIATE").ConfigureAwait(false);
        try
        {
            TaskItem result = await body().ConfigureAwait(false);
            await RawAsync(connection, "COMMIT").ConfigureAwait(false);
            return result;
        }
        catch
        {
            await TryRollbackAsync(connection).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task TryRollbackAsync(SqliteConnection connection)
    {
        try
        {
            await RawAsync(connection, "ROLLBACK").ConfigureAwait(false);
        }
        catch (SqliteException)
        {
            // A secondary rollback failure must not hide the original error.
        }
    }

    private static async Task RawAsync(SqliteConnection connection, string sql)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        foreach ((string name, object value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<TaskItem> GetWithConnectionAsync(
        SqliteConnection connection,
        long taskId,
        string operation,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE id = @id";
        command.Parameters.AddWithValue("@id", taskId);
        await using SqliteDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new TaskNotFoundException(taskId);
        }

        return MapRow(reader, operation);
    }

    private static TaskItem MapRow(SqliteDataReader reader, string operation)
    {
        // Treat persisted data as an untrusted boundary: verify SQLite's exact
        // representation before letting the domain enforce its invariants.
        if (reader.GetFieldType(0) != typeof(long)
            || reader.GetFieldType(1) != typeof(string)
            || reader.GetFieldType(2) != typeof(long))
        {
            throw new TaskStorageException($"SQLite {operation} returned an invalid task row", operation);
        }

        long id = reader.GetInt64(0);
        string title = reader.GetString(1);
        long completed = reader.GetInt64(2);
        if (completed is not (0 or 1))
        {
            throw new TaskStorageException($"SQLite {operation} returned an invalid task row", operation);
        }

        try
        {
            return new TaskItem(id, title, completed == 1);
        }
        catch (TaskValidationException error)
        {
            throw new TaskStorageException(
                $"SQLite {operation} returned an invalid task row: {error.Message}",
                operation);
        }
    }

    private static bool IsStorageFailure(Exception error)
        => error is SqliteException or IOException or InvalidCastException or InvalidOperationException or FormatException;

    private static TaskStorageException StorageError(string operation, Exception error)
        => new($"SQLite {operation} failed: {error.Message}", operation);
}
