namespace ReadingLog.Core;

public sealed record ReadingLogSnapshot(IReadOnlyList<Book> Books, IReadOnlyList<ReadingEntry> Entries)
{
    public static ReadingLogSnapshot Empty { get; } = new(Array.Empty<Book>(), Array.Empty<ReadingEntry>());
}
