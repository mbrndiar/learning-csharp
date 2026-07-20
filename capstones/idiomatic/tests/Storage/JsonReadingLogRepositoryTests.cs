using ReadingLog.Tests.TestInfrastructure;

namespace ReadingLog.Tests.Storage;

public sealed class JsonReadingLogRepositoryTests
{
    [Fact]
    public void ConstructorThrowsForBlankDirectory()
    {
        var exception = Assert.Throws<ArgumentException>(() => new JsonReadingLogRepository(new JsonReadingLogRepositoryOptions
        {
            StorageDirectory = " ",
            FileName = "reading-log.json",
        }));

        Assert.Contains("storage directory", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConstructorThrowsForUnsafeFileName()
    {
        var exception = Assert.Throws<ArgumentException>(() => new JsonReadingLogRepository(new JsonReadingLogRepositoryOptions
        {
            StorageDirectory = AppContext.BaseDirectory,
            FileName = "../reading-log.json",
        }));

        Assert.Contains("simple file name", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsyncReturnsEmptyWhenFileIsMissing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TestDirectory("storage-missing");
        var repository = CreateRepository(directory.Path);

        var snapshot = await repository.LoadAsync(cancellationToken);

        Assert.Empty(snapshot.Books);
        Assert.Empty(snapshot.Entries);
    }

    [Fact]
    public async Task LoadAsyncReturnsEmptyWhenFileIsZeroBytes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TestDirectory("storage-empty");
        var repository = CreateRepository(directory.Path);
        await File.WriteAllBytesAsync(repository.StorageFilePath, [], cancellationToken);

        var snapshot = await repository.LoadAsync(cancellationToken);

        Assert.Empty(snapshot.Books);
        Assert.Empty(snapshot.Entries);
    }

    [Fact]
    public async Task SaveAsyncAndLoadAsyncRoundTripSnapshot()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TestDirectory("storage-roundtrip");
        var repository = CreateRepository(directory.Path);
        var snapshot = new ReadingLogSnapshot([SampleData.Book()], [SampleData.Entry(finishedOn: new DateOnly(2026, 7, 2))]);

        await repository.SaveAsync(snapshot, cancellationToken);
        var loaded = await repository.LoadAsync(cancellationToken);

        Assert.Equivalent(snapshot, loaded);
    }

    [Fact]
    public async Task LoadAsyncThrowsForMalformedJson()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TestDirectory("storage-malformed-json");
        var repository = CreateRepository(directory.Path);
        await File.WriteAllTextAsync(repository.StorageFilePath, "{ not valid json", Encoding.UTF8, cancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => repository.LoadAsync(cancellationToken));

        Assert.Contains("malformed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsyncThrowsWhenSnapshotArraysAreMissing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TestDirectory("storage-missing-arrays");
        var repository = CreateRepository(directory.Path);
        await File.WriteAllTextAsync(repository.StorageFilePath, "{}", Encoding.UTF8, cancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => repository.LoadAsync(cancellationToken));

        Assert.Contains("'books' and 'entries' arrays", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsyncThrowsForInvalidSnapshotData()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TestDirectory("storage-invalid-data");
        var repository = CreateRepository(directory.Path);
        const string json = """
        {
          "books": [
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "title": "   ",
              "author": "Frank Herbert",
              "publicationYear": 1965,
              "isbn": "9780441172719",
              "createdAtUtc": "2026-07-01T12:00:00+00:00"
            }
          ],
          "entries": []
        }
        """;
        await File.WriteAllTextAsync(repository.StorageFilePath, json, Encoding.UTF8, cancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => repository.LoadAsync(cancellationToken));

        Assert.Contains("invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("""{"books":[null],"entries":[]}""")]
    [InlineData("""{"books":[],"entries":[null]}""")]
    public async Task LoadAsyncThrowsForNullSnapshotElements(string json)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TestDirectory("storage-null-element");
        var repository = CreateRepository(directory.Path);
        await File.WriteAllTextAsync(repository.StorageFilePath, json, Encoding.UTF8, cancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => repository.LoadAsync(cancellationToken));

        Assert.Contains("invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsyncHonorsCancellation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TestDirectory("storage-load-cancel");
        var repository = CreateRepository(directory.Path);
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await File.WriteAllTextAsync(repository.StorageFilePath, "{}", Encoding.UTF8, cancellationToken);
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.LoadAsync(cancellationTokenSource.Token));
    }

    [Fact]
    public async Task SaveAsyncHonorsCancellation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TestDirectory("storage-save-cancel");
        var repository = CreateRepository(directory.Path);
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.SaveAsync(new ReadingLogSnapshot([SampleData.Book()], []), cancellationTokenSource.Token));
    }

    [Fact]
    public async Task SaveAsyncThrowsForInvalidSnapshot()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TestDirectory("storage-invalid-save");
        var repository = CreateRepository(directory.Path);
        var invalidSnapshot = new ReadingLogSnapshot(
            [new Book(Guid.Empty, "Dune", "Frank Herbert", 1965, null, SampleData.FirstCreatedAt)],
            []);

        await Assert.ThrowsAsync<DomainValidationException>(() => repository.SaveAsync(invalidSnapshot, cancellationToken));
    }

    [Fact]
    public async Task SaveAsyncReplacesFileAndCleansTempFile()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TestDirectory("storage-replace");
        var repository = CreateRepository(directory.Path);
        var firstSnapshot = new ReadingLogSnapshot([SampleData.Book(title: "First")], []);
        var secondSnapshot = new ReadingLogSnapshot([SampleData.Book(title: "Second")], [SampleData.Entry(finishedOn: new DateOnly(2026, 7, 2))]);

        await repository.SaveAsync(firstSnapshot, cancellationToken);
        await repository.SaveAsync(secondSnapshot, cancellationToken);

        var fileContents = await File.ReadAllTextAsync(repository.StorageFilePath, cancellationToken);
        Assert.DoesNotContain("First", fileContents, StringComparison.Ordinal);
        Assert.Contains("Second", fileContents, StringComparison.Ordinal);
        Assert.False(File.Exists(repository.StorageFilePath + ".tmp"));

        var loaded = await repository.LoadAsync(cancellationToken);
        Assert.Equivalent(secondSnapshot, loaded);
    }

    [Fact]
    public async Task LoadAndSaveCanOverlapWithoutBreakingAtomicReplacement()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TestDirectory("storage-overlap");
        var repository = CreateRepository(directory.Path);
        Book[] books = Enumerable.Range(1, 20_000)
            .Select(index => SampleData.Book(
                id: Guid.NewGuid(),
                title: $"Book {index}"))
            .ToArray();
        var original = new ReadingLogSnapshot(books, []);
        var replacement = new ReadingLogSnapshot([SampleData.Book(title: "Replacement")], []);
        await repository.SaveAsync(original, cancellationToken);

        Task<ReadingLogSnapshot> loadTask = repository.LoadAsync(cancellationToken);
        await repository.SaveAsync(replacement, cancellationToken);
        ReadingLogSnapshot loadedDuringReplacement = await loadTask;

        Assert.Equal(original.Books.Count, loadedDuringReplacement.Books.Count);
        ReadingLogSnapshot finalSnapshot = await repository.LoadAsync(cancellationToken);
        Assert.Single(finalSnapshot.Books);
        Assert.Equal("Replacement", finalSnapshot.Books[0].Title);
    }

    private static JsonReadingLogRepository CreateRepository(string directory) =>
        new(new JsonReadingLogRepositoryOptions
        {
            StorageDirectory = directory,
            FileName = "reading-log.json",
        });
}
