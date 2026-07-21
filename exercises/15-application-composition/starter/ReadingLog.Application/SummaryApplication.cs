namespace LearningCSharp.Exercises.ApplicationComposition.Application;

public sealed class SummaryApplication(IReadingLogSource source)
{
    public Task<SummaryReport> RunAsync(
        SummaryConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        _ = source;
        // TODO: Implement RunAsync so it rejects a null configuration and a
        // missing data file, loads entries through the source abstraction
        // (honoring cancellation), runs the domain calculation, and shapes the
        // report's output lines from the resulting summary.
        throw new NotImplementedException("TODO: Orchestrate the summary workflow through the source abstraction.");
    }
}
