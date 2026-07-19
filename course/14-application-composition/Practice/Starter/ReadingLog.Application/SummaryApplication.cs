namespace LearningCSharp.Course.Unit14.Practice.Application;

public sealed class SummaryApplication(IReadingLogSource source)
{
    public Task<SummaryReport> RunAsync(
        SummaryConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        _ = source;
        throw new NotImplementedException("TODO: Orchestrate the summary workflow through the source abstraction.");
    }
}
