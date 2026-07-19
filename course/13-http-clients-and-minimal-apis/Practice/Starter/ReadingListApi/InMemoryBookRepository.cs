using LearningCSharp.Course.Unit13.Practice.Client;

namespace LearningCSharp.Course.Unit13.Practice.Api;

public sealed class InMemoryBookRepository : IBookRepository
{
    public Task<BookDto> AddAsync(CreateBookRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new BookDto(Guid.Empty, request.Title ?? string.Empty, request.Author ?? string.Empty, request.YearPublished));
    }

    public Task<BookDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult<BookDto?>(null);
    }

    public Task<IReadOnlyList<BookDto>> ListAsync(string? author, int maxResults, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<BookDto>>(Array.Empty<BookDto>());
    }
}
