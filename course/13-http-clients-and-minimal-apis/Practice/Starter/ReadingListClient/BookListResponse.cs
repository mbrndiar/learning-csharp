namespace LearningCSharp.Course.Unit13.Practice.Client;

public sealed record BookListResponse(IReadOnlyList<BookDto> Books, string? AuthorFilter);
