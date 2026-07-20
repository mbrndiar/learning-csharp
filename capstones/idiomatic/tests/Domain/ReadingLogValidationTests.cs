namespace ReadingLog.Tests.Domain;

public sealed class ReadingLogValidationTests
{
    [Fact]
    public void ValidateCreateBookRequestThrowsForBlankTitle()
    {
        var request = new CreateBookRequest("   ", "Frank Herbert", 1965, null);

        var exception = Assert.Throws<DomainValidationException>(() => ReadingLogValidation.ValidateCreateBookRequest(request));

        Assert.Contains("title", exception.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateCreateBookRequestTrimsValues()
    {
        var normalized = ReadingLogValidation.ValidateCreateBookRequest(new CreateBookRequest("  Dune  ", "  Frank Herbert  ", 1965, "  9780441172719  "));

        Assert.Equal("Dune", normalized.Title);
        Assert.Equal("Frank Herbert", normalized.Author);
        Assert.Equal("9780441172719", normalized.Isbn);
    }

    [Fact]
    public void ValidateCreateBookRequestThrowsForOutOfRangePublicationYear()
    {
        var request = new CreateBookRequest("Dune", "Frank Herbert", 1200, null);

        var exception = Assert.Throws<DomainValidationException>(() => ReadingLogValidation.ValidateCreateBookRequest(request));

        Assert.Contains("publicationYear", exception.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateCreateBookRequestThrowsForBlankOptionalIsbn()
    {
        var request = new CreateBookRequest("Dune", "Frank Herbert", 1965, "   ");

        var exception = Assert.Throws<DomainValidationException>(() => ReadingLogValidation.ValidateCreateBookRequest(request));

        Assert.Contains("isbn", exception.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateCreateReadingEntryRequestThrowsForReversedDates()
    {
        var request = new CreateReadingEntryRequest(Guid.NewGuid(), new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 1), 10, 4, null);

        var exception = Assert.Throws<DomainValidationException>(() => ReadingLogValidation.ValidateCreateReadingEntryRequest(request));

        Assert.Contains("finishedOn", exception.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateCreateReadingEntryRequestThrowsForInvalidPagesAndRating()
    {
        var request = new CreateReadingEntryRequest(Guid.Empty, new DateOnly(2026, 7, 10), null, 0, 9, "   ");

        var exception = Assert.Throws<DomainValidationException>(() => ReadingLogValidation.ValidateCreateReadingEntryRequest(request));

        Assert.Contains("bookId", exception.Errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("pagesRead", exception.Errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("rating", exception.Errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("notes", exception.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateSnapshotThrowsForUnknownBookReference()
    {
        var snapshot = new ReadingLogSnapshot(
            [SampleData.Book()],
            [SampleData.Entry(bookId: Guid.NewGuid())]);

        var exception = Assert.Throws<DomainValidationException>(() => ReadingLogValidation.ValidateSnapshot(snapshot));

        Assert.Contains("bookId", string.Join(',', exception.Errors.Keys), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateSnapshotThrowsForDuplicateIdsAndInvalidStoredValues()
    {
        var bookId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var entryId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var snapshot = new ReadingLogSnapshot(
            [
                new Book(bookId, "Dune", "Frank Herbert", 1965, null, SampleData.FirstCreatedAt),
                new Book(bookId, " ", "Frank Herbert", 1965, null, SampleData.SecondCreatedAt)
            ],
            [
                new ReadingEntry(entryId, bookId, new DateOnly(2026, 7, 2), null, 10, 4, null, SampleData.SecondCreatedAt),
                new ReadingEntry(entryId, Guid.Empty, new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 3), 0, 7, " ", SampleData.SecondCreatedAt)
            ]);

        var exception = Assert.Throws<DomainValidationException>(() => ReadingLogValidation.ValidateSnapshot(snapshot));

        Assert.Contains("books[1].id", exception.Errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("entries[1].id", exception.Errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("entries[1].bookId", exception.Errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("entries[1].pagesRead", exception.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }
}
