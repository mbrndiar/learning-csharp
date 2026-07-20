namespace Tasks.Client;

/// <summary>
/// One validated request that any supported HTTP library can represent. Only
/// GET, POST, PATCH, and DELETE are used; the body, when present, is a small
/// JSON object with string or Boolean values.
/// </summary>
public sealed record TransportRequest
{
    private static readonly HashSet<string> Methods = new(StringComparer.Ordinal) { "GET", "POST", "PATCH", "DELETE" };

    /// <summary>Create and validate one transport request.</summary>
    public TransportRequest(
        string method,
        string path,
        IReadOnlyDictionary<string, string>? query = null,
        IReadOnlyDictionary<string, object?>? jsonBody = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(path);
        if (!Methods.Contains(method))
        {
            throw new ArgumentException("unsupported HTTP method", nameof(method));
        }

        if (!path.StartsWith('/'))
        {
            throw new ArgumentException("request path must start with /", nameof(path));
        }

        Query = query is null
            ? EmptyQuery
            : new Dictionary<string, string>(query, StringComparer.Ordinal);
        if (jsonBody is not null)
        {
            var copy = new Dictionary<string, object?>(jsonBody, StringComparer.Ordinal);
            foreach (object? value in copy.Values)
            {
                if (value is not (null or string or bool or int or long or double))
                {
                    throw new ArgumentException("request body must contain JSON-compatible values", nameof(jsonBody));
                }
            }

            JsonBody = copy;
        }

        Method = method;
        Path = path;
    }

    /// <summary>The HTTP method.</summary>
    public string Method { get; }

    /// <summary>The request path beginning with a slash.</summary>
    public string Path { get; }

    /// <summary>Query parameters, if any.</summary>
    public IReadOnlyDictionary<string, string> Query { get; }

    /// <summary>The JSON object body, if any.</summary>
    public IReadOnlyDictionary<string, object?>? JsonBody { get; }

    private static IReadOnlyDictionary<string, string> EmptyQuery { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
