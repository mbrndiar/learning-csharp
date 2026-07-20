namespace Tasks.Core;

/// <summary>
/// Normalized partial update. Omitted fields stay <see cref="Maybe{T}"/> unset so
/// <c>completed = false</c> remains a real update.
/// </summary>
public sealed record UpdateTaskInput
{
    /// <summary>Capture the supplied fields.</summary>
    public UpdateTaskInput(Maybe<string> title = default, Maybe<bool> completed = default)
    {
        // TODO(milestone 1): reject an empty update and validate present fields,
        // keeping an explicit completed=false distinct from an omitted field.
        Title = title;
        Completed = completed;
    }

    /// <summary>The new title, when supplied.</summary>
    public Maybe<string> Title { get; }

    /// <summary>The new completion state, when supplied.</summary>
    public Maybe<bool> Completed { get; }

    /// <summary>Apply this update to a current task, preserving omitted fields.</summary>
    public TaskItem ApplyTo(TaskItem current)
    {
        ArgumentNullException.ThrowIfNull(current);
        // TODO(milestone 2): return a new task with each unset field preserved.
        _ = Title;
        return Incomplete.Value<TaskItem>($"milestone 2 update application for task {current.Id}");
    }
}
