using LearningCSharp.Exercises.ApplicationComposition.Application;

namespace LearningCSharp.Exercises.ApplicationComposition.Cli;

public sealed class SummaryCommand(SummaryApplication application, bool resolveDataFileAsFilePath = true)
{
    public Task<int> RunAsync(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        _ = application;
        _ = resolveDataFileAsFilePath;
        throw new NotImplementedException("TODO: Parse args, load config, run the app, and return exit codes.");
    }
}
