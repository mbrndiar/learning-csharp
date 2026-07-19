using LearningCSharp.Course.Unit13.Practice.Api;
using LearningCSharp.Course.Unit13.Practice.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<CatalogOptions>(builder.Configuration.GetSection(CatalogOptions.SectionName));
builder.Services.AddSingleton<IBookRepository, InMemoryBookRepository>();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();

app.MapGet("/books", BookEndpoints.ListAsync);
app.MapGet("/books/{id:guid}", BookEndpoints.GetAsync);
app.MapPost("/books", BookEndpoints.CreateAsync);

app.Run();

public partial class Program;

internal static partial class BookEndpoints
{
    public static async Task<Ok<BookListResponse>> ListAsync(
        string? author,
        IBookRepository repository,
        IOptions<CatalogOptions> options,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        BookApiLog.ListingBooks(logger, author ?? "<any>");
        IReadOnlyList<BookDto> books = await repository.ListAsync(author, options.Value.MaxResults, cancellationToken);
        return TypedResults.Ok(new BookListResponse(books, author));
    }

    public static async Task<Results<BadRequest<string>, NotFound, Ok<BookDto>>> GetAsync(
        Guid id,
        IBookRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return TypedResults.BadRequest("Use a non-empty GUID.");
        }

        BookApiLog.FetchingBook(logger, id);
        BookDto? book = await repository.GetAsync(id, cancellationToken);
        return book is null ? TypedResults.NotFound() : TypedResults.Ok(book);
    }

    public static async Task<Results<ValidationProblem, Created<BookDto>>> CreateAsync(
        CreateBookRequest request,
        IBookRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors = Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        BookDto created = await repository.AddAsync(request, cancellationToken);
        BookApiLog.CreatedBook(logger, created.Id);
        return TypedResults.Created($"/books/{created.Id:D}", created);
    }

    private static Dictionary<string, string[]> Validate(CreateBookRequest request)
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
}

internal static partial class BookApiLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Listing books for author filter {AuthorFilter}")]
    public static partial void ListingBooks(ILogger logger, string authorFilter);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Fetching book {BookId}")]
    public static partial void FetchingBook(ILogger logger, Guid bookId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Created book {BookId}")]
    public static partial void CreatedBook(ILogger logger, Guid bookId);
}
