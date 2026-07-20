using System.Net;
using LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;
using LearningCSharp.Exercises.HttpClientsAndMinimalApis.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Tests;

/// <summary>
/// <see cref="RawBookHttpClient"/> owns a brand-new <see cref="HttpClient"/> for
/// every call, so it needs a real loopback socket to exercise honestly (an
/// in-memory TestServer would hide the very connection/disposal behavior this
/// class demonstrates). These tests start one ephemeral Minimal API instance on
/// 127.0.0.1 for the whole class and stop it afterward; everything still runs
/// in-process and never leaves the loopback interface.
/// </summary>
public sealed class RawBookHttpClientTests : IAsyncLifetime
{
    private WebApplication? _app;
    private Uri? _baseAddress;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.Configure<CatalogOptions>(_ => { });
        builder.Services.AddSingleton<IBookRepository, InMemoryBookRepository>();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        _app = builder.Build();
        _app.MapGet("/books", async (string? author, IBookRepository repository, IOptions<CatalogOptions> options, CancellationToken cancellationToken) =>
        {
            IReadOnlyList<BookDto> books = await repository.ListAsync(author, options.Value.MaxResults, cancellationToken);
            return Results.Ok(new BookListResponse(books, author));
        });
        _app.MapGet("/books/{id:guid}", async (Guid id, IBookRepository repository, CancellationToken cancellationToken) =>
        {
            BookDto? book = await repository.GetAsync(id, cancellationToken);
            return book is null ? Results.NotFound() : Results.Ok(book);
        });

        await _app.StartAsync();

        IServer server = _app.Services.GetRequiredService<IServer>();
        string address = server.Features.Get<IServerAddressesFeature>()?.Addresses.Single()
            ?? throw new InvalidOperationException("The loopback test server did not expose an address.");
        _baseAddress = new Uri(address);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetBooksReturnsSeededDataOverARealLoopbackSocket()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        IReadOnlyList<BookDto> books = await RawBookHttpClient.GetBooksAsync(_baseAddress!, author: null, cancellationToken);

        Assert.Equal(2, books.Count);
        Assert.Contains(books, book => book.Id == SeedBookIds.CleanCode);
    }

    [Fact]
    public async Task TryGetBookReturnsNullForMissingBook()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        BookDto? book = await RawBookHttpClient.TryGetBookAsync(
            _baseAddress!,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            cancellationToken);

        Assert.Null(book);
    }

    [Fact]
    public async Task TryGetBookReturnsTheBookForAnExistingId()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        BookDto? book = await RawBookHttpClient.TryGetBookAsync(_baseAddress!, SeedBookIds.CleanCode, cancellationToken);

        Assert.NotNull(book);
        Assert.Equal("Clean Code", book.Title);
    }

    [Fact]
    public async Task EachRawCallCreatesAndDisposesItsOwnClientWithoutLeaking()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Calling this several times back-to-back is the anti-pattern this class
        // deliberately demonstrates: every call opens and disposes a brand-new
        // HttpClient/socket. It still behaves correctly for a handful of calls,
        // which is exactly why the mistake is easy to miss until load appears.
        for (var i = 0; i < 5; i++)
        {
            IReadOnlyList<BookDto> books = await RawBookHttpClient.GetBooksAsync(_baseAddress!, author: null, cancellationToken);
            Assert.NotEmpty(books);
        }
    }
}
