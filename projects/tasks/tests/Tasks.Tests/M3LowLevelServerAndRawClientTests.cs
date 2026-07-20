using Tasks.Client;
using Tasks.Tests.Support;

namespace Tasks.Tests;

/// <summary>
/// Milestone 3: the low-level middleware server and the raw <see cref="System.Net.Http.HttpClient"/>
/// transport. The shared HTTP and client contracts run against the low-level
/// adapter and the raw transport respectively.
/// </summary>
public sealed class M3LowLevelServerAndRawClientTests
{
    private const string Server = ServerAdapters.LowLevel;
    private const string Client = ClientAdapters.Raw;

    [Fact]
    public Task ServerNormalFlow() => ServerContract.NormalFlowAsync(Server, TestContext.Current.CancellationToken);

    [Fact]
    public Task ServerRoutingErrors() => ServerContract.RoutingErrorsAsync(Server, TestContext.Current.CancellationToken);

    [Fact]
    public Task ServerBodyValidation() => ServerContract.BodyValidationAsync(Server, TestContext.Current.CancellationToken);

    [Fact]
    public Task ServerQueryAndIdValidation() => ServerContract.QueryAndIdValidationAsync(Server, TestContext.Current.CancellationToken);

    [Fact]
    public Task ServerSanitizesStorageFailures() => ServerContract.StorageFailureAsync(Server, TestContext.Current.CancellationToken);

    [Fact]
    public Task ClientRunsNormalAndApiFailureFlows() => ClientContract.RunsNormalAndApiFailureFlowsAsync(Client, TestContext.Current.CancellationToken);

    [Fact]
    public Task ClientTranslatesConnectionFailures() => ClientContract.TranslatesConnectionFailuresAsync(Client, TestContext.Current.CancellationToken);

    [Fact]
    public Task ClientReportsMalformedResponses() => ClientContract.ReportsMalformedResponsesAsync(Client, TestContext.Current.CancellationToken);

    [Fact]
    public Task ClientDoesNotFollowRedirects() => ClientContract.DoesNotFollowRedirectsAsync(Client, TestContext.Current.CancellationToken);

    [Fact]
    public Task ClientUsesFiniteTimeouts() => ClientContract.UsesFiniteTimeoutsAsync(Client, TestContext.Current.CancellationToken);

    [Theory]
    [InlineData("show 0")]
    [InlineData("update 1")]
    [InlineData("add")]
    [InlineData("nonsense")]
    [InlineData("list --completed perhaps")]
    public async Task ClientUsageFailuresAreConciseAndDoNotCreateTransport(string commandLine)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string[] args = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int exitCode = await ClientApplication.RunAsync(
            args,
            (_, _) => throw new InvalidOperationException("a usage error must not build a transport"),
            stdout,
            stderr,
            "cli",
            token);

