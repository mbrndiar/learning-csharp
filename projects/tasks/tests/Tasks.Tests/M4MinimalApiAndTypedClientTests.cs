using Tasks.Tests.Support;

namespace Tasks.Tests;

/// <summary>
/// Milestone 4: the Minimal API server and the typed <c>IHttpClientFactory</c>
/// transport. The shared HTTP and client contracts run against the Minimal API
/// adapter and the typed transport respectively, so both server styles and both
/// client styles are held to one contract.
/// </summary>
public sealed class M4MinimalApiAndTypedClientTests
{
    private const string Server = ServerAdapters.MinimalApi;
    private const string Client = ClientAdapters.Typed;

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
}
