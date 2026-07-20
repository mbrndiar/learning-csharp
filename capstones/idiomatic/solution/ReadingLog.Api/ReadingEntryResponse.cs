using ReadingLog.Core;

namespace ReadingLog.Api;

public sealed record ReadingEntryResponse(
    Guid Id,
    Guid BookId,
    DateOnly StartedOn,
    DateOnly? FinishedOn,
    int PagesRead,
    int? Rating,
    string? Notes,
    DateTimeOffset CreatedAtUtc);
