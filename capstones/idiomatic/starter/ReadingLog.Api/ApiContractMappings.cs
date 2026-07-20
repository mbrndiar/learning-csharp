using ReadingLog.Core;

namespace ReadingLog.Api;

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
