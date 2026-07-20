namespace Tasks.Core;

/// <summary>A request or domain value violates the Task contract.</summary>
public sealed class TaskValidationException : TaskException
{
    /// <summary>Create a validation failure naming the offending field.</summary>
    public TaskValidationException(string message, string field)
        : base(ErrorCodes.ValidationError, message, new Dictionary<string, object> { ["field"] = field })
    {
        Field = field;
    }

    /// <summary>The field that failed validation.</summary>
    public string Field { get; }
}
