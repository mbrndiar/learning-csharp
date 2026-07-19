namespace ReadingLog.Cli;

public sealed record BookResponse(
    Guid Id,
    string Title,
    string Author,
    int? PublicationYear,
    string? Isbn,
    DateTimeOffset CreatedAtUtc);

public sealed record ReadingEntryResponse(
    Guid Id,
    Guid BookId,
    DateOnly StartedOn,
    DateOnly? FinishedOn,
    int PagesRead,
    int? Rating,
    string? Notes,
    DateTimeOffset CreatedAtUtc);

public sealed record BookDetailsResponse(
    BookResponse Book,
    IReadOnlyList<ReadingEntryResponse> Entries,
    int TotalPagesRead,
    bool HasFinished,
    double? AverageRating);

internal sealed record ProblemResponse(string? Title, string? Detail, int? Status);
