using ReadingLog.Core;

namespace ReadingLog.Api;

public sealed record CreateBookRequestDto(string Title, string Author, int? PublicationYear, string? Isbn);
