using ReadingLog.Core;

namespace ReadingLog.Api;

public sealed record CreateReadingEntryRequestDto(
    Guid BookId,
    DateOnly StartedOn,
    DateOnly? FinishedOn,
    int PagesRead,
    int? Rating,
    string? Notes);
