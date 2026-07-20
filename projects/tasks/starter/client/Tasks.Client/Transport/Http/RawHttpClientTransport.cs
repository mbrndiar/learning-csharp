using Tasks.Core;

namespace Tasks.Client;

/// <summary>
/// Standard-library-style transport built directly on <see cref="HttpClient"/>.
/// The starter validates the base URL and timeout; sending is milestone three.
/// </summary>
public sealed class RawHttpClientTransport : ITaskTransport
{
    private readonly string _baseUrl;
    private readonly TimeSpan _timeout;

    /// <summary>Create a one-shot transport bound to a base URL and timeout.</summary>
    public RawHttpClientTransport(string baseUrl, TimeSpan timeout)
    {
        _baseUrl = TransportUrls.NormalizeBaseUrl(baseUrl);
        ClientTimeout.Validate(timeout);
        _timeout = timeout;
    }

    /// <inheritdoc />
    public Task<TransportResponse> SendAsync(TransportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        // TODO(milestone 3): build an HttpRequestMessage, disable redirects and
        // ambient proxies, send once with the finite timeout, and capture the
        // status, headers, and body bytes.
        throw new IncompleteProjectException($"milestone 3 raw HttpClient send to {_baseUrl} within {_timeout}");
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