        Assert.Equal(ClientApplication.ExitUsage, exitCode);
        Assert.StartsWith("usage:", stderr.ToString(), StringComparison.Ordinal);
        Assert.Empty(stdout.ToString());
    }

    [Fact]
    public async Task ClientOwnsAndDisposesItsTransport()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var transport = new RecordingTransport();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int exitCode = await ClientApplication.RunAsync(["list"], (_, _) => transport, stdout, stderr, "cli", token);

        Assert.Equal(ClientApplication.ExitSuccess, exitCode);
        Assert.True(transport.Disposed);
        Assert.Equal("[]", stdout.ToString().Trim());
    }

    [Fact]
    public async Task ClientDecodesSuccessfulResponses()
    {
        CancellationToken token = TestContext.Current.CancellationToken;

        ClientOutcome added = await RunCannedAsync(
            ["add", "Learn REST"], Json(201, """{"id":1,"title":"Learn REST","completed":false}"""), token);
        Assert.Equal(0, added.ExitCode);
        Assert.Contains("Learn REST", added.Stdout, StringComparison.Ordinal);

        ClientOutcome listed = await RunCannedAsync(
            ["list"],
            Json(200, """[{"id":1,"title":"a","completed":false},{"id":2,"title":"b","completed":true}]"""),
            token);
        Assert.Equal(0, listed.ExitCode);

        ClientOutcome removed = await RunCannedAsync(["remove", "3"], NoContent(), token);
        Assert.Equal(0, removed.ExitCode);
        Assert.Contains("\"deleted\":3", removed.Stdout, StringComparison.Ordinal);

        string taskBody = """{"id":1,"title":"Learn REST","completed":true}""";
        Assert.Equal(0, (await RunCannedAsync(["update", "1", "--title", "New"], Json(200, taskBody), token)).ExitCode);
        Assert.Equal(0, (await RunCannedAsync(["complete", "1"], Json(200, taskBody), token)).ExitCode);
    }

    [Fact]
    public async Task ClientReportsDocumentedApiErrors()
    {
        CancellationToken token = TestContext.Current.CancellationToken;

        ClientOutcome notFound = await RunCannedAsync(
            ["show", "9"], Json(404, """{"error":{"code":"not_found","message":"task 9 was not found"}}"""), token);
        Assert.Equal(ClientApplication.ExitApi, notFound.ExitCode);
        Assert.StartsWith("api: 404 not_found:", notFound.Stderr.Trim(), StringComparison.Ordinal);

        ClientOutcome validation = await RunCannedAsync(
            ["add", "x"],
            Json(422, """{"error":{"code":"validation_error","message":"bad","details":{"field":"title"}}}"""),
            token);
        Assert.Equal(ClientApplication.ExitApi, validation.ExitCode);
    }

    [Theory]
    [InlineData("show", "1", 200, "text/plain", "{}")]
    [InlineData("show", "1", 200, "application/json", "{not json")]
    [InlineData("show", "1", 200, "application/json", """{"id":1,"title":"a","completed":false,"extra":1}""")]
    [InlineData("show", "1", 200, "application/json", """{"id":0,"title":"a","completed":false}""")]
    [InlineData("show", "1", 200, "application/json", """{"id":1,"title":"  untrimmed  ","completed":false}""")]
    [InlineData("show", "1", 500, "application/json", """{"error":{"code":"not_found","message":"m"}}""")]
    [InlineData("show", "1", 418, "application/json", """{"error":{"code":"not_found","message":"m"}}""")]
    [InlineData("list", "", 200, "application/json", """{"not":"array"}""")]
    [InlineData("list", "", 200, "application/json", """[{"id":2,"title":"a","completed":false},{"id":1,"title":"b","completed":false}]""")]
    public async Task ClientReportsMalformedResponsesForBrokenBodies(
        string command, string argument, int status, string contentType, string body)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string[] args = string.IsNullOrEmpty(argument) ? [command] : [command, argument];
        ClientOutcome outcome = await RunCannedAsync(args, new CannedResponse(status, contentType, body), token);
        Assert.Equal(ClientApplication.ExitMalformedResponse, outcome.ExitCode);
        Assert.StartsWith("malformed-response:", outcome.Stderr.Trim(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClientRejectsDeleteResponsesThatCarryABody()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        ClientOutcome outcome = await RunCannedAsync(
            ["remove", "1"], new CannedResponse(204, "application/json", "{}"), token);
        Assert.Equal(ClientApplication.ExitMalformedResponse, outcome.ExitCode);
    }

    [Fact]
    public async Task ClientRejectsErrorEnvelopesThatMismatchTheStatus()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        ClientOutcome outcome = await RunCannedAsync(
            ["show", "1"], Json(404, """{"error":{"code":"validation_error","message":"m"}}"""), token);
        Assert.Equal(ClientApplication.ExitMalformedResponse, outcome.ExitCode);
    }

    [Theory]
    [InlineData("""{"error":"notobject"}""")]
    [InlineData("""{"error":{"code":"not_found"}}""")]
    [InlineData("""{"error":{"code":1,"message":"m"}}""")]
    [InlineData("""{"error":{"code":"not_found","message":""}}""")]
    [InlineData("""{"error":{"code":"not_found","message":"m"},"extra":1}""")]
    [InlineData("""{"error":{"code":"not_found","message":"m","details":"x"}}""")]
    public async Task ClientRejectsMalformedErrorEnvelopes(string body)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        ClientOutcome outcome = await RunCannedAsync(["show", "1"], Json(404, body), token);
        Assert.Equal(ClientApplication.ExitMalformedResponse, outcome.ExitCode);
    }

    [Theory]
    [InlineData("timeout", "connection: timeout:")]
    [InlineData("connection", "connection:")]
    [InlineData("transport", "transport:")]
    public async Task ClientTranslatesTransportExceptionCategories(string kind, string prefix)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int exit = await ClientApplication.RunAsync(
            ["list"],
            (_, _) => new ThrowingTransport(kind),
            stdout,
            stderr,
            "cli",
            token);

        Assert.Equal(ClientApplication.ExitTransport, exit);
        Assert.StartsWith(prefix, stderr.ToString().Trim(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClientTreatsFactoryFailureAsTransportFailure()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int exit = await ClientApplication.RunAsync(
            ["list"],
            (_, _) => throw new InvalidOperationException("cannot build"),
            stdout,
            stderr,
            "cli",
            token);

        Assert.Equal(ClientApplication.ExitTransport, exit);
        Assert.StartsWith("transport:", stderr.ToString().Trim(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClientTreatsCleanupFailureAsTransportFailure()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int exit = await ClientApplication.RunAsync(
            ["list"],
            (_, _) => new CleanupFailingTransport(),
            stdout,
            stderr,
            "cli",
            token);

        Assert.Equal(ClientApplication.ExitTransport, exit);
    }

    private static async Task<ClientOutcome> RunCannedAsync(string[] args, CannedResponse response, CancellationToken token)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        int exit = await ClientApplication.RunAsync(
            ["--base-url", "http://127.0.0.1:8000", .. args],
            (_, _) => new CannedTransport(response),
            stdout,
            stderr,
            "cli",
            token);
        return new ClientOutcome(exit, stdout.ToString(), stderr.ToString());
    }

    private static CannedResponse Json(int status, string body) => new(status, "application/json", body);

    private static CannedResponse NoContent() => new(204, null, string.Empty);

    private sealed record CannedResponse(int Status, string? ContentType, string Body);

    private sealed class CannedTransport : ITaskTransport
    {
        private readonly CannedResponse _response;

        public CannedTransport(CannedResponse response) => _response = response;

        public Task<TransportResponse> SendAsync(TransportRequest request, CancellationToken cancellationToken = default)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (_response.ContentType is not null)
            {
                headers["Content-Type"] = _response.ContentType;
            }

            byte[] body = System.Text.Encoding.UTF8.GetBytes(_response.Body);
            return Task.FromResult(new TransportResponse(_response.Status, headers, body));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class ThrowingTransport : ITaskTransport
    {
        private readonly string _kind;

        public ThrowingTransport(string kind) => _kind = kind;

        public Task<TransportResponse> SendAsync(TransportRequest request, CancellationToken cancellationToken = default)
            => throw _kind switch
            {
                "timeout" => new TransportTimeoutException("slow"),
                "connection" => new TransportConnectionException("refused"),
                _ => new TransportException("weird"),
            };

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class CleanupFailingTransport : ITaskTransport
    {
        public Task<TransportResponse> SendAsync(TransportRequest request, CancellationToken cancellationToken = default)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = "application/json",
            };
            return Task.FromResult(new TransportResponse(200, headers, "[]"u8.ToArray()));
        }

        public ValueTask DisposeAsync() => throw new TransportConnectionException("cleanup failed");
    }

    private sealed class RecordingTransport : ITaskTransport
    {
        public bool Disposed { get; private set; }

        public Task<TransportResponse> SendAsync(TransportRequest request, CancellationToken cancellationToken = default)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = "application/json",
            };
            return Task.FromResult(new TransportResponse(200, headers, "[]"u8.ToArray()));
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
