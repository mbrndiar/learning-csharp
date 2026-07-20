using Tasks.Api;
using Tasks.Client;
using Tasks.Core;
using Tasks.Core.Hosting;
using Tasks.Server;
using Tasks.Storage;
using Tasks.Tests.Support;

namespace Tasks.Tests;

/// <summary>
/// Starter smoke checks (compiled only for the starter tree): the scaffold keeps
/// each milestone deliberately incomplete with a stable
/// <see cref="IncompleteProjectException"/>, produces no storage side effects on
/// untouched paths, and leaves the provided contracts and parsers wired.
/// </summary>
public sealed class StarterSmokeTests
{
    [Fact]
    public async Task ServiceOperationsAreDeliberatelyIncomplete()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var service = new TaskService(new InMemoryTaskRepository());

        await Assert.ThrowsAsync<IncompleteProjectException>(() => service.CreateTaskAsync("Learn REST", token));
        await Assert.ThrowsAsync<IncompleteProjectException>(() => service.ListTasksAsync(null, token));
        await Assert.ThrowsAsync<IncompleteProjectException>(() => service.DeleteTaskAsync(1, token));
    }

    [Fact]
    public void DomainValidationIsDeliberatelyIncomplete()
    {
        Assert.Throws<IncompleteProjectException>(() => TaskValidation.ValidateTitle("Learn REST"));
        Assert.Throws<IncompleteProjectException>(() => TaskValidation.ValidateTaskId(1));
    }

    [Fact]
    public async Task RepositoryConstructionHasNoStorageSideEffects()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string dbPath = directory.File("tasks.db");
        string mdPath = directory.File("tasks.md");

        var sqlite = new SqliteTaskRepository(dbPath);
        var markdown = new MarkdownTaskRepository(mdPath);

        Assert.False(File.Exists(dbPath));
        Assert.False(File.Exists(mdPath));

        await Assert.ThrowsAsync<IncompleteProjectException>(() => sqlite.CreateAsync(new CreateTaskInput("x"), token));
        await Assert.ThrowsAsync<IncompleteProjectException>(() => markdown.ListAsync(cancellationToken: token));
    }

    [Fact]
    public void ServerHostsAreDeliberatelyIncomplete()
    {
        var service = new TaskService(new InMemoryTaskRepository());
        Assert.Throws<IncompleteProjectException>(() => TaskServerHost.Build(service));
        Assert.Throws<IncompleteProjectException>(() => TaskApiHost.Build(service));
    }

    [Fact]
    public async Task ClientExecutionIsIncompleteWhileParsingIsWired()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        await Assert.ThrowsAsync<IncompleteProjectException>(
            () => ClientApplication.RunAsync(
                ["add", "Learn REST"],
                (_, _) => throw new InvalidOperationException("unreached"),
                stdout,
                stderr,
                "cli",
                token));

        ClientRequest parsed = ClientApplication.ParseRequest(["update", "2", "--completed", "false"]);
        var update = Assert.IsType<UpdateCommand>(parsed.Command);
        Assert.False(update.Completed);
        Assert.Equal("PATCH", ClientApplication.RequestFor(update).Method);
    }

    [Fact]
    public void LauncherAndTransportContractsAreWired()
    {
        ServerSettings settings = ServerSettings.Parse(["--backend", "sqlite", "--data", "tasks.db"]);
        Assert.Equal(StorageBackend.Sqlite, settings.Backend);
        Assert.Equal("http://127.0.0.1:8000", TransportUrls.NormalizeBaseUrl("http://127.0.0.1:8000/"));
    }
}
