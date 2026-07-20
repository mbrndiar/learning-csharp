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
        _ = request ?? throw new ArgumentNullException(nameof(request));
        _ = _httpClient;
        cancellationToken.ThrowIfCancellationRequested();

        // TODO(m5): Implement POST /books support in the starter CLI.
        throw new NotSupportedException("TODO(m5): AddBookAsync is intentionally left for milestone 5.");
    }

    public Task<ReadingEntryResponse> AddReadingEntryAsync(CreateReadingEntryRequest request, CancellationToken cancellationToken)
    {
        _ = request ?? throw new ArgumentNullException(nameof(request));
        _ = _httpClient;
        cancellationToken.ThrowIfCancellationRequested();

        // TODO(m5): Implement POST /entries support in the starter CLI.
        throw new NotSupportedException("TODO(m5): AddReadingEntryAsync is intentionally left for milestone 5.");
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

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested && linkedCts.IsCancellationRequested)
        {
            throw new TimeoutException($"The request timed out after {_options.RequestTimeout}.", exception);
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(detail, null, response.StatusCode);
        }

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken);
            return payload ?? throw new InvalidDataException("The API returned an empty JSON payload.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The API returned malformed JSON.", exception);
        }
    }
}
