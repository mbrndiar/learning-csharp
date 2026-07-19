namespace ReadingLog.Core;

public sealed record BookDetails(
    Book Book,
    IReadOnlyList<ReadingEntry> Entries,
    int TotalPagesRead,
    bool HasFinished,
    double? AverageRating);
