using ReadingLog.Core;

namespace ReadingLog.Api;

public sealed record BookResponse(
    Guid Id,
    string Title,
    string Author,
    int? PublicationYear,
    string? Isbn,
    DateTimeOffset CreatedAtUtc);
