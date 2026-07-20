namespace Tasks.Core;

/// <summary>
/// Validated, immutable task value shared by repositories and adapters. The
/// constructor enforces every field invariant so a constructed task is always
/// a legal domain value regardless of its source.
/// </summary>
public sealed record TaskItem
{
    /// <summary>Create a validated task value.</summary>
    public TaskItem(long id, string title, bool completed)
    {
        Id = TaskValidation.ValidateTaskId(id);
        Title = TaskValidation.ValidateTitle(title);
        Completed = completed;
    }

    /// <summary>Positive identifier allocated by the repository.</summary>
    public long Id { get; }

    /// <summary>Trimmed single-line title of 1-120 Unicode characters.</summary>
    public string Title { get; }

    /// <summary>Whether the task is complete.</summary>
    public bool Completed { get; }
}
