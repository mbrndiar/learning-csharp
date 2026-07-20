using Tasks.Core;

namespace Tasks.Api;

/// <summary>
/// Builds the Minimal API application over one composed service. Routing uses
/// <c>MapGet</c>/<c>MapPost</c>/<c>MapPatch</c>/<c>MapDelete</c>; an exception
/// middleware and status-code pages translate failures and framework-produced
/// 404/405 responses into the shared error envelope.
/// </summary>
public static class TaskApiHost
{
    /// <summary>
    /// Build a configured application. An optional logger provider lets tests
    /// capture or silence diagnostics; the default host logging is used otherwise.
    /// </summary>
    public static WebApplication Build(TaskService service, ILoggerProvider? loggerProvider = null)
    {
        ArgumentNullException.ThrowIfNull(service);

        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        if (loggerProvider is not null)
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(loggerProvider);
        }

        builder.Services.AddSingleton(service);

        WebApplication app = builder.Build();
        ILogger logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Tasks.Api");

        app.UseStatusCodePages(statusContext => ApiResponses.WriteStatusEnvelopeAsync(statusContext.HttpContext));
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await ApiResponses.WriteErrorAsync(context, exception, logger).ConfigureAwait(false);
            }
        });

        app.MapGet("/health", TaskEndpoints.Health);
        app.MapGet("/tasks", TaskEndpoints.List);
        app.MapPost("/tasks", TaskEndpoints.Create);
        app.MapGet("/tasks/{id}", TaskEndpoints.Get);
        app.MapPatch("/tasks/{id}", TaskEndpoints.Update);
        app.MapDelete("/tasks/{id}", TaskEndpoints.Delete);
        return app;
    }
}
