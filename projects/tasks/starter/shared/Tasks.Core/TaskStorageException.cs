namespace Tasks.Core;

/// <summary>Persistence could not safely complete an operation.</summary>
public sealed class TaskStorageException : TaskException
{
    /// <summary>Create a storage failure, retaining the operation for diagnostics.</summary>
    public TaskStorageException(string message, string? operation = null)
        : base(ErrorCodes.InternalError, message, Build(operation))
    {
        Operation = operation;
    }

    /// <summary>The storage operation that failed, when known.</summary>
    public string? Operation { get; }

    private static Dictionary<string, object>? Build(string? operation)
        => operation is null ? null : new Dictionary<string, object> { ["operation"] = operation };
}
