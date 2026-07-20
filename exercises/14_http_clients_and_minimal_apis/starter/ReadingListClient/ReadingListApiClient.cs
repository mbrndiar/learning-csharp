namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;

public sealed class ReadingListApiClient(HttpClient httpClient)
{
    public TimeSpan Timeout => httpClient.Timeout;

    public Task<IReadOnlyList<BookDto>> GetBooksAsync(string? author, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TODO: Issue GET /books, check the status first, and deserialize the buffered JSON body.");
    }

    public Task<BookDto?> TryGetBookAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TODO: Return null for 404 and a book for 200.");
    }

    public Task<BookDto> CreateBookAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TODO: POST JSON and deserialize the created book.");
    }
}
