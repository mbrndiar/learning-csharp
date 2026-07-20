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

app.MapGet("/books/{id:guid}", (Guid id) => Results.NotFound());
app.MapPost("/books", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

app.Run();

public partial class Program;
