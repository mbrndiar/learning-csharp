namespace ReadingLog.Tests.TestInfrastructure;

internal sealed class TestDirectory : IDisposable
{
    public TestDirectory(string scope)
    {
        var sanitizedScope = string.Concat(scope.Select(static character =>
            char.IsLetterOrDigit(character) ? character : '-'));
        Path = System.IO.Path.Combine(AppContext.BaseDirectory, "artifacts", sanitizedScope, Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}

internal static class SampleData
{
    public static readonly DateTimeOffset FirstCreatedAt = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset SecondCreatedAt = new(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);

    public static Book Book(Guid? id = null, string title = "Dune", string author = "Frank Herbert", int? publicationYear = 1965, string? isbn = "9780441172719") =>
        new(id ?? Guid.Parse("11111111-1111-1111-1111-111111111111"), title, author, publicationYear, isbn, FirstCreatedAt);

    public static ReadingEntry Entry(
        Guid? id = null,
        Guid? bookId = null,
        DateOnly? startedOn = null,
        DateOnly? finishedOn = null,
        int pagesRead = 40,
        int? rating = 5,
        string? notes = "Strong opening") =>
        new(
            id ?? Guid.Parse("22222222-2222-2222-2222-222222222222"),
            bookId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            startedOn ?? new DateOnly(2026, 7, 1),
            finishedOn,
            pagesRead,
            rating,
            notes,
            SecondCreatedAt);
}
