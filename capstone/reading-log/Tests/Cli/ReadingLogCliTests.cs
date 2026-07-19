using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using ReadingLog.Tests.TestInfrastructure;

namespace ReadingLog.Tests.Cli;

public sealed class ReadingLogCliTests
{
    [Fact]
    public async Task ListBooksWritesBooksToStdout()
    {
        using var httpClient = CreateHttpClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            return Task.FromResult(CreateJsonResponse<IReadOnlyList<ReadingLog.Cli.BookResponse>>(HttpStatusCode.OK, [new ReadingLog.Cli.BookResponse(Guid.NewGuid(), "Dune", "Frank Herbert", 1965, null, SampleData.FirstCreatedAt)]));
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var app = new CliApplication(new ReadingLogApiClient(httpClient), standardOutput, standardError);

        var exitCode = await app.RunAsync(["list-books"], CancellationToken.None);

        Assert.Equal((int)CliExitCode.Success, exitCode);
        Assert.Contains("Dune by Frank Herbert", standardOutput.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, standardError.ToString());
    }

    [Fact]
    public async Task ListBooksWritesNoBooksMessageWhenCatalogIsEmpty()
    {
        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(CreateJsonResponse<IReadOnlyList<ReadingLog.Cli.BookResponse>>(HttpStatusCode.OK, Array.Empty<ReadingLog.Cli.BookResponse>())));
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var app = new CliApplication(new ReadingLogApiClient(httpClient), standardOutput, standardError);

        var exitCode = await app.RunAsync(["list-books"], CancellationToken.None);

