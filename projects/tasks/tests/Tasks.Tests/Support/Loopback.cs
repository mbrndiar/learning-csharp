using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tasks.Api;
using Tasks.Client;
using Tasks.Core;
using Tasks.Server;

namespace Tasks.Tests.Support;

/// <summary>Names and builders for the two server adapters under test.</summary>
public static class ServerAdapters
{
    /// <summary>The low-level middleware server adapter name.</summary>
    public const string LowLevel = "low-level";

    /// <summary>The Minimal API server adapter name.</summary>
    public const string MinimalApi = "minimal-api";

    /// <summary>Theory data enumerating both server adapters.</summary>
    public static TheoryData<string> Names => new() { LowLevel, MinimalApi };

    /// <summary>Build the named server adapter over one service.</summary>
    public static WebApplication Build(string name, TaskService service, ILoggerProvider? loggerProvider) => name switch
    {
        LowLevel => TaskServerHost.Build(service, loggerProvider),
        MinimalApi => TaskApiHost.Build(service, loggerProvider),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "unknown server adapter"),
    };
}

/// <summary>Names and factories for the two client transports under test.</summary>
public static class ClientAdapters
{
    /// <summary>The raw HttpClient transport name.</summary>
    public const string Raw = "raw";

    /// <summary>The typed IHttpClientFactory transport name.</summary>
    public const string Typed = "typed";

    /// <summary>Theory data enumerating both client transports.</summary>
    public static TheoryData<string> Names => new() { Raw, Typed };

    /// <summary>Resolve the transport factory for a transport name.</summary>
    public static TransportFactory Factory(string name) => name switch
    {
        Raw => TaskTransports.CreateRaw,
        Typed => TaskTransports.CreateTyped,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "unknown client transport"),
    };
}

/// <summary>Theory data for the client-by-server interoperability matrix.</summary>
public static class InteropMatrix
{
    /// <summary>Every (server, client) pair to exercise.</summary>
    public static TheoryData<string, string> Pairs
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (string server in new[] { ServerAdapters.LowLevel, ServerAdapters.MinimalApi })
            {
                foreach (string client in new[] { ClientAdapters.Raw, ClientAdapters.Typed })
                {
                    data.Add(server, client);
                }
            }

            return data;
        }
    }
}

/// <summary>
/// Owns one loopback server from ready state through an idempotent shutdown on
/// an OS-assigned ephemeral port, guaranteeing cleanup even when a test fails.
/// </summary>
public sealed class LoopbackServer : IAsyncDisposable
{
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

    private readonly WebApplication _app;

    private LoopbackServer(WebApplication app, string baseUrl)
    {
        _app = app;
        BaseUrl = baseUrl;
    }

    /// <summary>The loopback base URL, without a trailing slash.</summary>
    public string BaseUrl { get; }

    /// <summary>Start the named server adapter and wait until it is bound.</summary>
    public static async Task<LoopbackServer> StartAsync(
        string serverName,
        TaskService service,
        ILoggerProvider? loggerProvider = null,
        CancellationToken cancellationToken = default)
    {
        WebApplication app = ServerAdapters.Build(serverName, service, loggerProvider ?? NullLoggerProvider.Instance);
        try
        {
            app.Urls.Add("http://127.0.0.1:0");
            await app.StartAsync(cancellationToken);
            string address = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!
                .Addresses.First();
            return new LoopbackServer(app, address.TrimEnd('/'));
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        using var cts = new CancellationTokenSource(ShutdownTimeout);
        try
        {
            await _app.StopAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Fall through to disposal; a slow stop must not hang the suite.
        }

        await _app.DisposeAsync();
    }
}

/// <summary>
/// A loopback server that returns a caller-controlled response for every
/// request, used to drive client transports through malformed, redirecting, or
/// slow responses without depending on the real servers.
/// </summary>
public sealed class ControlledServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    private ControlledServer(WebApplication app, string baseUrl)
    {
        _app = app;
        BaseUrl = baseUrl;
    }

    /// <summary>The loopback base URL, without a trailing slash.</summary>
    public string BaseUrl { get; }

    /// <summary>Start a server that runs the supplied terminal handler.</summary>
    public static async Task<ControlledServer> StartAsync(
        RequestDelegate handler,
        CancellationToken cancellationToken = default)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        WebApplication app = builder.Build();
        app.Run(handler);
        try
        {
            app.Urls.Add("http://127.0.0.1:0");
            await app.StartAsync(cancellationToken);
            string address = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!
                .Addresses.First();
            return new ControlledServer(app, address.TrimEnd('/'));
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await _app.StopAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        await _app.DisposeAsync();
    }
}
