namespace ReadingLog.Core;

public sealed record ReadingOverview(
    int BookCount,
    int EntryCount,
    int TotalPagesRead,
    int FinishedBookCount,
    Book? MostRecentBook);
