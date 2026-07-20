using System.Net.Http.Headers;

namespace Tasks.Client;

/// <summary>
/// Standard-library-style transport built directly on <see cref="HttpClient"/>
/// and <see cref="HttpRequestMessage"/>. It owns its handler and client, sets a
/// finite timeout, disables redirects and ambient proxies, reads the full body,
/// and never retries.
/// </summary>
public sealed class RawHttpClientTransport : ITaskTransport
{
    private readonly string _baseUrl;
    private readonly HttpClient _client;

    /// <summary>Create a one-shot transport with an owned client and handler.</summary>
    public RawHttpClientTransport(string baseUrl, TimeSpan timeout)
    {
        _baseUrl = TransportUrls.NormalizeBaseUrl(baseUrl);
        ClientTimeout.Validate(timeout);

        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            AutomaticDecompression = System.Net.DecompressionMethods.None,
        };
        _client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = timeout,
        };
    }

    /// <inheritdoc />
    public async Task<TransportResponse> SendAsync(
        TransportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var message = new HttpRequestMessage(
            new HttpMethod(request.Method),
            TransportUrls.BuildAbsoluteUrl(_baseUrl, request));
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
        catch (Exception error) when (error is IOException or System.Net.Sockets.SocketException)
        {
            throw new TransportConnectionException(ClientPayloads.Describe(error, "request failed"));
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
