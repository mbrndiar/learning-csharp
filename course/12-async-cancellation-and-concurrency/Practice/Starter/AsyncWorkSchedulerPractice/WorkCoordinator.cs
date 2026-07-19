namespace LearningCSharp.Course.Unit12.Practice;

public sealed class WorkCoordinator
{
    private readonly object _gate = new();

    public Task<WorkSummary> RunAsync(
        IEnumerable<WorkItem> items,
        int maxConcurrency,
        Func<WorkItem, CancellationToken, Task<int>> processAsync,
        CancellationToken cancellationToken = default)
    {
        _ = _gate;
        throw new NotImplementedException("TODO: Coordinate bounded concurrent work and aggregate the results.");
    }
}
