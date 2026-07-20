using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tasks.Core;
using Tasks.Http;

namespace Tasks.Tests.Support;

/// <summary>
/// The shared black-box HTTP contract, expressed as reusable scenarios that run
/// against a named server adapter. The low-level and Minimal API milestone
/// suites both invoke these so a status or envelope difference between the two
/// adapters is a test failure, and so a milestone selector produces stable red
/// feedback against the starter server.
/// </summary>
public static class ServerContract
{
    private static Task<LoopbackServer> StartAsync(string server, ITaskRepository repository, ILoggerProvider? logger, CancellationToken ct)
        => LoopbackServer.StartAsync(server, new TaskService(repository), logger, ct);

    /// <summary>Normal CRUD, completion filtering, and empty-body delete.</summary>
    public static async Task NormalFlowAsync(string server, CancellationToken ct)
    {
        await using LoopbackServer live = await StartAsync(server, new InMemoryTaskRepository(), null, ct);

        ProbeResponse health = await LoopbackProbe.SendAsync(live.BaseUrl, "GET", "/health", cancellationToken: ct);
        Assert.Equal(200, health.Status);
        Assert.Equal("ok", HttpAssertions.DecodeJson(health).GetProperty("status").GetString());

        ProbeResponse created =
            await LoopbackProbe.SendJsonAsync(live.BaseUrl, "POST", "/tasks", """{"title":"  Learn REST  "}""", ct);
        Assert.Equal(201, created.Status);
        HttpAssertions.AssertTask(HttpAssertions.DecodeJson(created), 1, "Learn REST", false);

        ProbeResponse patched =
            await LoopbackProbe.SendJsonAsync(live.BaseUrl, "PATCH", "/tasks/1", """{"completed":true}""", ct);
        HttpAssertions.AssertTask(HttpAssertions.DecodeJson(patched), 1, "Learn REST", true);

        JsonElement filtered = HttpAssertions.DecodeJson(
            await LoopbackProbe.SendAsync(live.BaseUrl, "GET", "/tasks?completed=true", cancellationToken: ct));
        Assert.Equal(1, filtered.GetArrayLength());
        HttpAssertions.AssertTask(filtered[0], 1, "Learn REST", true);
        Assert.Empty(HttpAssertions.DecodeJson(
            await LoopbackProbe.SendAsync(live.BaseUrl, "GET", "/tasks?completed=false", cancellationToken: ct)).EnumerateArray());

        ProbeResponse deleted = await LoopbackProbe.SendAsync(live.BaseUrl, "DELETE", "/tasks/1", cancellationToken: ct);
        Assert.Equal(204, deleted.Status);
        Assert.Empty(deleted.Body);

        HttpAssertions.AssertError(
            await LoopbackProbe.SendAsync(live.BaseUrl, "GET", "/tasks/1", cancellationToken: ct),
            404, ErrorCodes.NotFound, "task 1 was not found");
    }

