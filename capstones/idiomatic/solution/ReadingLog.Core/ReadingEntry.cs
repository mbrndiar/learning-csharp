namespace ReadingLog.Core;

public sealed record ReadingEntry(
    Guid Id,
    Guid BookId,
    DateOnly StartedOn,
    DateOnly? FinishedOn,
    int PagesRead,
    int? Rating,
    string? Notes,
    DateTimeOffset CreatedAtUtc);
