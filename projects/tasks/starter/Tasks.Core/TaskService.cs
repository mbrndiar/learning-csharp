namespace Tasks.Core;

/// <summary>
/// Framework-neutral application boundary shared by every transport adapter.
/// The starter leaves each operation for milestone one; the service must
/// validate caller-owned values before invoking persistence.
/// </summary>
public sealed class TaskService
{
    private readonly ITaskRepository _repository;

    /// <summary>Create a service over one repository.</summary>
    public TaskService(ITaskRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>Create one incomplete task from a title.</summary>
    public Task<TaskItem> CreateTaskAsync(string title, CancellationToken cancellationToken = default)
    {
        // TODO(milestone 1): normalize the title, then delegate to the repository.
        _ = _repository;
        return Incomplete.Value<Task<TaskItem>>($"milestone 1 task creation for '{title}'");
    }

    /// <summary>List tasks, optionally filtered by completion state.</summary>
    public Task<IReadOnlyList<TaskItem>> ListTasksAsync(
        bool? completed = null,
        CancellationToken cancellationToken = default)
    {
        // TODO(milestone 1): validate the optional strict Boolean filter.
        _ = _repository;
        return Incomplete.Value<Task<IReadOnlyList<TaskItem>>>($"milestone 1 task listing for completed={completed}");
    }

    /// <summary>Return one task by identifier.</summary>
    public Task<TaskItem> GetTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        // TODO(milestone 1): validate the identifier before repository access.
        _ = _repository;
        return Incomplete.Value<Task<TaskItem>>($"milestone 1 task lookup for {taskId}");
    }

    /// <summary>Apply a partial task update after validating the identifier.</summary>
    public Task<TaskItem> UpdateTaskAsync(
        long taskId,
        UpdateTaskInput update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        // TODO(milestone 1): normalize the identifier and update before one call.
        _ = _repository;
        return Incomplete.Value<Task<TaskItem>>($"milestone 1 task update for {taskId}");
    }

    /// <summary>Delete one task by identifier.</summary>
    public Task DeleteTaskAsync(long taskId, CancellationToken cancellationToken = default)
    {
        // TODO(milestone 1): validate the identifier before repository access.
        _ = _repository;
        return Incomplete.Value<Task>($"milestone 1 task deletion for {taskId}");
    }
}
