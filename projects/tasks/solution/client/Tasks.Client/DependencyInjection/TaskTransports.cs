using Microsoft.Extensions.DependencyInjection;

namespace Tasks.Client;

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
