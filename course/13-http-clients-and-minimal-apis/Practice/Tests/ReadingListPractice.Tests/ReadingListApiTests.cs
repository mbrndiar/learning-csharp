using System.Net;
using System.Net.Http.Json;
using System.Text;
using LearningCSharp.Course.Unit13.Practice.Api;
using LearningCSharp.Course.Unit13.Practice.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LearningCSharp.Course.Unit13.Practice.Tests;

public sealed class ReadingListApiTests
{
    [Fact]
    public async Task GetBooksReturnsSeededData()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WebApplicationFactory<Program> factory = new();
        HttpClient client = factory.CreateClient();

        BookListResponse? response = await client.GetFromJsonAsync<BookListResponse>("/books", cancellationToken);

        Assert.NotNull(response);
        Assert.Equal(2, response.Books.Count);
        Assert.Contains(response.Books, book => book.Id == SeedBookIds.CleanCode);
    }

    [Fact]
    public async Task GetBookReturnsBadRequestForEmptyGuid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WebApplicationFactory<Program> factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/books/{Guid.Empty:D}", cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostBookRejectsInvalidContract()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WebApplicationFactory<Program> factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/books", new CreateBookRequest("", "", 1200), cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Title", body, StringComparison.Ordinal);
        Assert.Contains("Author", body, StringComparison.Ordinal);
        Assert.Contains("YearPublished", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TypedClientRoundTripsCreatedBookAndUsesFiniteTimeout()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WebApplicationFactory<Program> factory = new();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddReadingListApiClient(
            client => client.BaseAddress = new Uri("http://localhost"),
            primaryHandlerFactory: () => factory.Server.CreateHandler());

        await using ServiceProvider provider = services.BuildServiceProvider();
        ReadingListApiClient apiClient = provider.GetRequiredService<ReadingListApiClient>();

        BookDto created = await apiClient.CreateBookAsync(new CreateBookRequest("Deep Work", "Cal Newport", 2016), cancellationToken);
        BookDto? loaded = await apiClient.TryGetBookAsync(created.Id, cancellationToken);

        Assert.Equal(TimeSpan.FromSeconds(5), apiClient.Timeout);
        Assert.Equal(created, loaded);
    }

    [Fact]
    public async Task GetBooksRespectsConfiguredMaxResults()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WebApplicationFactory<Program> factory = new();
        await using WebApplicationFactory<Program> limitedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Catalog:MaxResults"] = "1",
                })));

        HttpClient client = limitedFactory.CreateClient();
        BookListResponse? response = await client.GetFromJsonAsync<BookListResponse>("/books", cancellationToken);

        Assert.NotNull(response);
        Assert.Single(response.Books);
    }

    [Fact]
    public async Task TypedClientReturnsNullForMissingBook()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WebApplicationFactory<Program> factory = new();
        var apiClient = new ReadingListApiClient(factory.CreateClient());

        BookDto? book = await apiClient.TryGetBookAsync(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), cancellationToken);

        Assert.Null(book);
    }

    [Fact]
    public async Task TypedClientTimeoutAlsoCoversSlowResponseBody()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        byte[] jsonBytes = Encoding.UTF8.GetBytes("""
            {"books":[{"id":"11111111-1111-1111-1111-111111111111","title":"Clean Code","author":"Robert C. Martin","yearPublished":2008}],"authorFilter":null}
            """);

        using HttpClient httpClient = new(new SlowBodyHandler(jsonBytes, TimeSpan.FromMilliseconds(300)))
        {
            BaseAddress = new Uri("http://localhost"),
            Timeout = TimeSpan.FromMilliseconds(100),
        };

        var apiClient = new ReadingListApiClient(httpClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => apiClient.GetBooksAsync(null, cancellationToken));
    }

    private sealed class SlowBodyHandler(byte[] jsonBytes, TimeSpan delay) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new DelayedReadStream(jsonBytes, delay)),
            };
            response.Content.Headers.ContentType = new("application/json");
            return Task.FromResult(response);
        }
    }

    private sealed class DelayedReadStream(byte[] bytes, TimeSpan delay) : Stream
    {
        private readonly byte[] _bytes = bytes;
        private readonly TimeSpan _delay = delay;
        private int _position;
        private bool _delayed;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _bytes.Length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            DelayIfNeeded(CancellationToken.None).GetAwaiter().GetResult();
            return CopyNextChunk(buffer.AsSpan(offset, count));
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await DelayIfNeeded(cancellationToken);
            return CopyNextChunk(buffer.Span);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        private async Task DelayIfNeeded(CancellationToken cancellationToken)
        {
            if (_delayed)
            {
                return;
            }

            _delayed = true;
            await Task.Delay(_delay, cancellationToken);
        }

        private int CopyNextChunk(Span<byte> destination)
        {
            if (_position >= _bytes.Length)
            {
                return 0;
            }

            int bytesToCopy = Math.Min(destination.Length, _bytes.Length - _position);
            _bytes.AsSpan(_position, bytesToCopy).CopyTo(destination);
            _position += bytesToCopy;
            return bytesToCopy;
        }
    }
}
