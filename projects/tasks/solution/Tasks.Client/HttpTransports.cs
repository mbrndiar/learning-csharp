using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tasks.Core.Http;

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

/// <summary>Base URL supplied to the typed client through dependency injection.</summary>
public sealed record TypedClientOptions(string BaseUrl);

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

/// <summary>Registers the typed transport with an <c>IHttpClientFactory</c>.</summary>
public static class TaskApiClientServiceCollectionExtensions
{
    /// <summary>Register the typed task client with a base URL and finite timeout.</summary>
    public static IServiceCollection AddTaskApiClient(
        this IServiceCollection services,
        string baseUrl,
        TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(services);
        string normalized = TransportUrls.NormalizeBaseUrl(baseUrl);
        ClientTimeout.Validate(timeout);

        services.AddSingleton(new TypedClientOptions(normalized));
        services
            .AddHttpClient<TypedHttpClientTransport>(client =>
            {
                client.BaseAddress = new Uri(normalized + "/");
                client.Timeout = timeout;
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseProxy = false,
                AutomaticDecompression = System.Net.DecompressionMethods.None,
            });
        return services;
    }
}

/// <summary>Factory methods that build one owned transport per CLI invocation.</summary>
public static class TaskTransports
{
    /// <summary>Build the raw <see cref="HttpClient"/> transport.</summary>
    public static ITaskTransport CreateRaw(string baseUrl, TimeSpan timeout)
        => new RawHttpClientTransport(baseUrl, timeout);

    /// <summary>Build the typed <c>IHttpClientFactory</c> transport, owning its provider.</summary>
    public static ITaskTransport CreateTyped(string baseUrl, TimeSpan timeout)
    {
        var services = new ServiceCollection();
        services.AddTaskApiClient(baseUrl, timeout);
        ServiceProvider provider = services.BuildServiceProvider();
        try
        {
            TypedHttpClientTransport transport = provider.GetRequiredService<TypedHttpClientTransport>();
            return new OwnedTransport(transport, provider);
        }
        catch
        {
            provider.Dispose();
            throw;
        }
    }

    private sealed class OwnedTransport : ITaskTransport
    {
        private readonly ITaskTransport _inner;
        private readonly ServiceProvider _provider;

        public OwnedTransport(ITaskTransport inner, ServiceProvider provider)
        {
            _inner = inner;
            _provider = provider;
        }

        public Task<TransportResponse> SendAsync(TransportRequest request, CancellationToken cancellationToken = default)
            => _inner.SendAsync(request, cancellationToken);

        public async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync().ConfigureAwait(false);
            await _provider.DisposeAsync().ConfigureAwait(false);
        }
    }
}

internal static class ClientTimeout
{
    public static void Validate(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero || timeout == System.Threading.Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be positive and finite");
        }
    }
}

internal static class ClientPayloads
{
    public static ByteArrayContent JsonContent(IReadOnlyDictionary<string, object?> body)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(body, TaskJson.Options);
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }

    public static IReadOnlyDictionary<string, string> CollectHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Content.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        return headers;
    }

    public static string Describe(Exception error, string fallback)
    {
        string message = error.Message.Trim();
        return message.Length > 0 ? message : fallback;
    }
}
