namespace ReadingLog.Core;

public sealed record CreateBookRequest(
    string Title,
    string Author,
    int? PublicationYear,
    string? Isbn);
