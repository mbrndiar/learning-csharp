namespace Tasks.Client;

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
