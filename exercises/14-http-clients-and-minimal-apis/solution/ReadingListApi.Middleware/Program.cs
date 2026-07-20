using System.Text.Json;
using LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;
using LearningCSharp.Exercises.HttpClientsAndMinimalApis.Core;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<CatalogOptions>(builder.Configuration.GetSection(CatalogOptions.SectionName));
builder.Services.AddSingleton<IBookRepository, InMemoryBookRepository>();

WebApplication app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() => BookMiddlewareLog.PipelineStarted(app.Logger));
app.Lifetime.ApplicationStopping.Register(() => BookMiddlewareLog.PipelineStopping(app.Logger));

// This middleware is the only place that touches raw exceptions. Every response
// body it writes is sanitized: full exception details go to the log, never to
// the client. This is what `UseExceptionHandler` + `AddProblemDetails` do for
// you automatically in the Minimal API sample.
app.Use(async (HttpContext context, RequestDelegate next) =>
{
    try
    {
        await next(context);
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    {
        // The client disconnected or its request timed out; there is no one left to answer.
    }
    catch (Exception exception)
    {
        BookMiddlewareLog.UnhandledFailure(app.Logger, exception, context.Request.Method, context.Request.Path);
        if (!context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json; charset=utf-8";
            await context.Response.WriteAsync("""{"title":"An unexpected error occurred."}""", context.RequestAborted);
        }
    }
});

// A single terminal RequestDelegate replaces Minimal API route mapping. Every
// piece of dispatch, DI resolution, and serialization below is explicit.
app.Run(BookRequestPipeline.HandleAsync);

app.Run();

public partial class Program;

internal static partial class BookMiddlewareLog
{
    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Middleware pipeline started.")]
    public static partial void PipelineStarted(ILogger logger);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Middleware pipeline stopping.")]
    public static partial void PipelineStopping(ILogger logger);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Error, Message = "Unhandled failure while processing {Method} {Path}")]
    public static partial void UnhandledFailure(ILogger logger, Exception exception, string method, string path);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Listing books for author filter {AuthorFilter}")]
    public static partial void ListingBooks(ILogger logger, string authorFilter);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Fetching book {BookId}")]
    public static partial void FetchingBook(ILogger logger, Guid bookId);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Created book {BookId}")]
    public static partial void CreatedBook(ILogger logger, Guid bookId);
}

internal static class BookRequestPipeline
{
    private const int MaxRequestBodyBytes = 16 * 1024;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task HandleAsync(HttpContext context)
    {
        CancellationToken cancellationToken = context.RequestAborted;
        IBookRepository repository = context.RequestServices.GetRequiredService<IBookRepository>();
        CatalogOptions options = context.RequestServices.GetRequiredService<IOptions<CatalogOptions>>().Value;
        ILogger logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("BookRequestPipeline");

        string method = context.Request.Method;
        PathString path = context.Request.Path;

        if (HttpMethods.IsGet(method) && path == "/books")
        {
            await ListBooksAsync(context, repository, options, logger, cancellationToken);
            return;
        }

        if (HttpMethods.IsGet(method) && path.StartsWithSegments("/books", out PathString remainder) && remainder.HasValue && remainder.Value!.Length > 1)
        {
            await GetBookAsync(context, repository, remainder.Value!.Trim('/'), logger, cancellationToken);
            return;
        }

        if (HttpMethods.IsPost(method) && path == "/books")
        {
            await CreateBookAsync(context, repository, logger, cancellationToken);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    private static async Task ListBooksAsync(
        HttpContext context,
        IBookRepository repository,
        CatalogOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        string? author = context.Request.Query["author"].FirstOrDefault();
        BookMiddlewareLog.ListingBooks(logger, author ?? "<any>");
        IReadOnlyList<BookDto> books = await repository.ListAsync(author, options.MaxResults, cancellationToken);
        await WriteJsonAsync(context, StatusCodes.Status200OK, new BookListResponse(books, author), cancellationToken);
    }

    private static async Task GetBookAsync(
        HttpContext context,
        IBookRepository repository,
        string idSegment,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(idSegment, out Guid id) || id == Guid.Empty)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Use a non-empty GUID.", cancellationToken);
            return;
        }

        BookMiddlewareLog.FetchingBook(logger, id);
        BookDto? book = await repository.GetAsync(id, cancellationToken);
        if (book is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await WriteJsonAsync(context, StatusCodes.Status200OK, book, cancellationToken);
    }

    private static async Task CreateBookAsync(
        HttpContext context,
        IBookRepository repository,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // A declared Content-Length that already exceeds the limit lets us
        // reject before reading a single byte. But Content-Length can be
        // absent (chunked transfer encoding) or simply wrong, so the real
        // boundary is enforced by ReadBoundedBodyAsync while reading, not
        // just here.
        if (context.Request.ContentLength is > MaxRequestBodyBytes)
        {
            await WriteProblemAsync(context, StatusCodes.Status413RequestEntityTooLarge, "Request body missing or too large.", cancellationToken);
            return;
        }

        if (!context.Request.HasJsonContentType())
        {
            await WriteProblemAsync(context, StatusCodes.Status415UnsupportedMediaType, "Content-Type must be application/json.", cancellationToken);
            return;
        }

        byte[]? bodyBytes = await ReadBoundedBodyAsync(context.Request.Body, MaxRequestBodyBytes, cancellationToken);
        if (bodyBytes is null)
        {
            await WriteProblemAsync(context, StatusCodes.Status413RequestEntityTooLarge, "Request body missing or too large.", cancellationToken);
            return;
        }

        CreateBookRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<CreateBookRequest>(bodyBytes, SerializerOptions);
        }
        catch (JsonException)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Request body is not valid JSON.", cancellationToken);
            return;
        }

        if (request is null)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Request body must not be empty.", cancellationToken);
            return;
        }

        Dictionary<string, string[]> errors = Validate(request);
        if (errors.Count > 0)
        {
            await WriteJsonAsync(context, StatusCodes.Status400BadRequest, new { errors }, cancellationToken);
            return;
        }

        BookDto created = await repository.AddAsync(request, cancellationToken);
        BookMiddlewareLog.CreatedBook(logger, created.Id);
        context.Response.Headers.Location = $"/books/{created.Id:D}";
        await WriteJsonAsync(context, StatusCodes.Status201Created, created, cancellationToken);
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

    private static async Task WriteJsonAsync<T>(HttpContext context, int statusCode, T payload, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(context.Response.Body, payload, SerializerOptions, cancellationToken);
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string detail, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json; charset=utf-8";
        await JsonSerializer.SerializeAsync(context.Response.Body, new { title = detail }, SerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Reads at most <paramref name="maxBytes"/> + 1 bytes so the boundary is
    /// enforced against bytes actually received, not just a declared
    /// Content-Length (which chunked requests omit and any client could lie
    /// about).
    /// </summary>
    private static async Task<byte[]?> ReadBoundedBodyAsync(Stream body, int maxBytes, CancellationToken cancellationToken)
    {
        using MemoryStream buffer = new();
        byte[] chunk = new byte[8192];
        int totalRead = 0;
        int bytesRead;

        while ((bytesRead = await body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            totalRead += bytesRead;
            if (totalRead > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, bytesRead);
        }

        return buffer.ToArray();
    }
}
