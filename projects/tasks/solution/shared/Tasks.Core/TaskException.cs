namespace Tasks.Core;

/// <summary>
/// Base type for expected application failures that an adapter may translate
/// into the shared error envelope without leaking implementation details.
/// </summary>
public class TaskException : Exception
{
    /// <summary>Create a task failure with a stable code and optional details.</summary>
    public TaskException(string code, string message, IReadOnlyDictionary<string, object>? details = null)
        : base(message)
    {
        Code = code;
        Details = details;
    }

    /// <summary>The stable machine-readable error code.</summary>
    public string Code { get; }

    /// <summary>Small structured hints that help a caller locate the problem.</summary>
    public IReadOnlyDictionary<string, object>? Details { get; }
}
