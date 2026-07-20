namespace Tasks.Core;

/// <summary>Normalized repository input for creating an incomplete task.</summary>
public sealed record CreateTaskInput
{
    /// <summary>Capture the supplied title.</summary>
    public CreateTaskInput(string title)
    {
        // TODO(milestone 1): validate and normalize the title before storage.
        Title = title;
    }

    /// <summary>The create title.</summary>
    public string Title { get; }
}
