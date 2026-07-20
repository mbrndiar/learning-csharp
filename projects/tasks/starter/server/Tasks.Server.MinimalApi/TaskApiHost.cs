using Tasks.Core;

namespace Tasks.Server.MinimalApi;

/// <summary>
/// Builds the Minimal API application over one composed service. The starter
/// leaves routing, dependency injection, typed results, and error translation
/// for milestone four.
/// </summary>
public static class TaskApiHost
{
    /// <summary>Build a configured Minimal API application.</summary>
    public static WebApplication Build(TaskService service, ILoggerProvider? loggerProvider = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        _ = loggerProvider;
        // TODO(milestone 4): map the endpoints with MapGet/MapPost/MapPatch/
        // MapDelete, inject the service, and translate failures and framework
        // 404/405 responses into the shared error envelope.
        throw new IncompleteProjectException("milestone 4 Minimal API server host");
    }
}
