namespace LearningCSharp.Exercises.ApplicationComposition.Application;

public sealed class SummaryApplication(IReadingLogSource source)
{
    public Task<SummaryReport> RunAsync(
        SummaryConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        _ = source;
        // TODO: Reject a null configuration and a missing data file.
        // TODO: Load entries through the source abstraction (honoring cancellation), run the domain calculation,
        // TODO: and shape the report's output lines from the resulting summary.
        throw new NotImplementedException("TODO: Orchestrate the summary workflow through the source abstraction.");
    }
}
