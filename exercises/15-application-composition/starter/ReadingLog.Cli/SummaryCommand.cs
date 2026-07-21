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
        // TODO: Implement RunAsync so it rejects null args or writers, validates
        // the "summary <config-path>" usage and returns the usage exit code
        // otherwise, loads config, runs the application, and writes each report
        // line to stdout on success (exit code 0), maps usage/config/missing-file
        // failures to the shared configuration result, and maps invalid-data and
        // HTTP failures to their documented stderr messages and exit codes.
        throw new NotImplementedException("TODO: Parse args, load config, run the app, and return exit codes.");
    }
}
