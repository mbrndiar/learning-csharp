using System.Net.Http.Json;
using System.Text.Json;
using ReadingLog.Core;

namespace ReadingLog.Cli;

public sealed class ReadingLogApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly HttpClient _httpClient;
    private readonly ReadingLogApiClientOptions _options;

    public ReadingLogApiClient(HttpClient httpClient, ReadingLogApiClientOptions? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? new ReadingLogApiClientOptions();
    }

    public Task<IReadOnlyList<BookResponse>> ListBooksAsync(CancellationToken cancellationToken) =>
        SendAsync<IReadOnlyList<BookResponse>>(HttpMethod.Get, "/books", body: null, cancellationToken);

    public Task<BookDetailsResponse> GetBookAsync(Guid bookId, CancellationToken cancellationToken) =>
        SendAsync<BookDetailsResponse>(HttpMethod.Get, $"/books/{bookId}", body: null, cancellationToken);

    public Task<BookResponse> AddBookAsync(CreateBookRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SendAsync<BookResponse>(HttpMethod.Post, "/books", request, cancellationToken);
    }

    public Task<ReadingEntryResponse> AddReadingEntryAsync(CreateReadingEntryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SendAsync<ReadingEntryResponse>(HttpMethod.Post, "/entries", request, cancellationToken);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(_options.RequestTimeout);

        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: SerializerOptions);
        }

        try
        {
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var detail = await ReadProblemMessageAsync(response, linkedCts.Token);
                throw new HttpRequestException(detail, null, response.StatusCode);
            }

            try
            {
                var payload = await response.Content.ReadFromJsonAsync<T>(SerializerOptions, linkedCts.Token);
                return payload ?? throw new InvalidDataException("The API returned an empty JSON payload.");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("The API returned malformed JSON.", exception);
            }
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested && linkedCts.IsCancellationRequested)
        {
            throw new TimeoutException($"The request timed out after {_options.RequestTimeout}.", exception);
        }
    }

    private static async Task<string> ReadProblemMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"The API returned HTTP {(int)response.StatusCode} ({response.StatusCode}).";
        }

        try
        {
            var problem = JsonSerializer.Deserialize<ProblemResponse>(content, SerializerOptions);
            if (problem?.Title is not null || problem?.Detail is not null)
            {
                return string.Join(" ", new[] { problem.Title, problem.Detail }.Where(static value => !string.IsNullOrWhiteSpace(value)));
            }
        }
        catch (JsonException)
        {
        }

        return content;
    }
}
