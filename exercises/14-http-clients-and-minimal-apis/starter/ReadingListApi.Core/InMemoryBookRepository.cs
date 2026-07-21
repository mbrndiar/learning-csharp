using LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;

namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Core;

// TODO: Complete this repository so it starts seeded with exactly two books
// matching SeedBookIds.CleanCode ("Clean Code") and SeedBookIds.Refactoring
// ("Refactoring"), and stays safe to call concurrently from multiple
// callers listing, getting, and adding books at the same time.
public sealed class InMemoryBookRepository : IBookRepository
{
    public Task<BookDto> AddAsync(CreateBookRequest request, CancellationToken cancellationToken)
    {
        // TODO: Complete AddAsync so it stores a new book with a freshly
        // generated id, makes it visible to subsequent list/get calls, and
        // returns the stored book.
        return Task.FromResult(new BookDto(Guid.Empty, request.Title ?? string.Empty, request.Author ?? string.Empty, request.YearPublished));
    }

    public Task<BookDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        // TODO: Complete GetAsync so it returns the matching seeded/added book,
        // or null when no book has that id.
        return Task.FromResult<BookDto?>(null);
    }

    public Task<IReadOnlyList<BookDto>> ListAsync(string? author, int maxResults, CancellationToken cancellationToken)
    {
        // TODO: Complete ListAsync so it returns the seeded/added books, filtered
        // by author (case-insensitive) when one is provided, and never more than
        // maxResults books.
        return Task.FromResult<IReadOnlyList<BookDto>>(Array.Empty<BookDto>());
    }
}
