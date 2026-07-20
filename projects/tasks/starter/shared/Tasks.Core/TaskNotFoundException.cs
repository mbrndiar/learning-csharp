namespace Tasks.Core;

/// <summary>Signals that a task identifier does not exist.</summary>
public sealed class TaskNotFoundException : TaskException
{
    /// <summary>Create a not-found failure for one identifier.</summary>
    public TaskNotFoundException(long taskId)
        : base(ErrorCodes.NotFound, $"task {taskId} was not found")
    {
        TaskId = taskId;
    }

    /// <summary>The identifier that was not found.</summary>
    public long TaskId { get; }
}
