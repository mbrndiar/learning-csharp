using System.Buffers;
using Tasks.Core;
using Tasks.Http;

namespace Tasks.Server.MinimalApi;

/// <summary>
/// Response helpers shared by the Minimal API endpoints, its exception
/// middleware, and its status-code pages. They keep the wire contract identical
/// to the low-level server while letting this adapter use framework routing.
/// </summary>
internal static partial class ApiResponses
{
    /// <summary>Read the request body up to the shared size bound.</summary>
    public static async Task<byte[]> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength is long declared && declared > TaskHttpContract.MaxRequestBodyBytes)
        {
            throw ApiErrorException.PayloadTooLarge();
        }

        using var buffer = new MemoryStream();
        byte[] rented = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            int read;
            while ((read = await request.Body.ReadAsync(rented, cancellationToken).ConfigureAwait(false)) > 0)
            {
                if (buffer.Length + read > TaskHttpContract.MaxRequestBodyBytes)
                {
                    throw ApiErrorException.PayloadTooLarge();
                }

                buffer.Write(rented, 0, read);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }

        return buffer.ToArray();
    }

    /// <summary>Translate an endpoint exception into the shared error envelope.</summary>
    public static async Task WriteErrorAsync(HttpContext context, Exception exception, ILogger logger)
    {
        MappedError mapped = TaskHttpContract.Describe(exception);
        if (mapped.StatusCode == StatusCodes.Status500InternalServerError)
        {
            ApiLog.RequestFailed(logger, exception);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        await WriteEnvelopeAsync(context, mapped).ConfigureAwait(false);
    }

    /// <summary>Write an envelope for a routing-produced 404 or 405 status.</summary>
    public static async Task WriteStatusEnvelopeAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        int status = context.Response.StatusCode;
        MappedError mapped = status switch
        {
            StatusCodes.Status405MethodNotAllowed => TaskHttpContract.Describe(new ApiErrorException(
                StatusCodes.Status405MethodNotAllowed,
                ErrorCodes.MethodNotAllowed,
                "method is not allowed for this path",
                allow: context.Response.Headers.Allow.ToString())),
            StatusCodes.Status404NotFound => TaskHttpContract.Describe(ApiErrorException.RouteNotFound()),
            _ => TaskHttpContract.Describe(new InvalidOperationException("unexpected framework status")),
        };

        await WriteEnvelopeAsync(context, mapped).ConfigureAwait(false);
    }

    private static async Task WriteEnvelopeAsync(HttpContext context, MappedError mapped)
    {
        byte[] body = TaskHttpContract.SerializeError(mapped.Body);
        HttpResponse response = context.Response;
        response.StatusCode = mapped.StatusCode;
        response.ContentType = "application/json";
        if (mapped.Allow is { Length: > 0 } allow)
        {
            response.Headers.Allow = allow;
        }

        response.ContentLength = body.Length;
        await response.Body.WriteAsync(body, context.RequestAborted).ConfigureAwait(false);
    }
}