    /// <summary>405 with an Allow header and 404 for unknown routes.</summary>
    public static async Task RoutingErrorsAsync(string server, CancellationToken ct)
    {
        await using LoopbackServer live = await StartAsync(server, new InMemoryTaskRepository(), null, ct);

        ProbeResponse health = await LoopbackProbe.SendAsync(live.BaseUrl, "DELETE", "/health", cancellationToken: ct);
        HttpAssertions.AssertError(health, 405, ErrorCodes.MethodNotAllowed, "method is not allowed for this path");
        Assert.Equal(new HashSet<string> { "GET" }, HttpAssertions.AllowMethods(health));

        ProbeResponse tasks = await LoopbackProbe.SendAsync(live.BaseUrl, "PUT", "/tasks", cancellationToken: ct);
        Assert.Equal(405, tasks.Status);
        Assert.Equal(new HashSet<string> { "GET", "POST" }, HttpAssertions.AllowMethods(tasks));

        ProbeResponse item = await LoopbackProbe.SendAsync(live.BaseUrl, "POST", "/tasks/1", cancellationToken: ct);
        Assert.Equal(405, item.Status);
        Assert.Equal(new HashSet<string> { "GET", "PATCH", "DELETE" }, HttpAssertions.AllowMethods(item));

        HttpAssertions.AssertError(
            await LoopbackProbe.SendAsync(live.BaseUrl, "GET", "/nope", cancellationToken: ct),
            404, ErrorCodes.NotFound, "route was not found");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendAsync(live.BaseUrl, "GET", "/tasks/1/extra", cancellationToken: ct),
            404, ErrorCodes.NotFound, "route was not found");
    }

    /// <summary>400 for JSON framing failures and 422 for invalid fields or shapes.</summary>
    public static async Task BodyValidationAsync(string server, CancellationToken ct)
    {
        await using LoopbackServer live = await StartAsync(server, new InMemoryTaskRepository(), null, ct);

        HttpAssertions.AssertError(
            await LoopbackProbe.SendAsync(live.BaseUrl, "POST", "/tasks", body: "{}"u8.ToArray(), cancellationToken: ct),
            400, ErrorCodes.InvalidJson, "request Content-Type must be application/json");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendAsync(live.BaseUrl, "POST", "/tasks", "text/plain", """{"title":"x"}"""u8.ToArray(), ct),
            400, ErrorCodes.InvalidJson, "request Content-Type must be application/json");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendJsonAsync(live.BaseUrl, "POST", "/tasks", "{not json", ct),
            400, ErrorCodes.InvalidJson, "request body must be valid JSON");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendAsync(live.BaseUrl, "POST", "/tasks", "application/json", [0xff, 0xfe], ct),
            400, ErrorCodes.InvalidJson, "request body must be valid JSON");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendJsonAsync(live.BaseUrl, "POST", "/tasks", """{"title":"a","title":"b"}""", ct),
            400, ErrorCodes.InvalidJson, "request body must be valid JSON");

        string big = $$"""{"title":"{{new string('x', 70_000)}}"}""";
        HttpAssertions.AssertError(
            await LoopbackProbe.SendJsonAsync(live.BaseUrl, "POST", "/tasks", big, ct),
            400, ErrorCodes.InvalidJson, "request body is too large");

        HttpAssertions.AssertError(
            await LoopbackProbe.SendJsonAsync(live.BaseUrl, "POST", "/tasks", """{"title":"x","done":true}""", ct),
            422, ErrorCodes.ValidationError, "unknown property: done", "done");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendJsonAsync(live.BaseUrl, "POST", "/tasks", "{}", ct),
            422, ErrorCodes.ValidationError, "missing property: title", "title");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendJsonAsync(live.BaseUrl, "POST", "/tasks", """{"title":123}""", ct),
            422, ErrorCodes.ValidationError, "title must be a string", "title");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendJsonAsync(live.BaseUrl, "POST", "/tasks", """{"title":"   "}""", ct),
            422, ErrorCodes.ValidationError, "title must contain between 1 and 120 characters", "title");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendJsonAsync(live.BaseUrl, "POST", "/tasks", "[]", ct),
            422, ErrorCodes.ValidationError, "request body must be a JSON object", "body");

        await LoopbackProbe.SendJsonAsync(live.BaseUrl, "POST", "/tasks", """{"title":"Present"}""", ct);
        HttpAssertions.AssertError(
            await LoopbackProbe.SendJsonAsync(live.BaseUrl, "PATCH", "/tasks/1", "{}", ct),
            422, ErrorCodes.ValidationError, "update must include title or completed", "update");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendJsonAsync(live.BaseUrl, "PATCH", "/tasks/1", """{"completed":"yes"}""", ct),
            422, ErrorCodes.ValidationError, "completed must be a Boolean", "completed");
    }

    /// <summary>422 for invalid filters, unknown queries, and invalid identifiers.</summary>
    public static async Task QueryAndIdValidationAsync(string server, CancellationToken ct)
    {
        await using LoopbackServer live = await StartAsync(server, new InMemoryTaskRepository(), null, ct);

        HttpAssertions.AssertError(
            await LoopbackProbe.SendAsync(live.BaseUrl, "GET", "/tasks?completed=maybe", cancellationToken: ct),
            422, ErrorCodes.ValidationError, "completed filter must be true or false", "completed");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendAsync(live.BaseUrl, "GET", "/tasks?colour=blue", cancellationToken: ct),
            422, ErrorCodes.ValidationError, "unknown query parameter: colour", "colour");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendAsync(live.BaseUrl, "GET", "/tasks/abc", cancellationToken: ct),
            422, ErrorCodes.ValidationError, "task ID must be a positive integer", "id");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendAsync(live.BaseUrl, "GET", "/tasks/0", cancellationToken: ct),
            422, ErrorCodes.ValidationError, "task ID must be a positive integer", "id");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendAsync(live.BaseUrl, "GET", "/health?x=1", cancellationToken: ct),
            422, ErrorCodes.ValidationError, "unknown query parameter: x", "x");
        HttpAssertions.AssertError(
            await LoopbackProbe.SendAsync(live.BaseUrl, "GET", "/tasks/9", cancellationToken: ct),
            404, ErrorCodes.NotFound, "task 9 was not found");
    }

    /// <summary>A storage failure is logged internally and sanitized on the wire.</summary>
    public static async Task StorageFailureAsync(string server, CancellationToken ct)
    {
        var logger = new CapturingLoggerProvider();
        await using LoopbackServer live = await StartAsync(server, new ThrowingTaskRepository(), logger, ct);

        ProbeResponse response = await LoopbackProbe.SendAsync(live.BaseUrl, "GET", "/tasks", cancellationToken: ct);
        HttpAssertions.AssertError(response, 500, ErrorCodes.InternalError, "the server could not complete the request");

        Assert.Contains(
            logger.Entries,
            entry => entry.Exception is not null
                     && entry.Exception.Contains("injected storage failure", StringComparison.Ordinal));
    }
}
