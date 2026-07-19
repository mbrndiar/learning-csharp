namespace ReadingLog.Cli;

public sealed class ReadingLogApiClientOptions
{
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(5);
}
