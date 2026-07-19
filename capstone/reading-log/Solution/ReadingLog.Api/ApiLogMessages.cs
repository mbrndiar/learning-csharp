namespace ReadingLog.Api;

internal static partial class ApiLogMessages
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Error, Message = "Stored reading log data is malformed.")]
    public static partial void StoredDataMalformed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Added book {BookId}.")]
    public static partial void AddedBook(ILogger logger, Guid bookId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Added reading entry {EntryId} for book {BookId}.")]
    public static partial void AddedReadingEntry(ILogger logger, Guid entryId, Guid bookId);
}
