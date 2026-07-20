namespace ReadingLog.Cli;

public sealed record BookResponse(
    Guid Id,
    string Title,
    string Author,
    int? PublicationYear,
    string? Isbn,
    DateTimeOffset CreatedAtUtc);
