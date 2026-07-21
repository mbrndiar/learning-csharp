namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;

public sealed class ReadingListApiClient(HttpClient httpClient)
{
    public TimeSpan Timeout => httpClient.Timeout;

    public Task<IReadOnlyList<BookDto>> GetBooksAsync(string? author, CancellationToken cancellationToken = default)
    {
        // TODO: Implement GetBooksAsync so it issues GET /books (including the
        // author filter only when one is provided), checks the status before
        // reading, disposes the response, and treats a missing body as invalid
        // data.
        throw new NotImplementedException("TODO: Issue GET /books, check the status first, and deserialize the buffered JSON body.");
    }

    public Task<BookDto?> TryGetBookAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // TODO: Implement TryGetBookAsync so it issues GET /books/{id}, returns
        // null on 404 and a book on success, ensures success for other statuses,
        // disposes the response, and treats a missing body as invalid data.
        throw new NotImplementedException("TODO: Return null for 404 and a book for 200.");
    }

    public Task<BookDto> CreateBookAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: Implement CreateBookAsync so it rejects a null request, POSTs it
        // as JSON, ensures success, disposes the response, and treats a missing
        // body as invalid data.
        throw new NotImplementedException("TODO: POST JSON and deserialize the created book.");
    }
}
