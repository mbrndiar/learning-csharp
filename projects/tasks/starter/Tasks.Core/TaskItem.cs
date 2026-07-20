namespace Tasks.Core;

/// <summary>
/// Task value shared by repositories and adapters. In the solution this
/// constructor enforces every field invariant; the starter stores raw values so
/// milestone-one validation can be added deliberately.
/// </summary>
public sealed record TaskItem
{
    /// <summary>Create a task value.</summary>
    public TaskItem(long id, string title, bool completed)
    {
        // TODO(milestone 1): validate and normalize every field here so a
        // constructed task is always a legal domain value (see TaskValidation).
        Id = id;
        Title = title;
        Completed = completed;
    }

    /// <summary>Positive identifier allocated by the repository.</summary>
    public long Id { get; }

    /// <summary>Trimmed single-line title of 1-120 Unicode characters.</summary>
    public string Title { get; }

    /// <summary>Whether the task is complete.</summary>
    public bool Completed { get; }
}
