using ReadingLog.Core;

namespace ReadingLog.Api;

public sealed record CreateBookRequestDto(string Title, string Author, int? PublicationYear, string? Isbn);

public sealed record CreateReadingEntryRequestDto(
    Guid BookId,
    DateOnly StartedOn,
    DateOnly? FinishedOn,
    int PagesRead,
    int? Rating,
    string? Notes);

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

public sealed record ReadingOverviewResponse(
    int BookCount,
    int EntryCount,
    int TotalPagesRead,
    int FinishedBookCount,
    BookResponse? MostRecentBook);

public sealed class ReadingLogApiOptions
{
    public string StorageDirectory { get; init; } = "App_Data";

    public string StorageFileName { get; init; } = "reading-log.json";
}

internal static class ApiContractMappings
{
    public static BookResponse ToResponse(this Book book) =>
        new(book.Id, book.Title, book.Author, book.PublicationYear, book.Isbn, book.CreatedAtUtc);

    public static ReadingEntryResponse ToResponse(this ReadingEntry entry) =>
        new(entry.Id, entry.BookId, entry.StartedOn, entry.FinishedOn, entry.PagesRead, entry.Rating, entry.Notes, entry.CreatedAtUtc);

    public static BookDetailsResponse ToResponse(this BookDetails details) =>
        new(details.Book.ToResponse(), details.Entries.Select(ToResponse).ToArray(), details.TotalPagesRead, details.HasFinished, details.AverageRating);

    public static ReadingOverviewResponse ToResponse(this ReadingOverview overview) =>
        new(overview.BookCount, overview.EntryCount, overview.TotalPagesRead, overview.FinishedBookCount, overview.MostRecentBook?.ToResponse());
}
