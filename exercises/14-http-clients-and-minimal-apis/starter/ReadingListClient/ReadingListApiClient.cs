namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;

public sealed class ReadingListApiClient(HttpClient httpClient)
{
    public TimeSpan Timeout => httpClient.Timeout;

    public Task<IReadOnlyList<BookDto>> GetBooksAsync(string? author, CancellationToken cancellationToken = default)
    {
        // TODO: Issue GET /books (include the author filter only when one is provided).
        // TODO: Check the status before reading, dispose the response, and treat a missing body as invalid data.
        throw new NotImplementedException("TODO: Issue GET /books, check the status first, and deserialize the buffered JSON body.");
    }

    public Task<BookDto?> TryGetBookAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // TODO: Issue GET /books/{id}; return null on 404 and a book on success.
        // TODO: Ensure success for other statuses, dispose the response, and treat a missing body as invalid data.
        throw new NotImplementedException("TODO: Return null for 404 and a book for 200.");
    }

    public Task<BookDto> CreateBookAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: Reject a null request, then POST it as JSON.
        // TODO: Ensure success, dispose the response, and treat a missing body as invalid data.
        throw new NotImplementedException("TODO: POST JSON and deserialize the created book.");
    }
}
