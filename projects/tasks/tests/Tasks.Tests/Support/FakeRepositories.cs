using Tasks.Core;

namespace Tasks.Tests.Support;

/// <summary>
/// A minimal in-memory repository for domain and service tests. It mirrors the
/// real adapters' observable contract: ordered listing, not-found errors, and
/// monotonic identifiers that are never reused after deletion.
/// </summary>
public sealed class InMemoryTaskRepository : ITaskRepository
{
    private readonly List<TaskItem> _tasks = [];
    private long _nextId = 1;

    /// <inheritdoc />
    public Task<TaskItem> CreateAsync(CreateTaskInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var created = new TaskItem(_nextId, input.Title, false);
        _nextId++;
        _tasks.Add(created);
        return Task.FromResult(created);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TaskItem>> ListAsync(
        bool? completed = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<TaskItem> query = _tasks.OrderBy(task => task.Id);
        if (completed is not null)
        {
            query = query.Where(task => task.Completed == completed.Value);
        }

        return Task.FromResult<IReadOnlyList<TaskItem>>(query.ToArray());
    }

    /// <inheritdoc />
    public Task<TaskItem> GetAsync(long taskId, CancellationToken cancellationToken = default)
        => Task.FromResult(_tasks.FirstOrDefault(task => task.Id == taskId)
            ?? throw new TaskNotFoundException(taskId));

    /// <inheritdoc />
    public Task<TaskItem> UpdateAsync(
        long taskId,
        UpdateTaskInput update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        for (int index = 0; index < _tasks.Count; index++)
        {
            if (_tasks[index].Id != taskId)
            {
                continue;
            }

            TaskItem updated = update.ApplyTo(_tasks[index]);
            _tasks[index] = updated;
            return Task.FromResult(updated);
        }

        throw new TaskNotFoundException(taskId);
    }

    /// <inheritdoc />
    public Task DeleteAsync(long taskId, CancellationToken cancellationToken = default)
    {
        int removed = _tasks.RemoveAll(task => task.Id == taskId);
        return removed == 0 ? throw new TaskNotFoundException(taskId) : Task.CompletedTask;
    }
}

/// <summary>A repository whose every operation fails as a storage error.</summary>
public sealed class ThrowingTaskRepository : ITaskRepository
{
    /// <inheritdoc />
    public Task<TaskItem> CreateAsync(CreateTaskInput input, CancellationToken cancellationToken = default)
        => throw new TaskStorageException("injected storage failure", "create");

    /// <inheritdoc />
    public Task<IReadOnlyList<TaskItem>> ListAsync(bool? completed = null, CancellationToken cancellationToken = default)
        => throw new TaskStorageException("injected storage failure", "list");

    /// <inheritdoc />
    public Task<TaskItem> GetAsync(long taskId, CancellationToken cancellationToken = default)
        => throw new TaskStorageException("injected storage failure", "get");

    /// <inheritdoc />
    public Task<TaskItem> UpdateAsync(long taskId, UpdateTaskInput update, CancellationToken cancellationToken = default)
        => throw new TaskStorageException("injected storage failure", "update");

    /// <inheritdoc />
    public Task DeleteAsync(long taskId, CancellationToken cancellationToken = default)
        => throw new TaskStorageException("injected storage failure", "delete");
}
