using System.Globalization;

namespace Tasks.Client;

/// <summary>Library-neutral non-HTTP-response failure during one exchange.</summary>
public class TransportException : Exception
{
    /// <summary>Create a transport failure with a stable message.</summary>
    public TransportException(string message)
        : base(message)
    {
    }
}

/// <summary>The client could not establish or maintain the HTTP exchange.</summary>
public class TransportConnectionException : TransportException
{
    /// <summary>Create a connection failure.</summary>
    public TransportConnectionException(string message)
        : base(message)
    {
    }
}

/// <summary>The HTTP exchange exceeded its finite timeout.</summary>
public sealed class TransportTimeoutException : TransportConnectionException
{
    /// <summary>Create a timeout failure.</summary>
    public TransportTimeoutException(string message)
        : base(message)
    {
    }
}

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

/// <summary>Raw status, headers, and owned body bytes for application validation.</summary>
public sealed class TransportResponse
{
    /// <summary>Create a captured response.</summary>
    public TransportResponse(int status, IReadOnlyDictionary<string, string> headers, byte[] body)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(body);
        Status = status;
        Headers = headers;
        Body = body;
    }

    /// <summary>The HTTP status code.</summary>
    public int Status { get; }

    /// <summary>Response headers, looked up case-insensitively.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>The fully-read response body bytes.</summary>
    public byte[] Body { get; }
}

/// <summary>
/// Send once per call and release all adapter-owned network resources. Adapters
/// expose only status, headers, and body bytes so the application owns response
/// decoding and validation policy.
/// </summary>
public interface ITaskTransport : IAsyncDisposable
{
    /// <summary>Send one request without retries, preserving non-idempotent semantics.</summary>
    Task<TransportResponse> SendAsync(TransportRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Constructs one transport for a single CLI invocation.</summary>
/// <param name="baseUrl">The validated base URL.</param>
/// <param name="timeout">A positive, finite request timeout.</param>
public delegate ITaskTransport TransportFactory(string baseUrl, TimeSpan timeout);

/// <summary>Safe URL composition shared by every transport.</summary>
public static class TransportUrls
{
    /// <summary>Return an unambiguous HTTP(S) base URL safe for path composition.</summary>
    public static string NormalizeBaseUrl(string value)
    {
        if (string.IsNullOrEmpty(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("base URL must be an absolute HTTP or HTTPS URL", nameof(value));
        }

        if (value.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("base URL must not contain whitespace", nameof(value));
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
        {
            throw new ArgumentException("base URL must be an absolute HTTP or HTTPS URL", nameof(value));
        }

        string scheme = uri.Scheme.ToLowerInvariant();
        if (scheme is not ("http" or "https")
            || uri.UserInfo.Length > 0
            || uri.Query.Length > 0
            || uri.Fragment.Length > 0)
        {
            throw new ArgumentException(
                "base URL must use HTTP or HTTPS without credentials, query, or fragment",
                nameof(value));
        }

        string authority = uri.Authority.ToLowerInvariant();
        string path = uri.AbsolutePath.TrimEnd('/');
        return $"{scheme}://{authority}{path}";
    }

    /// <summary>Build an absolute request URL, encoding path and query safely.</summary>
    public static string BuildAbsoluteUrl(string baseUrl, TransportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return NormalizeBaseUrl(baseUrl) + BuildRelativeUrl(request, leadingSlash: true);
    }

    /// <summary>Build a relative request URL for use with a client base address.</summary>
    public static string BuildRelativeUrl(TransportRequest request)
        => BuildRelativeUrl(request, leadingSlash: false);

    private static string BuildRelativeUrl(TransportRequest request, bool leadingSlash)
    {
        ArgumentNullException.ThrowIfNull(request);
        IEnumerable<string> segments = request.Path.Split('/').Select(Uri.EscapeDataString);
        string path = string.Join('/', segments);
        if (!leadingSlash)
        {
            path = path.TrimStart('/');
        }

        if (request.Query.Count == 0)
        {
            return path;
        }

        string query = string.Join(
            '&',
            request.Query.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        return $"{path}?{query}";
    }

    internal static string FormatTimeout(TimeSpan timeout)
        => timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture);
}
