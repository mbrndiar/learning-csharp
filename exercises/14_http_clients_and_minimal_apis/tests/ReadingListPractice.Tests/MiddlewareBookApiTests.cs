// The low-level middleware app and the Minimal API app both compile a public
// partial `Program` in the global namespace. The extern alias below (wired in
// the .csproj as `Aliases="MiddlewareApp"`) disambiguates the two so this file
// can host WebApplicationFactory-driven tests against the middleware app
// without colliding with the Minimal API app's `Program` used elsewhere in
// this test project.
extern alias MiddlewareApp;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;
using LearningCSharp.Exercises.HttpClientsAndMinimalApis.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MiddlewareProgram = MiddlewareApp::Program;

namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Tests;

public sealed class MiddlewareBookApiTests
{
    [Fact]
    public async Task GetBooksReturnsSeededDataThroughTheMiddlewarePipeline()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WebApplicationFactory<MiddlewareProgram> factory = new();
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
        await using WebApplicationFactory<MiddlewareProgram> factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/books/{Guid.Empty:D}", cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBookReturnsNotFoundForMissingBook()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WebApplicationFactory<MiddlewareProgram> factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"/books/{Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"):D}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostBookRejectsInvalidContract()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WebApplicationFactory<MiddlewareProgram> factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/books", new CreateBookRequest("", "", 1200), cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Title", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostBookRejectsNonJsonContentType()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WebApplicationFactory<MiddlewareProgram> factory = new();
        HttpClient client = factory.CreateClient();

        using StringContent content = new("title=Deep Work", Encoding.UTF8, "application/x-www-form-urlencoded");
        HttpResponseMessage response = await client.PostAsync("/books", content, cancellationToken);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task PostBookRejectsOversizedBody()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WebApplicationFactory<MiddlewareProgram> factory = new();
        HttpClient client = factory.CreateClient();

        string oversizedTitle = new('a', 32 * 1024);
        var request = new CreateBookRequest(oversizedTitle, "Author", 2020);
        using StringContent content = new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync("/books", content, cancellationToken);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task PostBookCreatesAndSetsLocationHeader()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WebApplicationFactory<MiddlewareProgram> factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/books",
            new CreateBookRequest("Deep Work", "Cal Newport", 2016),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        BookDto? created = await response.Content.ReadFromJsonAsync<BookDto>(cancellationToken: cancellationToken);
        Assert.NotNull(created);
        Assert.Equal("Deep Work", created.Title);
    }

    [Fact]
    public async Task GetBooksRespectsConfiguredMaxResultsThroughTheMiddlewarePipeline()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WebApplicationFactory<MiddlewareProgram> factory = new();
        await using WebApplicationFactory<MiddlewareProgram> limitedFactory = factory.WithWebHostBuilder(builder =>
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

    // Note: the in-memory WebApplicationFactory/TestServer transport used above
    // does not faithfully emulate chunked transfer encoding (it always
    // surfaces a computed Content-Length), so it cannot exercise the
    // "no declared Content-Length" boundary case. That real-socket scenario -
    // where ReadBoundedBodyAsync's actual-bytes-read check is the only thing
    // enforcing the size boundary - is verified by the lesson's
    // MiddlewareBookApiSample smoke test, which runs Kestrel on a real
    // loopback port. See lessons/14_http_clients_and_minimal_apis/README.md.
}
