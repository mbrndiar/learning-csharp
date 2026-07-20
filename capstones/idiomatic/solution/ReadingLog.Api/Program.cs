using ReadingLog.Api;
using ReadingLog.Core;
using ReadingLog.Storage.Json;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.WriteIndented = true;
});
builder.Services.Configure<ReadingLogApiOptions>(builder.Configuration.GetSection("ReadingLog"));
builder.Services.AddSingleton<IReadingLogRepository>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ReadingLogApiOptions>>().Value;
    var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
    var storageDirectory = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.StorageDirectory));
    return new JsonReadingLogRepository(new JsonReadingLogRepositoryOptions
    {
        StorageDirectory = storageDirectory,
        FileName = options.StorageFileName,
    });
});
builder.Services.AddSingleton<ReadingLogService>();

var app = builder.Build();
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (DomainValidationException exception)
    {
        await Results.ValidationProblem(
            exception.Errors,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Validation failed.").ExecuteAsync(context);
    }
    catch (KeyNotFoundException exception)
    {
        await Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Book not found.",
            detail: exception.Message).ExecuteAsync(context);
    }
    catch (InvalidDataException exception)
    {
        ApiLogMessages.StoredDataMalformed(app.Logger, exception);
        await Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Stored data is malformed.",
            detail: "Fix or replace the JSON storage file before retrying.").ExecuteAsync(context);
    }
});

app.MapGet("/", () => Results.Ok(new { name = "ReadingLog.Api", status = "ok" }));
app.MapGet("/overview", async (ReadingLogService service, CancellationToken cancellationToken) =>
{
    var overview = await service.GetOverviewAsync(cancellationToken);
    return Results.Ok(overview.ToResponse());
});
app.MapGet("/books", async (ReadingLogService service, CancellationToken cancellationToken) =>
{
    var books = await service.ListBooksAsync(cancellationToken);
    return Results.Ok(books.Select(ApiContractMappings.ToResponse).ToArray());
});
app.MapGet("/books/{id:guid}", async (Guid id, ReadingLogService service, CancellationToken cancellationToken) =>
{
    var details = await service.GetBookAsync(id, cancellationToken);
    return details is null
        ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Book not found.", detail: $"Book '{id}' was not found.")
        : Results.Ok(details.ToResponse());
});
app.MapGet("/entries", async (Guid? bookId, ReadingLogService service, CancellationToken cancellationToken) =>
{
    var entries = await service.ListEntriesAsync(bookId, cancellationToken);
    return Results.Ok(entries.Select(ApiContractMappings.ToResponse).ToArray());
});
app.MapPost("/books", async (CreateBookRequestDto request, ReadingLogService service, CancellationToken cancellationToken) =>
{
    var book = await service.AddBookAsync(request.ToCommand(), cancellationToken);
    ApiLogMessages.AddedBook(app.Logger, book.Id);
    return Results.Created($"/books/{book.Id}", book.ToResponse());
});
app.MapPost("/entries", async (CreateReadingEntryRequestDto request, ReadingLogService service, CancellationToken cancellationToken) =>
{
    var entry = await service.AddReadingEntryAsync(request.ToCommand(), cancellationToken);
    ApiLogMessages.AddedReadingEntry(app.Logger, entry.Id, entry.BookId);
    return Results.Created($"/entries/{entry.Id}", entry.ToResponse());
});

app.Run();

public partial class Program;
