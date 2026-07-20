namespace ReadingLog.Core;

public sealed class ReadingLogService
{
    private readonly IReadingLogRepository _repository;

    public ReadingLogService(IReadingLogRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IReadOnlyList<Book>> ListBooksAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken);
        return snapshot.Books
            .OrderBy(book => book.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(book => book.Author, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<BookDetails?> GetBookAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken);
        var book = snapshot.Books.SingleOrDefault(candidate => candidate.Id == bookId);
        if (book is null)
        {
            return null;
        }

        var entries = snapshot.Entries
            .Where(entry => entry.BookId == bookId)
            .OrderByDescending(entry => entry.StartedOn)
            .ThenByDescending(entry => entry.CreatedAtUtc)
            .ToArray();

        var ratings = entries.Where(entry => entry.Rating.HasValue).Select(entry => entry.Rating!.Value).ToArray();
        return new BookDetails(
            book,
            entries,
            entries.Sum(entry => entry.PagesRead),
            entries.Any(entry => entry.FinishedOn is not null),
            ratings.Length == 0 ? null : Math.Round(ratings.Average(), 2));
    }

    public async Task<IReadOnlyList<ReadingEntry>> ListEntriesAsync(Guid? bookId, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken);
        return snapshot.Entries
            .Where(entry => bookId is null || entry.BookId == bookId.Value)
            .OrderByDescending(entry => entry.StartedOn)
            .ThenByDescending(entry => entry.CreatedAtUtc)
            .ToArray();
    }

    public async Task<ReadingOverview> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken);
        return new ReadingOverview(
            snapshot.Books.Count,
            snapshot.Entries.Count,
            snapshot.Entries.Sum(entry => entry.PagesRead),
            snapshot.Entries.Where(entry => entry.FinishedOn is not null).Select(entry => entry.BookId).Distinct().Count(),
            snapshot.Books.OrderByDescending(book => book.CreatedAtUtc).FirstOrDefault());
    }

    public Task<Book> AddBookAsync(CreateBookRequest request, CancellationToken cancellationToken)
    {
        _ = ReadingLogValidation.ValidateCreateBookRequest(request);
        _ = _repository;
        cancellationToken.ThrowIfCancellationRequested();

        // TODO(m3): Implement write behavior and persistence in milestone 3.
        throw new NotSupportedException("TODO(m3): AddBookAsync is intentionally left for the milestone implementation.");
    }

    public Task<ReadingEntry> AddReadingEntryAsync(CreateReadingEntryRequest request, CancellationToken cancellationToken)
    {
        _ = ReadingLogValidation.ValidateCreateReadingEntryRequest(request);
        _ = _repository;
        cancellationToken.ThrowIfCancellationRequested();

        // TODO(m3): Implement entry creation and missing-book handling in milestone 3.
        throw new NotSupportedException("TODO(m3): AddReadingEntryAsync is intentionally left for the milestone implementation.");
    }
}
