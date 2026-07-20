using ReadingLog.Core;

namespace ReadingLog.Api;

public sealed record ReadingOverviewResponse(
    int BookCount,
    int EntryCount,
    int TotalPagesRead,
    int FinishedBookCount,
    BookResponse? MostRecentBook);
