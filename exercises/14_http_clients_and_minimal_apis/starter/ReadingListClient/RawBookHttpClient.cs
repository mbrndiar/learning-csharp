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
    public static Task<IReadOnlyList<BookDto>> GetBooksAsync(Uri baseAddress, string? author, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        // TODO: Create a disposable HttpClient scoped to this call, issue GET
        // /books (optionally filtered by author), check the status first, and
        // deserialize the buffered JSON body.
        throw new NotImplementedException("TODO: Issue a raw GET /books and dispose the HttpClient afterward.");
    }

    public static Task<BookDto?> TryGetBookAsync(Uri baseAddress, Guid id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        // TODO: Create a disposable HttpClient scoped to this call. Return null
        // for 404, otherwise ensure success and deserialize the book.
        throw new NotImplementedException("TODO: Return null for 404 and a book for 200 using a raw, disposed HttpClient.");
    }
}
