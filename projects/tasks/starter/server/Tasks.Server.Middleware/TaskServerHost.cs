using Tasks.Core;

namespace Tasks.Server.Middleware;

/// <summary>
/// Builds the low-level server application over one composed service. The
/// starter leaves the pipeline for milestone three.
/// </summary>
public static class TaskServerHost
{
    /// <summary>Build a configured application whose terminal middleware is the handler.</summary>
    public static WebApplication Build(TaskService service, ILoggerProvider? loggerProvider = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        _ = loggerProvider;
        // TODO(milestone 3): create a WebApplication, register the service, and
        // run the low-level task handler as the single terminal middleware.
        throw new IncompleteProjectException("milestone 3 low-level middleware server host");
    }
}
