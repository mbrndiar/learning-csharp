using Tasks.Client;
using Tasks.Server.Configuration;
using Tasks.Server.Persistence;

namespace Tasks.Tests;

/// <summary>
/// Direct unit coverage of the provided client transport contracts: base URL
/// normalization, request construction, timeout validation, and additional CLI
/// parsing branches. These types are shared infrastructure in both trees.
/// </summary>
public sealed class TransportContractTests
{
    [Theory]
    [InlineData("http://127.0.0.1:8000", "http://127.0.0.1:8000")]
    [InlineData("http://127.0.0.1:8000/", "http://127.0.0.1:8000")]
    [InlineData("https://Example.COM:9000/api/", "https://example.com:9000/api")]
    [InlineData("http://[::1]:8080", "http://[::1]:8080")]
    public void NormalizeBaseUrlProducesUnambiguousUrls(string input, string expected)
        => Assert.Equal(expected, TransportUrls.NormalizeBaseUrl(input));

    [Theory]
    [InlineData("")]
    [InlineData(" http://x ")]
    [InlineData("http://x y")]
    [InlineData("ftp://host")]
    [InlineData("http://user:pass@host")]
    [InlineData("http://host?q=1")]
    [InlineData("http://host#frag")]
    [InlineData("not-a-url")]
    public void NormalizeBaseUrlRejectsUnsafeInputs(string input)
        => Assert.Throws<ArgumentException>(() => TransportUrls.NormalizeBaseUrl(input));

    [Fact]
    public void BuildUrlEncodesPathAndQuery()
    {
        var request = new TransportRequest(
            "GET",
            "/tasks",
            query: new Dictionary<string, string> { ["completed"] = "true" });
        Assert.Equal("http://127.0.0.1:8000/tasks?completed=true", TransportUrls.BuildAbsoluteUrl("http://127.0.0.1:8000", request));
        Assert.Equal("tasks?completed=true", TransportUrls.BuildRelativeUrl(request));

        var noQuery = new TransportRequest("GET", "/tasks/5");
        Assert.Equal("http://127.0.0.1:8000/tasks/5", TransportUrls.BuildAbsoluteUrl("http://127.0.0.1:8000", noQuery));
        Assert.Equal("tasks/5", TransportUrls.BuildRelativeUrl(noQuery));
    }

    [Fact]
    public void TransportRequestValidatesItsInputs()
    {
        var request = new TransportRequest(
            "POST",
            "/tasks",
            jsonBody: new Dictionary<string, object?> { ["title"] = "x", ["completed"] = true });
        Assert.Equal("POST", request.Method);
        Assert.NotNull(request.JsonBody);

        Assert.Throws<ArgumentException>(() => new TransportRequest("PUT", "/tasks"));
        Assert.Throws<ArgumentException>(() => new TransportRequest("GET", "tasks"));
        Assert.Throws<ArgumentException>(
            () => new TransportRequest("POST", "/tasks", jsonBody: new Dictionary<string, object?> { ["x"] = DateTime.Now }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TransportFactoriesRejectNonPositiveTimeouts(int seconds)
    {
        TimeSpan timeout = TimeSpan.FromSeconds(seconds);
        Assert.Throws<ArgumentOutOfRangeException>(() => TaskTransports.CreateRaw("http://127.0.0.1:8000", timeout));
        Assert.Throws<ArgumentOutOfRangeException>(() => TaskTransports.CreateTyped("http://127.0.0.1:8000", timeout));
    }

    [Fact]
    public void RepositoryFactoryRejectsUnknownBackends()
        => Assert.Throws<ArgumentOutOfRangeException>(() => RepositoryFactory.Create((StorageBackend)99, "x"));

    [Fact]
    public void RequestForCoversUpdateAndListBranches()
    {
        TransportRequest titleOnly = ClientApplication.RequestFor(new UpdateCommand(1, "T", null));
        Assert.True(titleOnly.JsonBody!.ContainsKey("title"));
        Assert.False(titleOnly.JsonBody.ContainsKey("completed"));

        TransportRequest completedOnly = ClientApplication.RequestFor(new UpdateCommand(1, null, true));
        Assert.True(completedOnly.JsonBody!.ContainsKey("completed"));
        Assert.False(completedOnly.JsonBody.ContainsKey("title"));

        Assert.Equal("false", ClientApplication.RequestFor(new ListCommand(false)).Query["completed"]);
        Assert.Empty(ClientApplication.RequestFor(new ListCommand(null)).Query);
    }

    [Fact]
    public void ParseRequestCoversEveryCommandAndOptionForm()
    {
        Assert.IsType<AddCommand>(ClientApplication.ParseRequest(["add", "Learn REST"]).Command);
        Assert.IsType<ListCommand>(ClientApplication.ParseRequest(["list"]).Command);
        Assert.IsType<ShowCommand>(ClientApplication.ParseRequest(["show", "3"]).Command);
        Assert.IsType<CompleteCommand>(ClientApplication.ParseRequest(["complete", "4"]).Command);
        Assert.IsType<RemoveCommand>(ClientApplication.ParseRequest(["remove", "5"]).Command);

        ClientRequest listFiltered = ClientApplication.ParseRequest(["list", "--completed", "true"]);
        Assert.True(((ListCommand)listFiltered.Command).Completed);

        ClientRequest settings = ClientApplication.ParseRequest(
            ["--base-url=http://127.0.0.1:9000", "--timeout=2.5", "show", "1"]);
        Assert.Equal("http://127.0.0.1:9000", settings.Settings.BaseUrl);
        Assert.Equal(TimeSpan.FromSeconds(2.5), settings.Settings.Timeout);

        UpdateCommand update = (UpdateCommand)ClientApplication.ParseRequest(
            ["update", "2", "--title", "New", "--completed", "true"]).Command;
        Assert.Equal("New", update.Title);
        Assert.True(update.Completed);
    }

    [Theory]
    [InlineData("--base-url")]
    [InlineData("--unknown", "value", "list")]
    [InlineData("--timeout", "-1", "list")]
    [InlineData("--timeout", "notnumber", "list")]
    [InlineData("--base-url", "ftp://x", "list")]
    [InlineData("list", "extra")]
    [InlineData("update", "1", "--completed", "perhaps")]
    [InlineData("show", "abc")]
    public void ParseRequestRejectsMalformedCommandLines(params string[] args)
        => Assert.Throws<ClientUsageException>(() => ClientApplication.ParseRequest(args));
}
