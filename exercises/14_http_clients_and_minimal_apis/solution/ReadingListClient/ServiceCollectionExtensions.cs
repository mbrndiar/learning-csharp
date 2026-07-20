using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddReadingListApiClient(
        this IServiceCollection services,
        Uri baseAddress,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);
        return services.AddReadingListApiClient(client => client.BaseAddress = baseAddress, timeout: timeout);
    }

    public static IHttpClientBuilder AddReadingListApiClient(
        this IServiceCollection services,
        Action<HttpClient> configureClient,
        Func<HttpMessageHandler>? primaryHandlerFactory = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureClient);

        IHttpClientBuilder builder = services.AddHttpClient<ReadingListApiClient>(client =>
        {
            client.Timeout = timeout ?? TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            configureClient(client);
        });

        if (primaryHandlerFactory is not null)
        {
            builder = builder.ConfigurePrimaryHttpMessageHandler(primaryHandlerFactory);
        }

        return builder;
    }
}
