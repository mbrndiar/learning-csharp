using Tasks.Core;

namespace Tasks.Http;

/// <summary>
/// A boundary failure with an explicit HTTP status that is not itself a domain
/// error: invalid JSON framing (400), an unknown route (404), or an unsupported
/// method (405). Domain failures use <see cref="TaskException"/> instead.
/// </summary>
public sealed class ApiErrorException : Exception
{
    /// <summary>Create a boundary error with a status, code, and optional metadata.</summary>
    public ApiErrorException(
        int statusCode,
        string code,
        string message,
        IReadOnlyDictionary<string, object>? details = null,
        string? allow = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Details = details;
        Allow = allow;
    }

    /// <summary>The HTTP status to return.</summary>
    public int StatusCode { get; }

    /// <summary>The stable machine-readable error code.</summary>
    public string Code { get; }

    /// <summary>Optional structured details.</summary>
    public IReadOnlyDictionary<string, object>? Details { get; }

    /// <summary>The <c>Allow</c> header value for a 405 response, when relevant.</summary>
    public string? Allow { get; }

    /// <summary>A 400 for a body that could not be decoded as JSON.</summary>
    public static ApiErrorException InvalidJson(string message)
        => new(400, ErrorCodes.InvalidJson, message);

    /// <summary>A 400 for a request body that exceeds the size bound.</summary>
    public static ApiErrorException PayloadTooLarge()
        => new(400, ErrorCodes.InvalidJson, "request body is too large");

    /// <summary>A 404 for an unknown route.</summary>
    public static ApiErrorException RouteNotFound()
        => new(404, ErrorCodes.NotFound, "route was not found");

    /// <summary>A 405 for a method a known path does not support.</summary>
    public static ApiErrorException MethodNotAllowed(IReadOnlyList<string> allowedMethods)
    {
        ArgumentNullException.ThrowIfNull(allowedMethods);
        return new ApiErrorException(
            405,
            ErrorCodes.MethodNotAllowed,
            "method is not allowed for this path",
            allow: string.Join(", ", allowedMethods));
    }
}
