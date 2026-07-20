using LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;

namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Core;

public interface IBookRepository
{
    Task<IReadOnlyList<BookDto>> ListAsync(string? author, int maxResults, CancellationToken cancellationToken);

    Task<BookDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<BookDto> AddAsync(CreateBookRequest request, CancellationToken cancellationToken);
}
