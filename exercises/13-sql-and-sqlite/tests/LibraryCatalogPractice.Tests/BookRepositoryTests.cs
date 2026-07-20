using LearningCSharp.Course.Unit13.Practice;
using Microsoft.Data.Sqlite;

namespace LearningCSharp.Course.Unit13.Practice.Tests;

public sealed class BookRepositoryTests
{
    [Fact]
    public void CreateAssignsMonotonicIdsAcrossInserts()
    {
        using BookRepository repository = CreateRepository();

        int firstId = repository.Create("The Clockwork Library", "R. Adeyemi", "978-0-00-000001-1", 2019);
        int secondId = repository.Create("Tides of Pearl Bay", "S. Marchetti", "978-0-00-000002-2", 2021);

        Assert.True(firstId > 0);
        Assert.True(secondId > firstId);
    }

    [Fact]
    public void GetByIdReturnsMatchingBookAfterCreate()
    {
        using BookRepository repository = CreateRepository();

        int id = repository.Create("Notes from the Silent Grove", "J. Okafor", "978-0-00-000003-3", 2020, rating: 4);

        Book? loaded = repository.GetById(id);

        Assert.NotNull(loaded);
        Assert.Equal(id, loaded.Id);
        Assert.Equal("Notes from the Silent Grove", loaded.Title);
        Assert.Equal("J. Okafor", loaded.Author);
        Assert.Equal("978-0-00-000003-3", loaded.Isbn);
        Assert.Equal(2020, loaded.PublicationYear);
        Assert.Equal(4, loaded.Rating);
    }

    [Fact]
    public void GetByIdReturnsNullWhenBookIsMissing()
    {
        using BookRepository repository = CreateRepository();

        Book? loaded = repository.GetById(9999);

        Assert.Null(loaded);
    }

    [Fact]
    public void CreateRejectsDuplicateIsbn()
    {
        using BookRepository repository = CreateRepository();
        repository.Create("The Clockwork Library", "R. Adeyemi", "978-0-00-000001-1", 2019);

        Assert.Throws<SqliteException>(() =>
            repository.Create("A Different Title", "A Different Author", "978-0-00-000001-1", 2022));
    }

    [Fact]
    public void CreateRejectsPublicationYearBeforeMovableType()
    {
        using BookRepository repository = CreateRepository();

        Assert.Throws<SqliteException>(() =>
            repository.Create("Too Old", "Unknown", "978-0-00-000009-9", 1200));
    }

    [Fact]
    public void CreateRejectsRatingOutOfRange()
    {
        using BookRepository repository = CreateRepository();

        Assert.Throws<SqliteException>(() =>
            repository.Create("Off The Scale", "Unknown", "978-0-00-000008-8", 2020, rating: 6));
    }

    [Fact]
    public void ListOrdersByTitleAndAppliesMinimumYearFilter()
    {
        using BookRepository repository = CreateRepository();
        repository.Create("Zephyr Skies", "Author Z", "978-0-00-000004-4", 2015);
        repository.Create("Amber Fields", "Author A", "978-0-00-000005-5", 2022);
        repository.Create("Midnight Harbor", "Author M", "978-0-00-000006-6", 2010);

        IReadOnlyList<Book> books = repository.List(minimumPublicationYear: 2015);

        Assert.Equal(["Amber Fields", "Zephyr Skies"], books.Select(book => book.Title));
    }

    [Fact]
    public void ListRespectsLimit()
    {
        using BookRepository repository = CreateRepository();
        repository.Create("Alpha", "Author", "978-0-00-000010-1", 2001);
        repository.Create("Beta", "Author", "978-0-00-000010-2", 2002);
        repository.Create("Gamma", "Author", "978-0-00-000010-3", 2003);

        IReadOnlyList<Book> books = repository.List(limit: 2);

        Assert.Equal(2, books.Count);
    }

    [Fact]
    public void UpdateRatingChangesStoredValueAndReturnsTrue()
    {
        using BookRepository repository = CreateRepository();
        int id = repository.Create("The Clockwork Library", "R. Adeyemi", "978-0-00-000001-1", 2019);

        bool updated = repository.UpdateRating(id, 5);

        Assert.True(updated);
        Assert.Equal(5, repository.GetById(id)!.Rating);
    }

    [Fact]
    public void UpdateRatingReturnsFalseWhenBookIsMissing()
    {
        using BookRepository repository = CreateRepository();

        bool updated = repository.UpdateRating(9999, 3);

        Assert.False(updated);
    }

    [Fact]
    public void UpdateRatingRejectsOutOfRangeValueAndKeepsPreviousRating()
    {
        using BookRepository repository = CreateRepository();
        int id = repository.Create("The Clockwork Library", "R. Adeyemi", "978-0-00-000001-1", 2019, rating: 3);

        Assert.Throws<SqliteException>(() => repository.UpdateRating(id, 10));
        Assert.Equal(3, repository.GetById(id)!.Rating);
    }

    [Fact]
    public void DisposeClosesConnectionAndPreventsFurtherUse()
    {
        BookRepository repository = CreateRepository();
        repository.Dispose();
        repository.Dispose();

        Assert.Throws<ObjectDisposedException>(() => repository.GetById(1));
    }

    private static BookRepository CreateRepository()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "generated", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string databasePath = Path.Combine(directory, "library-catalog.db");
        return new BookRepository(databasePath);
    }
}
