namespace Tasks.Client;

/// <summary>
/// Framework-style transport whose <see cref="HttpClient"/> is supplied by
/// <c>IHttpClientFactory</c>. The factory owns handler lifetime, timeout, and
/// default headers; this class only builds each request and reads the response.
/// </summary>
public sealed class TypedHttpClientTransport : ITaskTransport
{
    private readonly HttpClient _client;

    /// <summary>Create the transport over an injected, pre-configured client.</summary>
    public TypedHttpClientTransport(HttpClient client, TypedClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        _client = client;
        _ = options;
    }

    /// <inheritdoc />
    public async Task<TransportResponse> SendAsync(
        TransportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var message = new HttpRequestMessage(
            new HttpMethod(request.Method),
            new Uri(TransportUrls.BuildRelativeUrl(request), UriKind.Relative));
        if (request.JsonBody is not null)
        {
            message.Content = ClientPayloads.JsonContent(request.JsonBody);
        }

        try
        {
            using HttpResponseMessage response =
                await _client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            byte[] body = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            return new TransportResponse((int)response.StatusCode, ClientPayloads.CollectHeaders(response), body);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TransportTimeoutException("request timed out");
        }
        catch (HttpRequestException error)
        {
            throw new TransportConnectionException(ClientPayloads.Describe(error, "request failed"));
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
