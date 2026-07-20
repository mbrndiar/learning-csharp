namespace ReadingLog.Storage.Json;

public sealed class JsonReadingLogRepositoryOptions
{
    public string StorageDirectory { get; init; } = string.Empty;

    public string FileName { get; init; } = "reading-log.json";
}
