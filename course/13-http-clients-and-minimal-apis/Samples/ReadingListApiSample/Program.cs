using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;

string[] hostArgs = args.Where(argument => !string.Equals(argument, "--smoke", StringComparison.Ordinal)).ToArray();
bool smokeMode = args.Any(argument => string.Equals(argument, "--smoke", StringComparison.Ordinal));

var builder = WebApplication.CreateBuilder(hostArgs);
if (smokeMode)
{
    builder.Logging.ClearProviders();
}

builder.Services.Configure<SampleCatalogOptions>(builder.Configuration.GetSection("Catalog"));
builder.Services.AddSingleton<SampleBookRepository>();

await using WebApplication app = builder.Build();

app.MapGet("/books", (string? author, SampleBookRepository repository, IOptions<SampleCatalogOptions> options) =>
{
    IReadOnlyList<SampleBook> books = repository.List(author, options.Value.MaxResults);
    return TypedResults.Ok(new SampleBookListResponse(books, author));
});

app.MapGet("/books/{id:guid}", (Guid id, SampleBookRepository repository) =>
{
    if (id == Guid.Empty)
    {
        return Results.BadRequest("Use a non-empty GUID.");
    }

    SampleBook? book = repository.Get(id);
    return book is null ? Results.NotFound() : Results.Ok(book);
});

app.MapPost("/books", (CreateSampleBookRequest request, SampleBookRepository repository) =>
{
    Dictionary<string, string[]> errors = Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    SampleBook created = repository.Add(request);
    return Results.Created($"/books/{created.Id:D}", created);
});

if (smokeMode)
{
    app.Urls.Add("http://127.0.0.1:0");
    await RunSmokeAsync(app);
    return;
}

app.Urls.Add("http://127.0.0.1:5074");
Console.WriteLine("ReadingListApiSample listening on http://127.0.0.1:5074");
await app.RunAsync();

static async Task RunSmokeAsync(WebApplication app)
{
    await app.StartAsync();

    try
    {
        string baseAddress = GetServerAddress(app);
        using HttpClient client = new()
        {
            BaseAddress = new Uri(baseAddress),
            Timeout = TimeSpan.FromSeconds(5),
        };

        SampleBookListResponse books = await GetBooksAsync(client, CancellationToken.None);
        SampleBook? seededBook = await TryGetBookAsync(
            client,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CancellationToken.None);

        if (seededBook is null)
        {
            throw new InvalidDataException("Smoke mode expected the seeded book to exist.");
        }

        SampleBook? missingBook = await TryGetBookAsync(
            client,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CancellationToken.None);

        using HttpResponseMessage invalidPost = await client.PostAsJsonAsync(
            "/books",
            new CreateSampleBookRequest("", "", 1200),
            cancellationToken: CancellationToken.None);

        if (books.Books.Count != 2
            || missingBook is not null
            || invalidPost.StatusCode != HttpStatusCode.BadRequest)
        {
            throw new InvalidDataException("Smoke mode observed unexpected HTTP behavior.");
        }

        Console.WriteLine(
            $"SMOKE TEST PASSED books={books.Books.Count} seeded={seededBook.Title} missing=404 invalidPost={(int)invalidPost.StatusCode}");
    }
    finally
    {
        await app.StopAsync();
    }
}

static string GetServerAddress(WebApplication app)
{
    IServer server = app.Services.GetRequiredService<IServer>();
    IServerAddressesFeature? addressesFeature = server.Features.Get<IServerAddressesFeature>();
    return addressesFeature?.Addresses.Single()
        ?? throw new InvalidOperationException("The sample server did not expose an address.");
}

static async Task<SampleBookListResponse> GetBooksAsync(HttpClient client, CancellationToken cancellationToken)
{
    using HttpResponseMessage response = await client.GetAsync("/books", cancellationToken);

    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<SampleBookListResponse>(
               SampleJsonOptions.Value,
               cancellationToken)
           ?? throw new InvalidDataException("The sample API returned an empty book list.");
}

static async Task<SampleBook?> TryGetBookAsync(HttpClient client, Guid id, CancellationToken cancellationToken)
{
    using HttpResponseMessage response = await client.GetAsync($"/books/{id:D}", cancellationToken);

    if (response.StatusCode == HttpStatusCode.NotFound)
    {
        return null;
    }

    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<SampleBook>(SampleJsonOptions.Value, cancellationToken)
           ?? throw new InvalidDataException("The sample API returned an empty book payload.");
}

static Dictionary<string, string[]> Validate(CreateSampleBookRequest request)
{
    Dictionary<string, string[]> errors = [];

    if (string.IsNullOrWhiteSpace(request.Title))
    {
        errors[nameof(request.Title)] = ["Title is required."];
    }

    if (string.IsNullOrWhiteSpace(request.Author))
    {
        errors[nameof(request.Author)] = ["Author is required."];
    }

    if (request.YearPublished is < 1450 or > 2100)
    {
        errors[nameof(request.YearPublished)] = ["YearPublished must be between 1450 and 2100."];
    }

    return errors;
}

internal static class SampleJsonOptions
{
    public static readonly JsonSerializerOptions Value = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };
}

internal sealed class SampleBookRepository
{
    private readonly List<SampleBook> _books =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Clean Code", "Robert C. Martin", 2008),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Refactoring", "Martin Fowler", 1999),
    ];

    public SampleBook Add(CreateSampleBookRequest request)
    {
        SampleBook created = new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            request.Title!.Trim(),
            request.Author!.Trim(),
            request.YearPublished);
        _books.Add(created);
        return created;
    }

    public SampleBook? Get(Guid id) => _books.SingleOrDefault(book => book.Id == id);

    public IReadOnlyList<SampleBook> List(string? author, int maxResults)
    {
        IEnumerable<SampleBook> query = _books;
        if (!string.IsNullOrWhiteSpace(author))
        {
            query = query.Where(book => string.Equals(book.Author, author, StringComparison.OrdinalIgnoreCase));
        }

        return query.Take(maxResults).ToArray();
    }
}

internal sealed record SampleCatalogOptions
{
    public int MaxResults { get; init; } = 20;
}

internal sealed record SampleBook(Guid Id, string Title, string Author, int YearPublished);

internal sealed record CreateSampleBookRequest(string? Title, string? Author, int YearPublished);

internal sealed record SampleBookListResponse(IReadOnlyList<SampleBook> Books, string? AuthorFilter);
