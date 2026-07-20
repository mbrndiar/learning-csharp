using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
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

/// <summary>Base URL supplied to the typed client through dependency injection.</summary>
public sealed record TypedClientOptions(string BaseUrl);

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
