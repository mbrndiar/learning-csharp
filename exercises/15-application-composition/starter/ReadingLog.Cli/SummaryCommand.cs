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
        // TODO: Reject null args or writers.
        // TODO: Validate the "summary <config-path>" usage and return the usage exit code otherwise.
        // TODO: Load config, run the application, and write each report line to stdout on success (exit code 0).
        // TODO: Map usage, config, and missing-file failures to the shared configuration result;
        // TODO: map invalid-data and HTTP failures to their documented stderr messages and exit codes.
        throw new NotImplementedException("TODO: Parse args, load config, run the app, and return exit codes.");
    }
}
