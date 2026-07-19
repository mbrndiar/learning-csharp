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

    public async Task<Book> AddBookAsync(CreateBookRequest request, CancellationToken cancellationToken)
    {
        var normalizedRequest = ReadingLogValidation.ValidateCreateBookRequest(request);
        var snapshot = await _repository.LoadAsync(cancellationToken);
        var books = snapshot.Books.ToList();

        var book = new Book(
            Guid.NewGuid(),
            normalizedRequest.Title,
            normalizedRequest.Author,
            normalizedRequest.PublicationYear,
            normalizedRequest.Isbn,
            DateTimeOffset.UtcNow);

        books.Add(book);
        await _repository.SaveAsync(snapshot with { Books = books.ToArray() }, cancellationToken);
        return book;
    }

    public async Task<ReadingEntry> AddReadingEntryAsync(CreateReadingEntryRequest request, CancellationToken cancellationToken)
    {
        var normalizedRequest = ReadingLogValidation.ValidateCreateReadingEntryRequest(request);
        var snapshot = await _repository.LoadAsync(cancellationToken);
        if (snapshot.Books.All(book => book.Id != normalizedRequest.BookId))
        {
            throw new KeyNotFoundException($"Book '{normalizedRequest.BookId}' was not found.");
        }

        var entries = snapshot.Entries.ToList();
        var entry = new ReadingEntry(
            Guid.NewGuid(),
            normalizedRequest.BookId,
            normalizedRequest.StartedOn,
            normalizedRequest.FinishedOn,
            normalizedRequest.PagesRead,
            normalizedRequest.Rating,
            normalizedRequest.Notes,
            DateTimeOffset.UtcNow);

        entries.Add(entry);
        await _repository.SaveAsync(snapshot with { Entries = entries.ToArray() }, cancellationToken);
        return entry;
    }
}
