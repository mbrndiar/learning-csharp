namespace ReadingLog.Core;

public sealed record CreateReadingEntryRequest(
    Guid BookId,
    DateOnly StartedOn,
    DateOnly? FinishedOn,
    int PagesRead,
    int? Rating,
    string? Notes);
