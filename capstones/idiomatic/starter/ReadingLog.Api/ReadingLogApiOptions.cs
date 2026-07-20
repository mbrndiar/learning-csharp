using ReadingLog.Core;

namespace ReadingLog.Api;

public sealed class ReadingLogApiOptions
{
    public string StorageDirectory { get; init; } = "App_Data";

    public string StorageFileName { get; init; } = "reading-log.json";
}
