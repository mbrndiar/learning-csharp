using Tasks.Core;

namespace Tasks.Server.Middleware;

/// <summary>
/// Low-level HTTP boundary implemented as a single request handler. The starter
/// captures the injected service and logger; routing, byte reading, JSON
/// decoding, status selection, and error translation are milestone three.
/// </summary>
public sealed class LowLevelTaskHandler
{
    private readonly TaskService _service;
    private readonly ILogger<LowLevelTaskHandler> _logger;

    /// <summary>Create the handler over one composed service.</summary>
    public LowLevelTaskHandler(TaskService service, ILogger<LowLevelTaskHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(logger);
        _service = service;
        _logger = logger;
    }

    /// <summary>Handle one request, translating every failure to the shared envelope.</summary>
    public Task HandleAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _ = _service;
        _ = _logger;
        // TODO(milestone 3): match the route and method, enforce the JSON body
        // rules, dispatch to the service, and write the shared error envelope on
        // failure (logging unexpected errors before sanitizing 500 responses).
        throw new IncompleteProjectException("milestone 3 low-level request handling");
    }
}
