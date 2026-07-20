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
