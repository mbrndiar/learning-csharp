using Tasks.Core;

namespace Tasks.Client;

/// <summary>
/// Framework-style transport whose <see cref="HttpClient"/> is supplied by
/// <c>IHttpClientFactory</c>. The starter captures the client; sending is
/// milestone four.
/// </summary>
public sealed class TypedHttpClientTransport : ITaskTransport
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;

    /// <summary>Create the transport over an injected, pre-configured client.</summary>
    public TypedHttpClientTransport(HttpClient client, TypedClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        _client = client;
        _baseUrl = options.BaseUrl;
    }

    /// <inheritdoc />
    public Task<TransportResponse> SendAsync(TransportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = _client;
        // TODO(milestone 4): send the request through the injected client using
        // its base address and finite timeout, then capture the raw response.
        throw new IncompleteProjectException($"milestone 4 typed HttpClient send to {_baseUrl}");
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
