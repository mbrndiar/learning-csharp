namespace Tasks.Core;

/// <summary>
/// Framework-neutral application boundary shared by every transport adapter.
/// The service validates caller-owned values before invoking persistence, so
/// repositories always receive normalized domain inputs and a validation
/// failure can never begin a storage operation.
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
        => _repository.CreateAsync(new CreateTaskInput(title), cancellationToken);

    /// <summary>List tasks, optionally filtered by completion state.</summary>
    public Task<IReadOnlyList<TaskItem>> ListTasksAsync(bool? completed = null, CancellationToken cancellationToken = default)
        => _repository.ListAsync(completed, cancellationToken);

    /// <summary>Return one task by identifier.</summary>
    public Task<TaskItem> GetTaskAsync(long taskId, CancellationToken cancellationToken = default)
        => _repository.GetAsync(TaskValidation.ValidateTaskId(taskId), cancellationToken);

    /// <summary>Apply a partial task update after validating the identifier.</summary>
    public Task<TaskItem> UpdateTaskAsync(
        long taskId,
        UpdateTaskInput update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        long normalizedId = TaskValidation.ValidateTaskId(taskId);
        return _repository.UpdateAsync(normalizedId, update, cancellationToken);
    }

    /// <summary>Delete one task by identifier.</summary>
    public Task DeleteTaskAsync(long taskId, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(TaskValidation.ValidateTaskId(taskId), cancellationToken);
}
