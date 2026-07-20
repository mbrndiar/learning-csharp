using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

// This sample builds the SAME tiny Books contract as ReadingListApiSample, but
// with none of the Minimal API routing helpers: no MapGet/MapPost, no
// automatic model binding, no automatic ProblemDetails. Every piece of
// dispatch, body handling, and response writing below is explicit so you can
// see exactly what Minimal APIs (and, one layer further down, MVC) do for you.
string[] hostArgs = args.Where(argument => !string.Equals(argument, "--smoke", StringComparison.Ordinal)).ToArray();
bool smokeMode = args.Any(argument => string.Equals(argument, "--smoke", StringComparison.Ordinal));

var builder = WebApplication.CreateBuilder(hostArgs);
if (smokeMode)
{
    builder.Logging.ClearProviders();
}

builder.Services.AddSingleton<SampleBookRepository>();

await using WebApplication app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() => BookMiddlewareLog.SampleStarted(app.Logger));
app.Lifetime.ApplicationStopping.Register(() => BookMiddlewareLog.SampleStopping(app.Logger));

// A single piece of middleware is the only place that touches raw exceptions.
// It logs full detail and always writes back a generic, sanitized failure body.
app.Use(async (HttpContext context, RequestDelegate next) =>
{
    try
    {
        await next(context);
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    {
        // The caller disconnected; there is no one left to answer.
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

// One terminal RequestDelegate replaces Minimal API route mapping entirely.
app.Run(context => BookRequestPipeline.HandleAsync(context, context.RequestServices.GetRequiredService<SampleBookRepository>()));

if (smokeMode)
{
    app.Urls.Add("http://127.0.0.1:0");
    await RunSmokeAsync(app);
    return;
}

app.Urls.Add("http://127.0.0.1:5175");
Console.WriteLine("MiddlewareBookApiSample listening on http://127.0.0.1:5175");
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

        using HttpResponseMessage list = await client.GetAsync("/books", CancellationToken.None);
        list.EnsureSuccessStatusCode();
        SampleBookListResponse? books = await list.Content.ReadFromJsonAsync<SampleBookListResponse>(SampleJsonOptions.Value, CancellationToken.None);

        using HttpResponseMessage missing = await client.GetAsync(
            $"/books/{Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"):D}",
            CancellationToken.None);

        using HttpResponseMessage badContentType = await client.PostAsync(
            "/books",
            new StringContent("not-json", System.Text.Encoding.UTF8, "text/plain"),
            CancellationToken.None);

        using HttpResponseMessage created = await client.PostAsJsonAsync(
            "/books",
            new CreateSampleBookRequest("Deep Work", "Cal Newport", 2016),
            cancellationToken: CancellationToken.None);

        if (books is null
            || books.Books.Count != 2
            || missing.StatusCode != HttpStatusCode.NotFound
            || badContentType.StatusCode != HttpStatusCode.UnsupportedMediaType
            || created.StatusCode != HttpStatusCode.Created
            || created.Headers.Location is null)
        {
            throw new InvalidDataException("Smoke mode observed unexpected middleware pipeline behavior.");
        }

        Console.WriteLine(
            $"SMOKE TEST PASSED books={books.Books.Count} missing=404 badContentType={(int)badContentType.StatusCode} created={(int)created.StatusCode}");
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

internal static class SampleJsonOptions
{
    public static readonly JsonSerializerOptions Value = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };
}

internal static partial class BookMiddlewareLog
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Information, Message = "Middleware sample started.")]
    public static partial void SampleStarted(ILogger logger);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Middleware sample stopping.")]
    public static partial void SampleStopping(ILogger logger);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Error, Message = "Unhandled failure while processing {Method} {Path}")]
    public static partial void UnhandledFailure(ILogger logger, Exception exception, string method, string path);
}

/// <summary>
/// Everything a Minimal API's route mapping would otherwise do for you,
/// written out explicitly: method/path routing, content-type and body-size
/// boundaries, manual JSON streaming, and manual status/header writes.
/// </summary>
internal static class BookRequestPipeline
{
    private const int MaxRequestBodyBytes = 8 * 1024;

    public static async Task HandleAsync(HttpContext context, SampleBookRepository repository)
    {
        CancellationToken cancellationToken = context.RequestAborted;
        string method = context.Request.Method;
        PathString path = context.Request.Path;

        if (HttpMethods.IsGet(method) && path == "/books")
        {
            await WriteJsonAsync(context, StatusCodes.Status200OK, new SampleBookListResponse(repository.List(), null), cancellationToken);
            return;
        }

        if (HttpMethods.IsGet(method) && path.StartsWithSegments("/books", out PathString remainder) && remainder.HasValue && remainder.Value!.Length > 1)
        {
            string idSegment = remainder.Value!.Trim('/');
            if (!Guid.TryParse(idSegment, out Guid id) || id == Guid.Empty)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            SampleBook? book = repository.Get(id);
            if (book is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await WriteJsonAsync(context, StatusCodes.Status200OK, book, cancellationToken);
            return;
        }

        if (HttpMethods.IsPost(method) && path == "/books")
        {
            // A declared Content-Length that already exceeds the limit lets us
            // reject before reading a single byte. But Content-Length can be
            // absent (chunked transfer encoding) or simply wrong, so the real
            // boundary is enforced below while reading, not just here.
            if (context.Request.ContentLength is > MaxRequestBodyBytes)
            {
                context.Response.StatusCode = StatusCodes.Status413RequestEntityTooLarge;
                return;
            }

            if (!context.Request.HasJsonContentType())
            {
                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                return;
            }

            byte[]? bodyBytes = await ReadBoundedBodyAsync(context.Request.Body, MaxRequestBodyBytes, cancellationToken);
            if (bodyBytes is null)
            {
                context.Response.StatusCode = StatusCodes.Status413RequestEntityTooLarge;
                return;
            }

            CreateSampleBookRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<CreateSampleBookRequest>(bodyBytes, SampleJsonOptions.Value);
            }
            catch (JsonException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (request is null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Author))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            SampleBook created = repository.Add(request);
            context.Response.Headers.Location = $"/books/{created.Id:D}";
            await WriteJsonAsync(context, StatusCodes.Status201Created, created, cancellationToken);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    private static async Task WriteJsonAsync<T>(HttpContext context, int statusCode, T payload, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(context.Response.Body, payload, SampleJsonOptions.Value, cancellationToken);
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

internal sealed class SampleBookRepository
{
    private readonly List<SampleBook> _books =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Clean Code", "Robert C. Martin", 2008),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Refactoring", "Martin Fowler", 1999),
    ];

    public SampleBook Add(CreateSampleBookRequest request)
    {
        SampleBook created = new(Guid.Parse("33333333-3333-3333-3333-333333333333"), request.Title!.Trim(), request.Author!.Trim(), request.YearPublished);
        _books.Add(created);
        return created;
    }

    public SampleBook? Get(Guid id) => _books.SingleOrDefault(book => book.Id == id);

    public IReadOnlyList<SampleBook> List() => _books;
}

internal sealed record SampleBook(Guid Id, string Title, string Author, int YearPublished);

internal sealed record CreateSampleBookRequest(string? Title, string? Author, int YearPublished);

internal sealed record SampleBookListResponse(IReadOnlyList<SampleBook> Books, string? AuthorFilter);
