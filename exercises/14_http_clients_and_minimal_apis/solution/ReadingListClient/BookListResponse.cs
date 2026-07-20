namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;

public sealed record BookListResponse(IReadOnlyList<BookDto> Books, string? AuthorFilter);
