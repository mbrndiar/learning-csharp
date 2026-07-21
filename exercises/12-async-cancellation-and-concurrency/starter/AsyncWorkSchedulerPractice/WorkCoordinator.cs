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
        // TODO: Reject null items or processor, and a non-positive concurrency limit (ArgumentOutOfRangeException).
        // TODO: Bound how many items run at once to the requested limit.
        // TODO: Guard the shared totals and completion order with the lock while aggregating each result.
        // TODO: Let processor faults and cancellation propagate to the caller.
        // TODO: Report started/completed counts, total value, and completion order.
        throw new NotImplementedException("TODO: Coordinate bounded concurrent work and aggregate the results.");
    }
}
