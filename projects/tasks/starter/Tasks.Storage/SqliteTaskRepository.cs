using Tasks.Core;

namespace Tasks.Storage;

/// <summary>
/// SQLite implementation of the shared task repository. The starter binds the
/// database path without touching the filesystem, so untouched smoke paths have
/// no side effects; each operation is implemented in milestone two.
/// </summary>
public sealed class SqliteTaskRepository : ITaskRepository
{
    private readonly string _databasePath;

    /// <summary>Bind the repository to one database path.</summary>
    public SqliteTaskRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(databasePath);
        // TODO(milestone 2): initialize the checked AUTOINCREMENT schema idempotently.
        _databasePath = databasePath;
    }

    /// <inheritdoc />
    public Task<TaskItem> CreateAsync(CreateTaskInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Incomplete.Value<Task<TaskItem>>($"milestone 2 SQLite create in {_databasePath}");
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TaskItem>> ListAsync(bool? completed = null, CancellationToken cancellationToken = default)
        => Incomplete.Value<Task<IReadOnlyList<TaskItem>>>($"milestone 2 SQLite list in {_databasePath}");

    /// <inheritdoc />
    public Task<TaskItem> GetAsync(long taskId, CancellationToken cancellationToken = default)
        => Incomplete.Value<Task<TaskItem>>($"milestone 2 SQLite get {taskId} in {_databasePath}");

    /// <inheritdoc />
    public Task<TaskItem> UpdateAsync(long taskId, UpdateTaskInput update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        return Incomplete.Value<Task<TaskItem>>($"milestone 2 SQLite update {taskId} in {_databasePath}");
    }

    /// <inheritdoc />
    public Task DeleteAsync(long taskId, CancellationToken cancellationToken = default)
        => Incomplete.Value<Task>($"milestone 2 SQLite delete {taskId} in {_databasePath}");
}
