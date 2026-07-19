using LearningCSharp.Course.Unit14.Practice.Application;

namespace LearningCSharp.Course.Unit14.Practice.Cli;

public sealed class SummaryCommand(SummaryApplication application)
{
    public Task<int> RunAsync(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        _ = application;
        throw new NotImplementedException("TODO: Parse args, load config, run the app, and return exit codes.");
    }
}
