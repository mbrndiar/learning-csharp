using System.Collections.Generic;

namespace Tasks.Core;

/// <summary>
/// Stable machine-readable error categories shared by the domain, the HTTP
/// adapters, and the command-line clients.
/// </summary>
public static class ErrorCodes
{
    /// <summary>A request body could not be decoded as JSON.</summary>
    public const string InvalidJson = "invalid_json";

    /// <summary>A task or route was not found.</summary>
    public const string NotFound = "not_found";

    /// <summary>A known path does not support the request method.</summary>
    public const string MethodNotAllowed = "method_not_allowed";

    /// <summary>A decoded value violates the request or domain rules.</summary>
    public const string ValidationError = "validation_error";

    /// <summary>An unexpected server or persistence failure occurred.</summary>
    public const string InternalError = "internal_error";
}

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
