namespace ReadingLog.Cli;

public sealed record BookDetailsResponse(
    BookResponse Book,
    IReadOnlyList<ReadingEntryResponse> Entries,
    int TotalPagesRead,
    bool HasFinished,
    double? AverageRating);
