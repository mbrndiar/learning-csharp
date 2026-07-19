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

app.MapGet("/", () => Results.Ok(new { name = "ReadingLog.Api", status = "starter-ready" }));
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
    var book = await service.GetBookAsync(id, cancellationToken);
    return book is null
        ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Book not found.", detail: $"Book '{id}' was not found.")
        : Results.Ok(book.ToResponse());
});
app.MapGet("/entries", async (Guid? bookId, ReadingLogService service, CancellationToken cancellationToken) =>
{
    var entries = await service.ListEntriesAsync(bookId, cancellationToken);
    return Results.Ok(entries.Select(ApiContractMappings.ToResponse).ToArray());
});
app.MapPost("/books", () =>
{
    // TODO(m4): Map DTO validation and create-book flow in milestone 4.
    return Results.Problem(statusCode: StatusCodes.Status501NotImplemented, title: "TODO(m4)", detail: "Starter POST /books is intentionally left for milestone 4.");
});
app.MapPost("/entries", () =>
{
    // TODO(m4): Map DTO validation and create-entry flow in milestone 4.
    return Results.Problem(statusCode: StatusCodes.Status501NotImplemented, title: "TODO(m4)", detail: "Starter POST /entries is intentionally left for milestone 4.");
});

app.Run();

public partial class Program;
