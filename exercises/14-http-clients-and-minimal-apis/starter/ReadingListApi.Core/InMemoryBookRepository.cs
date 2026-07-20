using LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;

namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Core;

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
