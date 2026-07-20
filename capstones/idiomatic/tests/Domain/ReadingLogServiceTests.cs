using ReadingLog.Tests.TestInfrastructure;

namespace ReadingLog.Tests.Domain;

public sealed class ReadingLogServiceTests
{
    [Fact]
    public async Task AddBookAsyncPersistsBookWithTrimmedValues()
    {
        var repository = new InMemoryReadingLogRepository();
        var service = new ReadingLogService(repository);

        var created = await service.AddBookAsync(new CreateBookRequest("  Dune  ", "  Frank Herbert  ", 1965, "  9780441172719  "), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Dune", created.Title);
        Assert.Single(repository.Snapshot.Books);
    }

    [Fact]
    public async Task AddReadingEntryAsyncThrowsWhenBookDoesNotExist()
    {
        var repository = new InMemoryReadingLogRepository();
        var service = new ReadingLogService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddReadingEntryAsync(
            new CreateReadingEntryRequest(Guid.NewGuid(), new DateOnly(2026, 7, 1), null, 25, 4, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task GetBookAsyncReturnsSummaryValues()
    {
        var book = SampleData.Book();
        var repository = new InMemoryReadingLogRepository(new ReadingLogSnapshot(
            [book],
            [
                SampleData.Entry(pagesRead: 40, rating: 5, finishedOn: new DateOnly(2026, 7, 2)),
                SampleData.Entry(id: Guid.Parse("33333333-3333-3333-3333-333333333333"), startedOn: new DateOnly(2026, 7, 3), pagesRead: 15, rating: 3, notes: "Short session")
            ]));
        var service = new ReadingLogService(repository);

        var details = await service.GetBookAsync(book.Id, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(55, details.TotalPagesRead);
        Assert.True(details.HasFinished);
        Assert.Equal(4d, details.AverageRating);
        Assert.Equal(2, details.Entries.Count);
    }

    [Fact]
    public async Task GetBookAsyncReturnsNullWhenBookDoesNotExist()
    {
        var service = new ReadingLogService(new InMemoryReadingLogRepository());

        var details = await service.GetBookAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(details);
    }

    [Fact]
    public async Task AddReadingEntryAsyncPersistsEntryForExistingBook()
    {
        var book = SampleData.Book();
        var repository = new InMemoryReadingLogRepository(new ReadingLogSnapshot([book], []));
        var service = new ReadingLogService(repository);

        var entry = await service.AddReadingEntryAsync(
            new CreateReadingEntryRequest(book.Id, new DateOnly(2026, 7, 6), null, 25, null, null),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Single(repository.Snapshot.Entries);
        Assert.Equal(book.Id, repository.Snapshot.Entries[0].BookId);
    }

    [Fact]
    public async Task ListEntriesAsyncFiltersByBookId()
    {
        var book = SampleData.Book();
        var otherBook = SampleData.Book(id: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), title: "Hyperion", author: "Dan Simmons");
        var repository = new InMemoryReadingLogRepository(new ReadingLogSnapshot(
            [book, otherBook],
            [
                SampleData.Entry(),
                SampleData.Entry(
                    id: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    bookId: otherBook.Id,
                    startedOn: new DateOnly(2026, 7, 5),
                    pagesRead: 18,
                    rating: null,
                    notes: null)
            ]));
        var service = new ReadingLogService(repository);

        var entries = await service.ListEntriesAsync(book.Id, CancellationToken.None);

        Assert.Single(entries);
        Assert.Equal(book.Id, entries[0].BookId);
    }

    [Fact]
    public async Task GetOverviewAsyncReturnsCountsAndMostRecentBook()
    {
        var olderBook = SampleData.Book(id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), title: "Dune", author: "Frank Herbert");
        var newerBook = new Book(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Hyperion",
            "Dan Simmons",
            1989,
            null,
            SampleData.SecondCreatedAt.AddDays(2));
        var repository = new InMemoryReadingLogRepository(new ReadingLogSnapshot(
            [olderBook, newerBook],
            [
                SampleData.Entry(bookId: olderBook.Id, finishedOn: new DateOnly(2026, 7, 2), pagesRead: 40),
                SampleData.Entry(
                    id: Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    bookId: newerBook.Id,
                    startedOn: new DateOnly(2026, 7, 3),
                    pagesRead: 15,
                    rating: null,
                    notes: null)
            ]));
        var service = new ReadingLogService(repository);

        var overview = await service.GetOverviewAsync(CancellationToken.None);

        Assert.Equal(2, overview.BookCount);
        Assert.Equal(2, overview.EntryCount);
        Assert.Equal(55, overview.TotalPagesRead);
        Assert.Equal(1, overview.FinishedBookCount);
        Assert.Equal(newerBook.Id, overview.MostRecentBook?.Id);
    }

    [Fact]
    public async Task ListBooksAsyncOrdersByTitleThenAuthor()
    {
        var repository = new InMemoryReadingLogRepository(new ReadingLogSnapshot(
            [
                SampleData.Book(id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), title: "The Hobbit", author: "Tolkien"),
                SampleData.Book(id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), title: "Dune", author: "Herbert"),
                SampleData.Book(id: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), title: "Dune", author: "Anderson")
            ],
            []));
        var service = new ReadingLogService(repository);

        var books = await service.ListBooksAsync(CancellationToken.None);

        Assert.Collection(
            books,
            book => Assert.Equal("Anderson", book.Author),
            book => Assert.Equal("Herbert", book.Author),
            book => Assert.Equal("The Hobbit", book.Title));
    }

    [Fact]
    public async Task GetBookAsyncReturnsNullAverageWhenEntriesHaveNoRatings()
    {
        var book = SampleData.Book();
        var repository = new InMemoryReadingLogRepository(new ReadingLogSnapshot(
            [book],
            [SampleData.Entry(rating: null, notes: null)]));
        var service = new ReadingLogService(repository);

        var details = await service.GetBookAsync(book.Id, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Null(details.AverageRating);
        Assert.False(details.HasFinished);
    }
}
