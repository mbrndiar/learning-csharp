using ReadingLog.Tests.TestInfrastructure;

namespace ReadingLog.Tests.Api;

public sealed class ReadingLogApiTests
{
    [Fact]
    public async Task GetRootReturnsStatusPayload()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new ReadingLogApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("\"status\": \"ok\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetBooksReturnsEmptyArrayInitially()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new ReadingLogApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/books", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Equal("[]", body.Trim());
    }

    [Fact]
    public async Task PostBooksReturnsCreatedContract()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new ReadingLogApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/books", new CreateBookRequestDto("Dune", "Frank Herbert", 1965, "9780441172719"), cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ReadingLog.Api.BookResponse>(cancellationToken);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Dune", created.Title);
        Assert.Equal($"/books/{created.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task GetOverviewReturnsCountsAfterCreatingBookAndEntry()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new ReadingLogApiFactory();
        using var client = factory.CreateClient();
        var createdBook = await CreateBookAsync(client, cancellationToken);
        using var createEntryResponse = await client.PostAsJsonAsync(
            "/entries",
            new CreateReadingEntryRequestDto(createdBook.Id, new DateOnly(2026, 7, 19), new DateOnly(2026, 7, 19), 45, 5, "Great pacing"),
            cancellationToken);
        createEntryResponse.EnsureSuccessStatusCode();

        var overview = await client.GetFromJsonAsync<ReadingOverviewResponse>("/overview", cancellationToken);

        Assert.NotNull(overview);
        Assert.Equal(1, overview.BookCount);
        Assert.Equal(1, overview.EntryCount);
        Assert.Equal(45, overview.TotalPagesRead);
        Assert.Equal(1, overview.FinishedBookCount);
        Assert.Equal(createdBook.Id, overview.MostRecentBook?.Id);
    }

    [Fact]
    public async Task PostBooksReturnsValidationProblemForBlankTitle()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new ReadingLogApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/books", new CreateBookRequestDto("   ", "Frank Herbert", 1965, null), cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("Validation failed.", body, StringComparison.Ordinal);
        Assert.Contains("title", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostEntriesReturnsCreatedContractForExistingBook()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new ReadingLogApiFactory();
        using var client = factory.CreateClient();
        var createdBook = await CreateBookAsync(client, cancellationToken);
        using var response = await client.PostAsJsonAsync(
            "/entries",
            new CreateReadingEntryRequestDto(createdBook.Id, new DateOnly(2026, 7, 20), null, 30, 4, "Strong follow-up"),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdEntry = await response.Content.ReadFromJsonAsync<ReadingLog.Api.ReadingEntryResponse>(cancellationToken);
        Assert.NotNull(createdEntry);
        Assert.Equal(createdBook.Id, createdEntry.BookId);
        Assert.Equal("/entries/" + createdEntry.Id, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task PostEntriesReturnsValidationProblemForInvalidPayload()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new ReadingLogApiFactory();
        using var client = factory.CreateClient();
        var createdBook = await CreateBookAsync(client, cancellationToken);
        using var response = await client.PostAsJsonAsync(
            "/entries",
            new CreateReadingEntryRequestDto(createdBook.Id, new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 19), 0, 9, " "),
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("Validation failed.", body, StringComparison.Ordinal);
        Assert.Contains("pagesRead", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetBookReturnsNotFoundProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new ReadingLogApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync($"/books/{Guid.NewGuid()}", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("Book not found.", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetEntriesCanFilterByBookId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new ReadingLogApiFactory();
        using var client = factory.CreateClient();
        var dune = await CreateBookAsync(client, cancellationToken);
        var hyperion = await CreateBookAsync(client, cancellationToken, "Hyperion", "Dan Simmons", 1989);
        await client.PostAsJsonAsync(
            "/entries",
            new CreateReadingEntryRequestDto(dune.Id, new DateOnly(2026, 7, 20), null, 10, null, null),
            cancellationToken);
        await client.PostAsJsonAsync(
            "/entries",
            new CreateReadingEntryRequestDto(hyperion.Id, new DateOnly(2026, 7, 21), null, 12, null, null),
            cancellationToken);

        var entries = await client.GetFromJsonAsync<IReadOnlyList<ReadingLog.Api.ReadingEntryResponse>>($"/entries?bookId={dune.Id}", cancellationToken);

        Assert.NotNull(entries);
        Assert.Single(entries);
        Assert.Equal(dune.Id, entries[0].BookId);
    }

    [Fact]
    public async Task PostEntriesReturnsNotFoundProblemForUnknownBook()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new ReadingLogApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/entries",
            new CreateReadingEntryRequestDto(Guid.NewGuid(), new DateOnly(2026, 7, 1), null, 10, 4, "Notes"),
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("Book not found.", body, StringComparison.Ordinal);
    }

    private static async Task<ReadingLog.Api.BookResponse> CreateBookAsync(
        HttpClient client,
        CancellationToken cancellationToken,
        string title = "Dune",
        string author = "Frank Herbert",
        int? publicationYear = 1965)
    {
        using var response = await client.PostAsJsonAsync("/books", new CreateBookRequestDto(title, author, publicationYear, null), cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ReadingLog.Api.BookResponse>(cancellationToken);
        return created ?? throw new InvalidOperationException("Expected a created book payload.");
    }
}
