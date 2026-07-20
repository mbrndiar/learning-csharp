namespace Tasks.Server.MinimalApi;

/// <summary>
/// Source-generated logging for the Minimal API adapter. Keeping the message
/// definition separate lets the exception middleware record unexpected failures
/// without allocating on the success path.
/// </summary>
internal static partial class ApiLog
{
    [LoggerMessage(EventId = 2000, Level = LogLevel.Error, Message = "Minimal API Task request failed")]
    public static partial void RequestFailed(ILogger logger, Exception exception);
}
