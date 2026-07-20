using ReadingLog.Core;

namespace ReadingLog.Cli;

public enum CliExitCode
{
    Success = 0,
    InvalidArguments = 1,
    RequestFailed = 2,
    NotFound = 3,
    Cancelled = 4,
    UnexpectedResponse = 5,
    TimedOut = 6,
}
