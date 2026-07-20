using Tasks.Core;

namespace Tasks.Server.Middleware;

/// <summary>
/// Builds the low-level server application over one composed service. The
/// caller owns starting, binding, and disposing the returned application.
/// </summary>
public static class TaskServerHost
{
    /// <summary>
    /// Build a configured application whose single terminal middleware is the
    /// low-level task handler. An optional logger provider lets tests capture or
    /// silence diagnostics; the default host logging is used otherwise.
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
        var handler = new LowLevelTaskHandler(
            service,
            app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<LowLevelTaskHandler>());
        app.Run(handler.HandleAsync);
        return app;
    }
}
