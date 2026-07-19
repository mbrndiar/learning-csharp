using LearningCSharp.Course.Unit13.Practice.Client;

namespace LearningCSharp.Course.Unit13.Practice.Api;

public interface IBookRepository
{
    Task<IReadOnlyList<BookDto>> ListAsync(string? author, int maxResults, CancellationToken cancellationToken);

    Task<BookDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<BookDto> AddAsync(CreateBookRequest request, CancellationToken cancellationToken);
}
