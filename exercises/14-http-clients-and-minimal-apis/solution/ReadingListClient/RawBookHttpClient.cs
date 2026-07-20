using System.Net;
using System.Net.Http.Json;

namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;

/// <summary>
/// The raw <see cref="HttpClient"/> pattern: every call creates, uses, and disposes
/// its own client. This is the simplest way to reason about a single request, but
/// creating a new client per call risks socket exhaustion under load and cannot
/// share DNS/connection-pool state. Contrast with <see cref="ReadingListApiClient"/>,
/// which is resolved through <see cref="IHttpClientFactory"/> so DI owns the handler
/// lifetime and connection reuse instead.
/// </summary>
public static class RawBookHttpClient
{
    public static async Task<IReadOnlyList<BookDto>> GetBooksAsync(Uri baseAddress, string? author, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        using HttpClient client = new() { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(5) };
        string path = string.IsNullOrWhiteSpace(author)
            ? "/books"
            : $"/books?author={Uri.EscapeDataString(author)}";

        using HttpResponseMessage response = await client.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        BookListResponse? body = await response.Content.ReadFromJsonAsync<BookListResponse>(cancellationToken: cancellationToken);
        return body?.Books ?? throw new InvalidDataException("The API did not return a valid book list.");
    }

    public static async Task<BookDto?> TryGetBookAsync(Uri baseAddress, Guid id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        using HttpClient client = new() { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(5) };
        using HttpResponseMessage response = await client.GetAsync($"/books/{id:D}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BookDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidDataException("The API returned an empty book response.");
    }
}
