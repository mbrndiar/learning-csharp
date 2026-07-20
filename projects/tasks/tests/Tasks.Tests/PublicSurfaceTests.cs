using System.Reflection;
using Tasks.Api;
using Tasks.Client;
using Tasks.Core;
using Tasks.Core.Hosting;
using Tasks.Core.Http;
using Tasks.Server;
using Tasks.Storage;

namespace Tasks.Tests;

/// <summary>
/// Verifies the shared public surface. Because the harness compiles against the
/// selected tree, running this under both Starter and Solution proves their
/// public APIs match. The starter-only <c>IncompleteProjectException</c> scaffold
/// type is deliberately excluded from this parity check.
/// </summary>
public sealed class PublicSurfaceTests
{
    [Fact]
    public void CorePublicTypesAndMembersAreAvailable()
    {
        AssertPublicType(typeof(TaskItem));
        AssertPublicType(typeof(TaskService));
        AssertPublicType(typeof(ITaskRepository));
        AssertPublicType(typeof(CreateTaskInput));
        AssertPublicType(typeof(UpdateTaskInput));
        AssertPublicType(typeof(Maybe<int>).GetGenericTypeDefinition());
        AssertPublicType(typeof(TaskValidation));

        AssertMethod(typeof(TaskService), "CreateTaskAsync");
        AssertMethod(typeof(TaskService), "ListTasksAsync");
        AssertMethod(typeof(TaskService), "GetTaskAsync");
        AssertMethod(typeof(TaskService), "UpdateTaskAsync");
        AssertMethod(typeof(TaskService), "DeleteTaskAsync");
        AssertMethod(typeof(TaskValidation), "ValidateTitle");
        AssertMethod(typeof(TaskValidation), "ValidateTaskId");
        AssertMethod(typeof(UpdateTaskInput), "ApplyTo");

        Assert.NotNull(typeof(ITaskRepository).GetMethod("CreateAsync"));
        Assert.NotNull(typeof(ITaskRepository).GetMethod("DeleteAsync"));
    }

    [Fact]
    public void ContractAndHostingTypesAreAvailable()
    {
        AssertPublicType(typeof(TaskHttpContract));
        AssertPublicType(typeof(TaskResponse));
        AssertPublicType(typeof(HealthResponse));
        AssertPublicType(typeof(ErrorResponse));
        AssertPublicType(typeof(ApiErrorException));
        AssertPublicType(typeof(ServerSettings));
        AssertPublicType(typeof(ServerConfigurationException));
        Assert.True(typeof(StorageBackend).IsEnum);
        AssertMethod(typeof(ServerSettings), "Parse");
    }

    [Fact]
    public void StorageAndServerTypesAreAvailable()
    {
        AssertPublicType(typeof(SqliteTaskRepository));
        AssertPublicType(typeof(MarkdownTaskRepository));
        AssertPublicType(typeof(RepositoryFactory));
        AssertPublicType(typeof(LowLevelTaskHandler));
        AssertMethod(typeof(RepositoryFactory), "Create");
        AssertMethod(typeof(TaskServerHost), "Build");
        AssertMethod(typeof(TaskApiHost), "Build");

        Assert.True(typeof(ITaskRepository).IsAssignableFrom(typeof(SqliteTaskRepository)));
        Assert.True(typeof(ITaskRepository).IsAssignableFrom(typeof(MarkdownTaskRepository)));
    }

    [Fact]
    public void ClientTypesAndCommandsAreAvailable()
    {
        AssertPublicType(typeof(ITaskTransport));
        AssertPublicType(typeof(TransportRequest));
        AssertPublicType(typeof(TransportResponse));
        AssertPublicType(typeof(RawHttpClientTransport));
        AssertPublicType(typeof(TypedHttpClientTransport));
        AssertPublicType(typeof(TaskTransports));
        AssertPublicType(typeof(TransportUrls));
        AssertPublicType(typeof(ClientApplication));
        AssertPublicType(typeof(TransportTimeoutException));

        AssertMethod(typeof(ClientApplication), "RunAsync");
        AssertMethod(typeof(ClientApplication), "ParseRequest");
        AssertMethod(typeof(ClientApplication), "RequestFor");
        AssertMethod(typeof(TaskTransports), "CreateRaw");
        AssertMethod(typeof(TaskTransports), "CreateTyped");
        AssertMethod(typeof(TransportUrls), "NormalizeBaseUrl");

        foreach (Type command in new[]
                 {
                     typeof(AddCommand), typeof(ListCommand), typeof(ShowCommand),
                     typeof(UpdateCommand), typeof(CompleteCommand), typeof(RemoveCommand),
                 })
        {
            AssertPublicType(command);
            Assert.True(typeof(ClientCommand).IsAssignableFrom(command));
        }

        Assert.True(typeof(ITaskTransport).IsAssignableFrom(typeof(RawHttpClientTransport)));
        Assert.True(typeof(ITaskTransport).IsAssignableFrom(typeof(TypedHttpClientTransport)));
    }

    private static void AssertPublicType(Type type)
        => Assert.True(type.IsPublic || type.IsNestedPublic, $"{type.FullName} must be public");

    private static void AssertMethod(Type type, string name)
        => Assert.True(
            type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Any(method => string.Equals(method.Name, name, StringComparison.Ordinal)),
            $"{type.FullName} must expose {name}");
}
