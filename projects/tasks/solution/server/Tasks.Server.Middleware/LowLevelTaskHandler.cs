using System.Buffers;
using Tasks.Core;
using Tasks.Http;

namespace Tasks.Server.Middleware;

/// <summary>
/// Low-level HTTP boundary implemented as a single <see cref="RequestDelegate"/>.
/// Unlike the Minimal API adapter, this handler owns routing, method dispatch,
/// byte reading, content-length bounds, JSON decoding, header and status
/// selection, and error translation explicitly, exposing the mechanics a
/// framework usually hides.
/// </summary>
public sealed partial class LowLevelTaskHandler
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
    public async Task HandleAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            await DispatchAsync(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await WriteErrorAsync(context, exception).ConfigureAwait(false);
        }
    }

    private async Task DispatchAsync(HttpContext context)
    {
        CancellationToken cancellationToken = context.RequestAborted;
        HttpRequest request = context.Request;
        string method = request.Method;
        string path = request.Path.HasValue ? request.Path.Value! : "/";

        RouteMatch? match = TaskHttpContract.Match(path)
            ?? throw ApiErrorException.RouteNotFound();
        RouteMatch route = match.Value;

        IReadOnlyList<string> allowed = TaskHttpContract.AllowedMethods(route.Route);
        if (!allowed.Contains(method))
        {
            throw ApiErrorException.MethodNotAllowed(allowed);
        }

        bool isTaskList = string.Equals(route.Route, TaskHttpContract.TasksRoute, StringComparison.Ordinal)
            && string.Equals(method, "GET", StringComparison.Ordinal);
        if (!isTaskList)
        {
            TaskHttpContract.RejectUnexpectedQuery(request.Query.Keys, TaskHttpContract.NoQueryKeys);
        }

        switch (route.Route)
        {
            case TaskHttpContract.HealthRoute:
                await WriteJsonAsync(context, StatusCodes.Status200OK, TaskHttpContract.SerializeHealth())
                    .ConfigureAwait(false);
                return;
            case TaskHttpContract.TasksRoute:
                await DispatchTasksAsync(context, method, cancellationToken).ConfigureAwait(false);
                return;
            default:
                await DispatchTaskAsync(context, method, route.IdText!, cancellationToken).ConfigureAwait(false);
                return;
        }
    }

    private async Task DispatchTasksAsync(HttpContext context, string method, CancellationToken cancellationToken)
    {
        if (string.Equals(method, "POST", StringComparison.Ordinal))
        {
            TaskHttpContract.ValidateJsonContentType(context.Request.ContentType);
            byte[] body = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
            string title = TaskHttpContract.DecodeCreateTitle(body);
            TaskItem created = await _service.CreateTaskAsync(title, cancellationToken).ConfigureAwait(false);
            await WriteJsonAsync(context, StatusCodes.Status201Created, TaskHttpContract.SerializeTask(created))
                .ConfigureAwait(false);
            return;
        }

        TaskHttpContract.RejectUnexpectedQuery(context.Request.Query.Keys, TaskHttpContract.CompletedQueryKeys);
        bool? completed = TaskHttpContract.ParseCompletedFilter(context.Request.Query["completed"]);
        IReadOnlyList<TaskItem> tasks = await _service.ListTasksAsync(completed, cancellationToken)
            .ConfigureAwait(false);
        await WriteJsonAsync(context, StatusCodes.Status200OK, TaskHttpContract.SerializeTasks(tasks))
            .ConfigureAwait(false);
    }

    private async Task DispatchTaskAsync(
        HttpContext context,
        string method,
        string idText,
        CancellationToken cancellationToken)
    {
        long taskId = TaskHttpContract.ParseTaskId(idText);

        if (string.Equals(method, "GET", StringComparison.Ordinal))
        {
            TaskItem task = await _service.GetTaskAsync(taskId, cancellationToken).ConfigureAwait(false);
            await WriteJsonAsync(context, StatusCodes.Status200OK, TaskHttpContract.SerializeTask(task))
                .ConfigureAwait(false);
            return;
        }

        if (string.Equals(method, "DELETE", StringComparison.Ordinal))
        {
            await _service.DeleteTaskAsync(taskId, cancellationToken).ConfigureAwait(false);
            await WriteEmptyAsync(context, StatusCodes.Status204NoContent).ConfigureAwait(false);
            return;
        }

        TaskHttpContract.ValidateJsonContentType(context.Request.ContentType);
        byte[] body = await ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
        UpdateTaskInput update = TaskHttpContract.DecodeUpdate(body);
        TaskItem updated = await _service.UpdateTaskAsync(taskId, update, cancellationToken).ConfigureAwait(false);
        await WriteJsonAsync(context, StatusCodes.Status200OK, TaskHttpContract.SerializeTask(updated))
            .ConfigureAwait(false);
    }

    private static async Task<byte[]> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength is long declared && declared > TaskHttpContract.MaxRequestBodyBytes)
        {
            throw ApiErrorException.PayloadTooLarge();
        }

        using var buffer = new MemoryStream();
        byte[] rented = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            int read;
            while ((read = await request.Body.ReadAsync(rented, cancellationToken).ConfigureAwait(false)) > 0)
            {
                if (buffer.Length + read > TaskHttpContract.MaxRequestBodyBytes)
                {
                    throw ApiErrorException.PayloadTooLarge();
                }

                buffer.Write(rented, 0, read);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }

        return buffer.ToArray();
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        MappedError mapped = TaskHttpContract.Describe(exception);
        if (mapped.StatusCode == StatusCodes.Status500InternalServerError)
        {
            LogRequestFailed(exception);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        await WriteJsonAsync(
                context,
                mapped.StatusCode,
                TaskHttpContract.SerializeError(mapped.Body),
                mapped.Allow)
            .ConfigureAwait(false);
    }

    private static async Task WriteJsonAsync(HttpContext context, int status, byte[] body, string? allow = null)
    {
        HttpResponse response = context.Response;
        response.StatusCode = status;
        response.ContentType = "application/json";
        if (allow is not null)
        {
            response.Headers.Allow = allow;
        }

        response.ContentLength = body.Length;
        await response.Body.WriteAsync(body, context.RequestAborted).ConfigureAwait(false);
    }

    private static Task WriteEmptyAsync(HttpContext context, int status)
    {
        context.Response.StatusCode = status;
        context.Response.ContentLength = 0;
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1000, Level = LogLevel.Error, Message = "Low-level Task API request failed")]
    private partial void LogRequestFailed(Exception exception);
}
