namespace Tasks.Core;

/// <summary>
/// Narrow persistence capability consumed by the framework-neutral service.
/// Missing single-task operations throw <see cref="TaskNotFoundException"/>
/// rather than returning null, so every successful result is a complete task.
/// Repository-assigned identifiers increase monotonically and are never reused
/// after deletion; gaps are allowed.
/// </summary>
public interface ITaskRepository
{
    /// <summary>Persist a new incomplete task and return it with its identifier.</summary>
    Task<TaskItem> CreateAsync(CreateTaskInput input, CancellationToken cancellationToken = default);

    /// <summary>Return tasks ordered by identifier, optionally filtered by completion.</summary>
    Task<IReadOnlyList<TaskItem>> ListAsync(bool? completed = null, CancellationToken cancellationToken = default);

    /// <summary>Return one task or throw <see cref="TaskNotFoundException"/>.</summary>
    Task<TaskItem> GetAsync(long taskId, CancellationToken cancellationToken = default);

    /// <summary>Apply all supplied fields atomically or leave the task unchanged.</summary>
    Task<TaskItem> UpdateAsync(long taskId, UpdateTaskInput update, CancellationToken cancellationToken = default);

    /// <summary>Atomically remove one task or throw <see cref="TaskNotFoundException"/>.</summary>
    Task DeleteAsync(long taskId, CancellationToken cancellationToken = default);
}
