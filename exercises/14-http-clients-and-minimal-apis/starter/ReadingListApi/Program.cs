using LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;
using LearningCSharp.Exercises.HttpClientsAndMinimalApis.Core;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<CatalogOptions>(builder.Configuration.GetSection(CatalogOptions.SectionName));
builder.Services.AddSingleton<IBookRepository, InMemoryBookRepository>();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();

app.MapGet("/books", async (string? author, IBookRepository repository, IOptions<CatalogOptions> options, CancellationToken cancellationToken) =>
{
    IReadOnlyList<BookDto> books = await repository.ListAsync(author, options.Value.MaxResults, cancellationToken);
    return TypedResults.Ok(new BookListResponse(books, author));
});

// TODO: Replace this placeholder so the handler validates the id (empty GUID -> 400 with a message),
// returns 404 when no book matches, and 200 with the book otherwise.
app.MapGet("/books/{id:guid}", (Guid id) => Results.NotFound());
// TODO: Replace this placeholder so the handler validates the request body and responds
// 201 with the created book (and its location), or 400 with validation details.
app.MapPost("/books", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

app.Run();

public partial class Program;
