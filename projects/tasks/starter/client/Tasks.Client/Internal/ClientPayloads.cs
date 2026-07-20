using System.Net.Http.Headers;
using System.Text.Json;
using Tasks.Http;

namespace Tasks.Client;

internal static class ClientPayloads
{
    public static ByteArrayContent JsonContent(IReadOnlyDictionary<string, object?> body)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(body, TaskJson.Options);
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }

    public static IReadOnlyDictionary<string, string> CollectHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Content.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        return headers;
    }

    public static string Describe(Exception error, string fallback)
    {
        string message = error.Message.Trim();
        return message.Length > 0 ? message : fallback;
    }
}
