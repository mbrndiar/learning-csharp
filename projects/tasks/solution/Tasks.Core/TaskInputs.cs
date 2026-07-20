namespace Tasks.Core;

/// <summary>Normalized repository input for creating an incomplete task.</summary>
public sealed record CreateTaskInput
{
    /// <summary>Validate and normalize the supplied title.</summary>
    public CreateTaskInput(string title)
    {
        Title = TaskValidation.ValidateTitle(title);
    }

    /// <summary>The validated, trimmed title.</summary>
    public string Title { get; }
}

/// <summary>
/// Normalized partial update. Omitted fields stay <see cref="Optional{T}.Unset"/>
/// so <c>completed = false</c> remains a real update and an empty patch is
/// rejected before any storage operation begins.
/// </summary>
public sealed record UpdateTaskInput
{
    /// <summary>Validate a partial update, requiring at least one field.</summary>
    public UpdateTaskInput(Maybe<string> title = default, Maybe<bool> completed = default)
    {
        if (!title.HasValue && !completed.HasValue)
        {
            throw new TaskValidationException("update must include title or completed", "update");
        }

        Title = title.HasValue
            ? new Maybe<string>(TaskValidation.ValidateTitle(title.Value))
            : default;
        Completed = completed;
    }

    /// <summary>The validated new title, when supplied.</summary>
    public Maybe<string> Title { get; }

    /// <summary>The new completion state, when supplied.</summary>
    public Maybe<bool> Completed { get; }

    /// <summary>Apply this update to a current task, preserving omitted fields.</summary>
    public TaskItem ApplyTo(TaskItem current)
    {
        ArgumentNullException.ThrowIfNull(current);
        string title = Title.TryGet(out string? newTitle) ? newTitle : current.Title;
        bool completed = Completed.TryGet(out bool newCompleted) ? newCompleted : current.Completed;
        return new TaskItem(current.Id, title, completed);
    }
}
