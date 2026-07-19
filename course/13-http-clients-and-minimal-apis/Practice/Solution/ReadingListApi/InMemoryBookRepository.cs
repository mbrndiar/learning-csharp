using LearningCSharp.Course.Unit13.Practice.Client;

namespace LearningCSharp.Course.Unit13.Practice.Api;

public sealed class InMemoryBookRepository : IBookRepository
{
    private readonly List<BookDto> _books =
    [
        new(SeedBookIds.CleanCode, "Clean Code", "Robert C. Martin", 2008),
        new(SeedBookIds.Refactoring, "Refactoring", "Martin Fowler", 1999),
    ];

    private readonly object _gate = new();

    public Task<IReadOnlyList<BookDto>> ListAsync(string? author, int maxResults, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BookDto[] snapshot;
        lock (_gate)
        {
            snapshot = _books.ToArray();
        }

        IEnumerable<BookDto> query = snapshot;

        if (!string.IsNullOrWhiteSpace(author))
        {
            query = query.Where(book => string.Equals(book.Author, author, StringComparison.OrdinalIgnoreCase));
        }

        IReadOnlyList<BookDto> result = query.Take(maxResults).ToArray();
        return Task.FromResult(result);
    }

    public Task<BookDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BookDto? book;
        lock (_gate)
        {
            book = _books.SingleOrDefault(candidate => candidate.Id == id);
        }

        return Task.FromResult(book);
    }

    public Task<BookDto> AddAsync(CreateBookRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        BookDto created = new(Guid.NewGuid(), request.Title!.Trim(), request.Author!.Trim(), request.YearPublished);
        lock (_gate)
        {
            _books.Add(created);
        }

        return Task.FromResult(created);
    }
}
