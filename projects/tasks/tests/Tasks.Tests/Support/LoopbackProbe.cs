using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Tasks.Tests.Support;

/// <summary>One captured loopback response with case-insensitive header lookup.</summary>
public sealed class ProbeResponse
{
    /// <summary>Create a captured response.</summary>
    public ProbeResponse(int status, IReadOnlyDictionary<string, string> headers, byte[] body)
    {
        Status = status;
        Headers = headers;
        Body = body;
    }

    /// <summary>The HTTP status code.</summary>
    public int Status { get; }

    /// <summary>Response headers keyed case-insensitively.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>The response body bytes.</summary>
    public byte[] Body { get; }

    /// <summary>Read one header value, or null when absent.</summary>
    public string? Header(string name) => Headers.TryGetValue(name, out string? value) ? value : null;

    /// <summary>Parse the body as a detached JSON element.</summary>
    public JsonElement Json()
    {
        using var document = JsonDocument.Parse(Body);
        return document.RootElement.Clone();
    }
}

/// <summary>
/// A finite-timeout loopback HTTP client used to exercise servers as a black
/// box, independent of the project's own client transports.
/// </summary>
public static class LoopbackProbe
{
    /// <summary>Send one request, capturing status, headers, and body bytes.</summary>
    public static async Task<ProbeResponse> SendAsync(
        string baseUrl,
        string method,
        string path,
        string? contentType = null,
        byte[]? body = null,
        CancellationToken cancellationToken = default)
    {
        using var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
        };
        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5),
        };
        using var message = new HttpRequestMessage(new HttpMethod(method), baseUrl + path);
        if (body is not null)
        {
            var content = new ByteArrayContent(body);
            if (contentType is not null)
            {
                content.Headers.TryAddWithoutValidation("Content-Type", contentType);
            }

            message.Content = content;
        }

        using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);
        byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Content.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        return new ProbeResponse((int)response.StatusCode, headers, bytes);
    }

    /// <summary>Send a UTF-8 JSON request body with a JSON content type.</summary>
    public static Task<ProbeResponse> SendJsonAsync(
        string baseUrl,
        string method,
        string path,
        string json,
        CancellationToken cancellationToken = default)
        => SendAsync(baseUrl, method, path, "application/json", Encoding.UTF8.GetBytes(json), cancellationToken);
}
