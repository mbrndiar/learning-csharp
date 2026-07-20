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
