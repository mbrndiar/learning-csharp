namespace ReadingLog.Core;

public sealed record Book(
    Guid Id,
    string Title,
    string Author,
    int? PublicationYear,
    string? Isbn,
    DateTimeOffset CreatedAtUtc);
