using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Tasks.Client;
using Tasks.Core;
using Tasks.Core.Hosting;
using Tasks.Core.Http;
using Tasks.Storage;
using Tasks.Tests.Support;

namespace Tasks.Tests;

/// <summary>
/// Infrastructure and contract checks that hold for both implementation trees:
/// launcher composition, request mapping, the semantic OpenAPI contract, and its
/// agreement with the live routing table.
/// </summary>
public sealed class SharedInfraTests
{
    private static readonly string[] HealthMethods = ["GET"];
    private static readonly string[] TasksMethods = ["GET", "POST"];
    private static readonly string[] TaskMethods = ["GET", "PATCH", "DELETE"];

    [Fact]
    public async Task CheckedInOpenApiIsSemanticallyValidAndMatchesRouting()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string yaml = await File.ReadAllTextAsync(TestPaths.Docs("openapi.yaml"), token);
        JsonNode node = YamlToJson.Convert(yaml);

        ReadResult result = OpenApiDocument.Parse(node.ToJsonString(), "json", new OpenApiReaderSettings());
        Assert.Empty(result.Diagnostic!.Errors);

        OpenApiDocument document = result.Document!;
        Assert.Equal("Task REST API", document.Info.Title);
        Assert.Equal(
            new HashSet<string> { "/health", "/tasks", "/tasks/{taskId}" },
            document.Paths.Keys.ToHashSet(StringComparer.Ordinal));

        Assert.Equal("getHealth", OperationId(document, "/health", HttpMethod.Get));
        Assert.Equal("listTasks", OperationId(document, "/tasks", HttpMethod.Get));
        Assert.Equal("createTask", OperationId(document, "/tasks", HttpMethod.Post));
        Assert.Equal("getTask", OperationId(document, "/tasks/{taskId}", HttpMethod.Get));
        Assert.Equal("updateTask", OperationId(document, "/tasks/{taskId}", HttpMethod.Patch));
        Assert.Equal("deleteTask", OperationId(document, "/tasks/{taskId}", HttpMethod.Delete));

        IOpenApiSchema task = document.Components!.Schemas!["Task"];
        Assert.Equal(
            new HashSet<string> { "id", "title", "completed" },
            task.Properties!.Keys.ToHashSet(StringComparer.Ordinal));

        IOpenApiSchema code = document.Components.Schemas["Error"].Properties!["error"].Properties!["code"];
        HashSet<string> codes = code.Enum!.Select(value => value!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(
            new HashSet<string>
            {
                ErrorCodes.InvalidJson, ErrorCodes.NotFound, ErrorCodes.MethodNotAllowed,
                ErrorCodes.ValidationError, ErrorCodes.InternalError,
            },
            codes);

        // The documented methods per path must match the live routing table.
        Assert.True(DocumentedMethods(document, "/health").SetEquals(HealthMethods));
        Assert.True(DocumentedMethods(document, "/tasks").SetEquals(TasksMethods));
        Assert.True(DocumentedMethods(document, "/tasks/{taskId}").SetEquals(TaskMethods));
    }

    [Fact]
    public void RoutingContractMatchesTheDocumentedMethods()
    {
        Assert.Equal(HealthMethods, TaskHttpContract.AllowedMethods(TaskHttpContract.HealthRoute));
        Assert.Equal(TasksMethods, TaskHttpContract.AllowedMethods(TaskHttpContract.TasksRoute));
        Assert.Equal(TaskMethods, TaskHttpContract.AllowedMethods(TaskHttpContract.TaskRoute));

        Assert.Equal(TaskHttpContract.TaskRoute, TaskHttpContract.Match("/tasks/5")!.Value.Route);
        Assert.Null(TaskHttpContract.Match("/tasks/5/extra"));
    }

    [Fact]
    public void ServerSettingsParseDocumentedOptionsAndBoundaries()
    {
        ServerSettings settings = ServerSettings.Parse(
            ["--host", "127.0.0.1", "--port", "8765", "--backend", "markdown", "--data", "tasks.md"]);
        Assert.Equal("127.0.0.1", settings.Host);
        Assert.Equal(8765, settings.Port);
        Assert.Equal(StorageBackend.Markdown, settings.Backend);
        Assert.Equal("tasks.md", settings.DataPath);

        Assert.Equal(0, ServerSettings.Parse(["--backend", "sqlite", "--data", "t.db", "--port", "0"]).Port);
        Assert.Equal(65535, ServerSettings.Parse(["--backend", "sqlite", "--data", "t.db", "--port", "65535"]).Port);
        Assert.Equal("localhost", ServerSettings.Parse(["--host", "localhost", "--backend", "sqlite", "--data", "t.db"]).Host);
    }

    [Theory]
    [InlineData("--host", "8.8.8.8", "--backend", "sqlite", "--data", "t.db")]
    [InlineData("--port", "70000", "--backend", "sqlite", "--data", "t.db")]
    [InlineData("--backend", "postgres", "--data", "t.db")]
    [InlineData("--backend", "sqlite")]
    public void ServerSettingsRejectInvalidLauncherArguments(params string[] args)
    {
        Assert.Throws<ServerConfigurationException>(() => ServerSettings.Parse(args));
    }

    [Fact]
    public void RepositoryFactorySelectsTheConfiguredBackend()
    {
        using var directory = new TempDirectory();
        Assert.IsType<SqliteTaskRepository>(RepositoryFactory.Create(StorageBackend.Sqlite, directory.File("t.db")));
        Assert.IsType<MarkdownTaskRepository>(RepositoryFactory.Create(StorageBackend.Markdown, directory.File("t.md")));
    }

    [Fact]
    public void ParseRequestPreservesFalseInsteadOfTreatingItAsOmitted()
    {
        ClientRequest request = ClientApplication.ParseRequest(["update", "1", "--completed", "false"]);
        var update = Assert.IsType<UpdateCommand>(request.Command);
        Assert.Equal(1, update.TaskId);
        Assert.Null(update.Title);
        Assert.False(update.Completed);

        TransportRequest transport = ClientApplication.RequestFor(update);
        Assert.Equal("PATCH", transport.Method);
        Assert.Equal("/tasks/1", transport.Path);
        Assert.Equal(false, transport.JsonBody!["completed"]);
        Assert.False(transport.JsonBody.ContainsKey("title"));
    }

    [Fact]
    public void CommandsBuildTheDocumentedRequests()
    {
        Assert.Equal("/tasks", ClientApplication.RequestFor(new AddCommand("x")).Path);
        Assert.Equal("POST", ClientApplication.RequestFor(new AddCommand("x")).Method);

        TransportRequest complete = ClientApplication.RequestFor(new CompleteCommand(5));
        Assert.Equal("PATCH", complete.Method);
        Assert.Equal("/tasks/5", complete.Path);
        Assert.Equal(true, complete.JsonBody!["completed"]);

        Assert.Equal("true", ClientApplication.RequestFor(new ListCommand(true)).Query["completed"]);
        Assert.Equal("DELETE", ClientApplication.RequestFor(new RemoveCommand(9)).Method);
    }

    private static string? OperationId(OpenApiDocument document, string path, HttpMethod method)
        => document.Paths[path].Operations![method].OperationId;

    private static HashSet<string> DocumentedMethods(OpenApiDocument document, string path)
        => document.Paths[path].Operations!.Keys.Select(method => method.Method).ToHashSet(StringComparer.Ordinal);
}
