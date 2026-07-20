using System.Globalization;
using System.Text.Json;
using Tasks.Client;
using Tasks.Core;
using Tasks.Server.Persistence;
using Tasks.Tests.Support;

namespace Tasks.Tests;

/// <summary>
/// Milestone 5: interoperability across both servers and both clients, server
/// lifecycle, and the CLI run as a real process.
/// </summary>
public sealed class M5InteropTests
{
    [Theory]
    [MemberData(nameof(InteropMatrix.Pairs), MemberType = typeof(InteropMatrix))]
    public async Task EveryClientInteroperatesWithEveryServer(string server, string client)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        var service = new TaskService(new MarkdownTaskRepository(directory.File("tasks.md")));
        await using LoopbackServer live = await LoopbackServer.StartAsync(server, service, cancellationToken: token);
        TransportFactory factory = ClientAdapters.Factory(client);

        ClientOutcome added = await ClientInvoker.InvokeAsync(factory, live.BaseUrl, ["add", "Interop"], token);
        Assert.Equal(0, added.ExitCode);
        long id = JsonDocument.Parse(added.Stdout).RootElement.GetProperty("id").GetInt64();

        ClientOutcome completed = await ClientInvoker.InvokeAsync(
            factory, live.BaseUrl, ["complete", id.ToString(CultureInfo.InvariantCulture)], token);
        Assert.Equal(0, completed.ExitCode);

        ClientOutcome listed = await ClientInvoker.InvokeAsync(factory, live.BaseUrl, ["list", "--completed", "true"], token);
        Assert.Equal(0, listed.ExitCode);
        JsonElement tasks = JsonDocument.Parse(listed.Stdout).RootElement;
        Assert.Equal(1, tasks.GetArrayLength());
        Assert.Equal(id, tasks[0].GetProperty("id").GetInt64());

        ClientOutcome removed = await ClientInvoker.InvokeAsync(
            factory, live.BaseUrl, ["remove", id.ToString(CultureInfo.InvariantCulture)], token);
        Assert.Equal(0, removed.ExitCode);
    }

    [Fact]
    public async Task ServerReleasesItsPortOnShutdown()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var service = new TaskService(new InMemoryTaskRepository());
        LoopbackServer live = await LoopbackServer.StartAsync(ServerAdapters.MinimalApi, service, cancellationToken: token);
        string baseUrl = live.BaseUrl;

        Assert.Equal(200, (await LoopbackProbe.SendAsync(baseUrl, "GET", "/health", cancellationToken: token)).Status);

        await live.DisposeAsync();

        await Assert.ThrowsAnyAsync<HttpRequestException>(
            () => LoopbackProbe.SendAsync(baseUrl, "GET", "/health", cancellationToken: token));
    }

    [Fact]
    public async Task ProcessCliRunsNormalAndApiFailureFlows()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var service = new TaskService(new InMemoryTaskRepository());
        await using LoopbackServer live = await LoopbackServer.StartAsync(ServerAdapters.LowLevel, service, cancellationToken: token);

        ClientOutcome added = await CliRunner.RunAsync(
            ["--transport", "typed", "--base-url", live.BaseUrl, "add", "From a process"],
            token);
        Assert.Equal(0, added.ExitCode);
        Assert.Equal("From a process", JsonDocument.Parse(added.Stdout).RootElement.GetProperty("title").GetString());

        ClientOutcome missing = await CliRunner.RunAsync(
            ["--transport", "raw", "--base-url", live.BaseUrl, "show", "404"],
            token);
        Assert.Equal(ClientApplication.ExitApi, missing.ExitCode);
        Assert.Contains("not_found", missing.Stderr, StringComparison.Ordinal);
    }
}
