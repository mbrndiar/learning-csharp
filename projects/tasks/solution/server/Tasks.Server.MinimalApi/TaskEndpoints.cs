using Tasks.Core;
using Tasks.Http;

namespace Tasks.Server.MinimalApi;

/// <summary>
/// Minimal API endpoint handlers. Each is a thin function that resolves the
/// shared <see cref="TaskService"/> through dependency injection, applies the
/// shared boundary policy, and returns a typed result. Error translation is
/// handled by the host's exception middleware and status-code pages.
/// </summary>
internal static class TaskEndpoints
{
    public static IResult Health(HttpContext context)
    {
        TaskHttpContract.RejectUnexpectedQuery(context.Request.Query.Keys, TaskHttpContract.NoQueryKeys);
        return TypedResults.Json(new HealthResponse("ok"), TaskJson.Options, "application/json");
    }

    public static async Task<IResult> List(HttpContext context, TaskService service, CancellationToken cancellationToken)
    {
        TaskHttpContract.RejectUnexpectedQuery(context.Request.Query.Keys, TaskHttpContract.CompletedQueryKeys);
        bool? completed = TaskHttpContract.ParseCompletedFilter(context.Request.Query["completed"]);
        IReadOnlyList<TaskItem> tasks = await service.ListTasksAsync(completed, cancellationToken).ConfigureAwait(false);
        TaskResponse[] payload = new TaskResponse[tasks.Count];
        for (int index = 0; index < tasks.Count; index++)
        {
            payload[index] = TaskHttpContract.ToResponse(tasks[index]);
        }

        return TypedResults.Json(payload, TaskJson.Options, "application/json");
    }

    public static async Task<IResult> Create(HttpContext context, TaskService service, CancellationToken cancellationToken)
    {
        TaskHttpContract.RejectUnexpectedQuery(context.Request.Query.Keys, TaskHttpContract.NoQueryKeys);
        TaskHttpContract.ValidateJsonContentType(context.Request.ContentType);
        byte[] body = await ApiResponses.ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
        string title = TaskHttpContract.DecodeCreateTitle(body);
        TaskItem created = await service.CreateTaskAsync(title, cancellationToken).ConfigureAwait(false);
        return TypedResults.Json(
            TaskHttpContract.ToResponse(created),
            TaskJson.Options,
            "application/json",
            StatusCodes.Status201Created);
    }

    public static async Task<IResult> Get(
        HttpContext context,
        TaskService service,
        string id,
        CancellationToken cancellationToken)
    {
        TaskHttpContract.RejectUnexpectedQuery(context.Request.Query.Keys, TaskHttpContract.NoQueryKeys);
        long taskId = TaskHttpContract.ParseTaskId(id);
        TaskItem task = await service.GetTaskAsync(taskId, cancellationToken).ConfigureAwait(false);
        return TypedResults.Json(TaskHttpContract.ToResponse(task), TaskJson.Options, "application/json");
    }

    public static async Task<IResult> Update(
        HttpContext context,
        TaskService service,
        string id,
        CancellationToken cancellationToken)
    {
        TaskHttpContract.RejectUnexpectedQuery(context.Request.Query.Keys, TaskHttpContract.NoQueryKeys);
        long taskId = TaskHttpContract.ParseTaskId(id);
        TaskHttpContract.ValidateJsonContentType(context.Request.ContentType);
        byte[] body = await ApiResponses.ReadBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
        UpdateTaskInput update = TaskHttpContract.DecodeUpdate(body);
        TaskItem updated = await service.UpdateTaskAsync(taskId, update, cancellationToken).ConfigureAwait(false);
        return TypedResults.Json(TaskHttpContract.ToResponse(updated), TaskJson.Options, "application/json");
    }

    public static async Task<IResult> Delete(
        HttpContext context,
        TaskService service,
        string id,
        CancellationToken cancellationToken)
    {
        TaskHttpContract.RejectUnexpectedQuery(context.Request.Query.Keys, TaskHttpContract.NoQueryKeys);
        long taskId = TaskHttpContract.ParseTaskId(id);
        await service.DeleteTaskAsync(taskId, cancellationToken).ConfigureAwait(false);
        return TypedResults.NoContent();
    }
}
