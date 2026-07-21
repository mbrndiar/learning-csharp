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
}

internal static class BookRequestPipeline
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static Task HandleAsync(HttpContext context)
    {
        // TODO: Replace this placeholder so it dispatches on context.Request.Method
        // and context.Request.Path to implement:
        //   GET  /books          -> 200 with the seeded/added books as JSON
        //   GET  /books/{id}     -> 400 for Guid.Empty, 404 for missing, 200 otherwise
        //   POST /books          -> 415 for non-JSON, 413 above 16 KiB, 400 for
        //                           invalid JSON/contract data, or 201 with Location
        // Every branch must read/write through the HttpContext directly: no
        // Minimal API routing helpers, and request cancellation must be honored.
        context.Response.StatusCode = StatusCodes.Status501NotImplemented;
        return Task.CompletedTask;
    }
}
