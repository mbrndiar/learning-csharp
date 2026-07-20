using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using LearningCSharp.Exercises.ApplicationComposition.Cli;
using LearningCSharp.Exercises.ApplicationComposition.Domain;

namespace LearningCSharp.Exercises.ApplicationComposition.Tests;

public sealed class HttpReadingLogSourceTests
{
    [Fact]
    public async Task LoadAsyncReturnsDeserializedEntriesFromTheConfiguredEndpoint()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ReadingEntry[] expected = [new("Deep Work", 304, 5), new("Refactoring", 448, 5)];
        using FakeJsonHandler handler = new(HttpStatusCode.OK, JsonSerializer.Serialize(expected));
        using HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        var source = new HttpReadingLogSource(httpClient);

        IReadOnlyList<ReadingEntry> entries = await source.LoadAsync("/entries", cancellationToken);

        Assert.Equal(2, entries.Count);
        Assert.Equal("Deep Work", entries[0].Title);
        Assert.Equal("/entries", handler.LastRequestUri?.PathAndQuery);
    }

    [Fact]
    public async Task LoadAsyncThrowsHttpRequestExceptionForServerErrors()
    {
        // This is exactly the failure class SummaryCommand maps to its own
        // distinct exit code (4): a network/server failure, not malformed
        // data and not a usage mistake.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using FakeJsonHandler handler = new(HttpStatusCode.ServiceUnavailable, string.Empty);
        using HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        var source = new HttpReadingLogSource(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => source.LoadAsync("/entries", cancellationToken));
    }

    [Fact]
    public async Task LoadAsyncThrowsInvalidDataExceptionForAnEmptyResponseBody()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using FakeJsonHandler handler = new(HttpStatusCode.OK, "null");
        using HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        var source = new HttpReadingLogSource(httpClient);

        await Assert.ThrowsAsync<InvalidDataException>(() => source.LoadAsync("/entries", cancellationToken));
    }

    [Theory]
    [InlineData("http", true)]
    [InlineData("Http", true)]
    [InlineData("HTTP", true)]
    [InlineData("file", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void SourceSelectorChoosesTheAdapterFromTheEnvironmentValueAlone(string? environmentValue, bool expectHttp)
    {
        // Verifies the composition root's decision logic directly, without
        // mutating the real process environment (which would risk flaky
        // parallel test runs).
        bool actual = ReadingLogSourceSelector.ShouldUseHttpSource(environmentValue);

        Assert.Equal(expectHttp, actual);
    }

    private sealed class FakeJsonHandler(HttpStatusCode statusCode, string jsonBody) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