        Assert.Equal((int)CliExitCode.Success, exitCode);
        Assert.Contains("No books yet.", standardOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShowBookWritesDetailsWithoutEntries()
    {
        using var httpClient = CreateHttpClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            return Task.FromResult(CreateJsonResponse(
                HttpStatusCode.OK,
                new ReadingLog.Cli.BookDetailsResponse(
                    new ReadingLog.Cli.BookResponse(Guid.NewGuid(), "Dune", "Frank Herbert", 1965, null, SampleData.FirstCreatedAt),
                    Array.Empty<ReadingLog.Cli.ReadingEntryResponse>(),
                    0,
                    false,
                    null)));
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var app = new CliApplication(new ReadingLogApiClient(httpClient), standardOutput, standardError);

        var exitCode = await app.RunAsync(["show-book", "--book-id", Guid.NewGuid().ToString()], CancellationToken.None);

        Assert.Equal((int)CliExitCode.Success, exitCode);
        Assert.Contains("Finished: no", standardOutput.ToString(), StringComparison.Ordinal);
        Assert.Contains("No entries yet.", standardOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddBookPostsJsonAndPrintsSuccessMessage()
    {
        using var httpClient = CreateHttpClient(async (request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            var payload = await request.Content!.ReadFromJsonAsync<CreateBookRequest>(cancellationToken);
            Assert.NotNull(payload);
            Assert.Equal("Dune", payload.Title);
            Assert.Equal(1965, payload.PublicationYear);

            return CreateJsonResponse(
                HttpStatusCode.Created,
                new ReadingLog.Cli.BookResponse(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Dune", "Frank Herbert", 1965, null, SampleData.FirstCreatedAt));
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var app = new CliApplication(new ReadingLogApiClient(httpClient), standardOutput, standardError);

        var exitCode = await app.RunAsync(["add-book", "--title", "Dune", "--author", "Frank Herbert", "--year", "1965"], CancellationToken.None);

        Assert.Equal((int)CliExitCode.Success, exitCode);
        Assert.Contains("Added book Dune", standardOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddEntryPostsJsonAndPrintsSuccessMessage()
    {
        var bookId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        using var httpClient = CreateHttpClient(async (request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            var payload = await request.Content!.ReadFromJsonAsync<CreateReadingEntryRequest>(cancellationToken);
            Assert.NotNull(payload);
            Assert.Equal(bookId, payload.BookId);
            Assert.Equal(45, payload.PagesRead);

            return CreateJsonResponse(
                HttpStatusCode.Created,
                new ReadingLog.Cli.ReadingEntryResponse(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), bookId, new DateOnly(2026, 7, 19), null, 45, null, null, SampleData.SecondCreatedAt));
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var app = new CliApplication(new ReadingLogApiClient(httpClient), standardOutput, standardError);

        var exitCode = await app.RunAsync(
            ["add-entry", "--book-id", bookId.ToString(), "--started-on", "2026-07-19", "--pages-read", "45"],
            CancellationToken.None);

        Assert.Equal((int)CliExitCode.Success, exitCode);
        Assert.Contains("Added entry", standardOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncReturnsInvalidArgumentsForUnknownCommand()
    {
        using var httpClient = CreateHttpClient((_, _) => throw new InvalidOperationException("Unknown commands should not make HTTP requests."));
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var app = new CliApplication(new ReadingLogApiClient(httpClient), standardOutput, standardError);

        var exitCode = await app.RunAsync(["wat"], CancellationToken.None);

        Assert.Equal((int)CliExitCode.InvalidArguments, exitCode);
        Assert.Contains("Unknown command", standardError.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncReturnsInvalidArgumentsForBadAddBookYear()
    {
        using var httpClient = CreateHttpClient((_, _) => throw new InvalidOperationException("Validation failures should not make HTTP requests."));
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var app = new CliApplication(new ReadingLogApiClient(httpClient), standardOutput, standardError);

        var exitCode = await app.RunAsync(["add-book", "--title", "Dune", "--author", "Frank Herbert", "--year", "nineteen"], CancellationToken.None);

        Assert.Equal((int)CliExitCode.InvalidArguments, exitCode);
        Assert.Contains("--year must be a whole number.", standardError.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncReturnsInvalidArgumentsForBadAddEntryArguments()
    {
        using var httpClient = CreateHttpClient((_, _) => throw new InvalidOperationException("Validation failures should not make HTTP requests."));
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var app = new CliApplication(new ReadingLogApiClient(httpClient), standardOutput, standardError);

        var exitCode = await app.RunAsync(
            ["add-entry", "--book-id", Guid.NewGuid().ToString(), "--started-on", "2026-07-19", "--pages-read", "45", "--finished-on", "bad-date"],
            CancellationToken.None);

        Assert.Equal((int)CliExitCode.InvalidArguments, exitCode);
        Assert.Contains("--finished-on must use yyyy-MM-dd.", standardError.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncRejectsNonIsoCalendarDate()
    {
        using var httpClient = CreateHttpClient((_, _) =>
            throw new InvalidOperationException("Validation failures should not make HTTP requests."));
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var app = new CliApplication(new ReadingLogApiClient(httpClient), standardOutput, standardError);

        var exitCode = await app.RunAsync(
            ["add-entry", "--book-id", Guid.NewGuid().ToString(), "--started-on", "07/19/2026", "--pages-read", "45"],
            CancellationToken.None);

        Assert.Equal((int)CliExitCode.InvalidArguments, exitCode);
        Assert.Contains("--started-on <yyyy-MM-dd>", standardError.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncReturnsRequestFailedWhenApiReturnsProblemStatus()
    {
        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(CreateProblemResponse(HttpStatusCode.InternalServerError, "Stored data is malformed.", "Fix the file.")));
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var app = new CliApplication(new ReadingLogApiClient(httpClient), standardOutput, standardError);

        var exitCode = await app.RunAsync(["list-books"], CancellationToken.None);

        Assert.Equal((int)CliExitCode.RequestFailed, exitCode);
        Assert.Contains("Stored data is malformed.", standardError.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncReturnsNotFoundWhenApiReturnsNotFound()
    {
        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(CreateProblemResponse(HttpStatusCode.NotFound, "Book not found.", "Missing.")));
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var app = new CliApplication(new ReadingLogApiClient(httpClient), standardOutput, standardError);

        var exitCode = await app.RunAsync(["list-books"], CancellationToken.None);

        Assert.Equal((int)CliExitCode.NotFound, exitCode);
        Assert.Contains("Book not found.", standardError.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsyncReturnsUnexpectedResponseWhenJsonIsMalformed()
    {
        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json", Encoding.UTF8, "application/json"),
            }));
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var app = new CliApplication(new ReadingLogApiClient(httpClient), standardOutput, standardError);

        var exitCode = await app.RunAsync(["list-books"], CancellationToken.None);

        Assert.Equal((int)CliExitCode.UnexpectedResponse, exitCode);
        Assert.Contains("malformed JSON", standardError.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsyncReturnsTimedOutWhenRequestExceedsTimeout()
    {
        using var httpClient = CreateHttpClient(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable");
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var app = new CliApplication(
            new ReadingLogApiClient(httpClient, new ReadingLogApiClientOptions { RequestTimeout = TimeSpan.FromMilliseconds(50) }),
            standardOutput,
            standardError);

        var exitCode = await app.RunAsync(["list-books"], CancellationToken.None);

        Assert.Equal((int)CliExitCode.TimedOut, exitCode);
        Assert.Contains("timed out", standardError.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiClientTimesOutWhenSuccessBodyIsSlow()
    {
        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new DelayedStringContent("[]", "application/json", TimeSpan.FromMilliseconds(150)),
            }));
        var client = new ReadingLogApiClient(httpClient, new ReadingLogApiClientOptions { RequestTimeout = TimeSpan.FromMilliseconds(50) });

        var exception = await Assert.ThrowsAsync<TimeoutException>(() => client.ListBooksAsync(CancellationToken.None));

        Assert.Contains("timed out", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiClientTimesOutWhenProblemBodyIsSlow()
    {
        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new DelayedStringContent("""{"title":"Bad request","detail":"Too slow","status":400}""", "application/json", TimeSpan.FromMilliseconds(150)),
            }));
        var client = new ReadingLogApiClient(httpClient, new ReadingLogApiClientOptions { RequestTimeout = TimeSpan.FromMilliseconds(50) });

        var exception = await Assert.ThrowsAsync<TimeoutException>(() => client.ListBooksAsync(CancellationToken.None));

        Assert.Contains("timed out", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsyncReturnsCancelledWhenTokenIsCancelled()
    {
        using var httpClient = CreateHttpClient(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            throw new InvalidOperationException("Unreachable");
        });
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var app = new CliApplication(new ReadingLogApiClient(httpClient), standardOutput, standardError);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var exitCode = await app.RunAsync(["list-books"], cancellationTokenSource.Token);

        Assert.Equal((int)CliExitCode.Cancelled, exitCode);
        Assert.Contains("cancelled", standardError.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiClientPreservesCallerCancellationWhileReadingSuccessBody()
    {
        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new DelayedStringContent("[]", "application/json", TimeSpan.FromMilliseconds(250)),
            }));
        var client = new ReadingLogApiClient(httpClient, new ReadingLogApiClientOptions { RequestTimeout = TimeSpan.FromSeconds(5) });
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ListBooksAsync(cancellationTokenSource.Token));
    }

    [Fact]
    public async Task CliProgramPrintsHelpWhenRunAsProcess()
    {
        var result = await RunCliProcessAsync("help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("ReadingLog CLI", result.StandardOutput, StringComparison.Ordinal);
        Assert.Equal(string.Empty, result.StandardError);
    }

    [Fact]
    public async Task CliProgramReturnsFailureWhenBaseUrlValueIsMissing()
    {
        var result = await RunCliProcessAsync("--base-url");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("--base-url requires a value.", result.StandardError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CliProgramAcceptsExplicitBaseUrlBeforeHelp()
    {
        var result = await RunCliProcessAsync("--base-url", "http://127.0.0.1:5071/", "help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("ReadingLog CLI", result.StandardOutput, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("file:///tmp/reading-log")]
    public async Task CliProgramReturnsFailureForInvalidBaseUrl(string baseUrl)
    {
        var result = await RunCliProcessAsync("--base-url", baseUrl, "help");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("--base-url must be an absolute HTTP or HTTPS URL.", result.StandardError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApiClientUsesFallbackMessageWhenProblemBodyIsEmpty()
    {
        using var httpClient = CreateHttpClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)));
        var client = new ReadingLogApiClient(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.ListBooksAsync(CancellationToken.None));

        Assert.Contains("502", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApiClientReturnsRawProblemBodyWhenItIsNotJson()
    {
        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("plain error", Encoding.UTF8, "text/plain"),
            }));
        var client = new ReadingLogApiClient(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.ListBooksAsync(CancellationToken.None));

        Assert.Equal("plain error", exception.Message);
    }

    [Fact]
    public async Task ApiClientDisposesResponseAfterSuccessfulRead()
    {
        var wasDisposed = false;
        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new DelayedStringContent(
                    "[]",
                    "application/json",
                    TimeSpan.Zero,
                    () => wasDisposed = true),
            }));
        var client = new ReadingLogApiClient(httpClient);

        var books = await client.ListBooksAsync(CancellationToken.None);

        Assert.Empty(books);
        Assert.True(wasDisposed);
    }

    [Fact]
    public async Task ApiClientDisposesResponseAfterProblemRead()
    {
        var wasDisposed = false;
        using var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new DelayedStringContent(
                    """{"title":"Bad request","detail":"Problem payload","status":400}""",
                    "application/json",
                    TimeSpan.Zero,
                    () => wasDisposed = true),
            }));
        var client = new ReadingLogApiClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.ListBooksAsync(CancellationToken.None));

        Assert.True(wasDisposed);
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
        new(new StubHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("http://127.0.0.1:5071"),
        };

    private static HttpResponseMessage CreateJsonResponse<T>(HttpStatusCode statusCode, T payload) =>
        new(statusCode)
        {
            Content = JsonContent.Create(payload),
        };

    private static HttpResponseMessage CreateProblemResponse(HttpStatusCode statusCode, string title, string detail) =>
        new(statusCode)
        {
            Content = JsonContent.Create(new { title, detail, status = (int)statusCode }),
        };

    private static async Task<(int ExitCode, string StandardOutput, string StandardError)> RunCliProcessAsync(params string[] args)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = AppContext.BaseDirectory,
        };
        startInfo.ArgumentList.Add(typeof(ReadingLogApiClient).Assembly.Location);
        foreach (var argument in args)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the CLI process.");
        var standardOutputTask = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;
        return (process.ExitCode, standardOutput, standardError);
    }

    private sealed class DelayedStringContent : HttpContent
    {
        private readonly byte[] _payload;
        private readonly TimeSpan _delay;
        private readonly Action? _onDispose;

        public DelayedStringContent(string payload, string mediaType, TimeSpan delay, Action? onDispose = null)
        {
            _payload = Encoding.UTF8.GetBytes(payload);
            _delay = delay;
            _onDispose = onDispose;
            Headers.ContentType = new MediaTypeHeaderValue(mediaType)
            {
                CharSet = Encoding.UTF8.WebName,
            };
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return SerializeToStreamAsync(stream, context, CancellationToken.None);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken);
            }

            await stream.WriteAsync(_payload, cancellationToken);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _payload.Length;
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _onDispose?.Invoke();
            }

            base.Dispose(disposing);
        }
    }
}
