using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;

public sealed class ReadingListApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public TimeSpan Timeout => httpClient.Timeout;

    public async Task<IReadOnlyList<BookDto>> GetBooksAsync(string? author, CancellationToken cancellationToken = default)
    {
        string path = string.IsNullOrWhiteSpace(author)
            ? "/books"
            : $"/books?author={Uri.EscapeDataString(author)}";

        using HttpResponseMessage response = await httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        BookListResponse? body = await response.Content.ReadFromJsonAsync<BookListResponse>(SerializerOptions, cancellationToken);
        return body?.Books ?? throw new InvalidDataException("The API did not return a valid book list.");
    }

    public async Task<BookDto?> TryGetBookAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.GetAsync($"/books/{id:D}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BookDto>(SerializerOptions, cancellationToken)
            ?? throw new InvalidDataException("The API returned an empty book response.");
    }

    public async Task<BookDto> CreateBookAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync("/books", request, SerializerOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BookDto>(SerializerOptions, cancellationToken)
            ?? throw new InvalidDataException("The API returned an empty created-book response.");
    }
}
