using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace Tasks.Client;

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
