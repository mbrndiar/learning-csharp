using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Tasks.Client;

namespace Tasks.Tests.Support;

/// <summary>
/// The shared client contract, expressed as reusable scenarios that run against
/// a named transport. Servers here are test-owned controlled endpoints so the
/// scenarios isolate the transport and CLI policy; a milestone selector against
/// the starter therefore fails with the transport's stable incomplete error.
/// </summary>
public static class ClientContract
{
    /// <summary>Normal task flows and a documented API failure map to exit codes.</summary>
    public static async Task RunsNormalAndApiFailureFlowsAsync(string client, CancellationToken ct)
    {
        await using ControlledServer server = await ControlledServer.StartAsync(TaskLikeHandler, ct);
        TransportFactory factory = ClientAdapters.Factory(client);

        ClientOutcome added = await ClientInvoker.InvokeAsync(factory, server.BaseUrl, ["add", "  Learn REST  "], ct);
        Assert.Equal(0, added.ExitCode);
        HttpAssertions.AssertTask(JsonDocument.Parse(added.Stdout).RootElement, 1, "Learn REST", false);

        ClientOutcome completed = await ClientInvoker.InvokeAsync(factory, server.BaseUrl, ["complete", "1"], ct);
        Assert.Equal(0, completed.ExitCode);
        Assert.True(JsonDocument.Parse(completed.Stdout).RootElement.GetProperty("completed").GetBoolean());

        ClientOutcome removed = await ClientInvoker.InvokeAsync(factory, server.BaseUrl, ["remove", "1"], ct);
        Assert.Equal(0, removed.ExitCode);
        Assert.Equal(1, JsonDocument.Parse(removed.Stdout).RootElement.GetProperty("deleted").GetInt64());

        ClientOutcome missing = await ClientInvoker.InvokeAsync(factory, server.BaseUrl, ["show", "999"], ct);
        Assert.Equal(ClientApplication.ExitApi, missing.ExitCode);
        Assert.StartsWith("api: 404 not_found:", missing.Stderr.Trim(), StringComparison.Ordinal);
    }

    /// <summary>A refused connection is reported as a transport failure.</summary>
    public static async Task TranslatesConnectionFailuresAsync(string client, CancellationToken ct)
    {
        string unusedUrl = ReserveUnusedLoopbackUrl();
        ClientOutcome outcome = await ClientInvoker.InvokeAsync(ClientAdapters.Factory(client), unusedUrl, ["list"], ct);
        Assert.Equal(ClientApplication.ExitTransport, outcome.ExitCode);
        Assert.StartsWith("connection:", outcome.Stderr.Trim(), StringComparison.Ordinal);
    }

    /// <summary>A non-JSON success body is reported as a malformed response.</summary>
    public static async Task ReportsMalformedResponsesAsync(string client, CancellationToken ct)
    {
        await using ControlledServer server = await ControlledServer.StartAsync(
            async context =>
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("not json", ct);
            },
            ct);

        ClientOutcome outcome = await ClientInvoker.InvokeAsync(ClientAdapters.Factory(client), server.BaseUrl, ["list"], ct);
        Assert.Equal(ClientApplication.ExitMalformedResponse, outcome.ExitCode);
        Assert.StartsWith("malformed-response:", outcome.Stderr.Trim(), StringComparison.Ordinal);
    }

    /// <summary>The transport does not follow redirects.</summary>
    public static async Task DoesNotFollowRedirectsAsync(string client, CancellationToken ct)
    {
        await using ControlledServer server = await ControlledServer.StartAsync(
            context =>
            {
                context.Response.StatusCode = 302;
                context.Response.Headers.Location = "/elsewhere";
                return Task.CompletedTask;
            },
            ct);

        await using ITaskTransport transport = ClientAdapters.Factory(client)(server.BaseUrl, TimeSpan.FromSeconds(5));
        TransportResponse response = await transport.SendAsync(new TransportRequest("GET", "/tasks"), ct);
        Assert.Equal(302, response.Status);
    }

    /// <summary>The transport enforces a finite timeout.</summary>
    public static async Task UsesFiniteTimeoutsAsync(string client, CancellationToken ct)
    {
        await using ControlledServer server = await ControlledServer.StartAsync(
            async context =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), context.RequestAborted);
                }
                catch (OperationCanceledException)
                {
                    // The client's finite timeout aborts the request as expected.
                }
            },
            ct);

        await using ITaskTransport transport = ClientAdapters.Factory(client)(server.BaseUrl, TimeSpan.FromMilliseconds(250));
        await Assert.ThrowsAsync<TransportTimeoutException>(
            () => transport.SendAsync(new TransportRequest("GET", "/tasks"), ct));
    }

    private static async Task TaskLikeHandler(HttpContext context)
    {
        string method = context.Request.Method;
        string path = context.Request.Path.Value ?? "/";
        if (method == "POST" && path == "/tasks")
        {
            await WriteJsonAsync(context, 201, """{"id":1,"title":"Learn REST","completed":false}""");
        }
        else if (method == "PATCH" && path == "/tasks/1")
        {
            await WriteJsonAsync(context, 200, """{"id":1,"title":"Learn REST","completed":true}""");
        }
        else if (method == "DELETE" && path == "/tasks/1")
        {
            context.Response.StatusCode = 204;
        }
        else if (method == "GET" && path == "/tasks/999")
        {
            await WriteJsonAsync(context, 404, """{"error":{"code":"not_found","message":"task 999 was not found"}}""");
        }
        else
        {
            await WriteJsonAsync(context, 404, """{"error":{"code":"not_found","message":"route was not found"}}""");
        }
    }

    private static async Task WriteJsonAsync(HttpContext context, int status, string json)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json, context.RequestAborted);
    }

    private static string ReserveUnusedLoopbackUrl()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return $"http://127.0.0.1:{port}";
    }
}
