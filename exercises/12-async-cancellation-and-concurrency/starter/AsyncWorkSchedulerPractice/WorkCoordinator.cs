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
        // TODO: Implement RunAsync so it rejects null items or processor and a
        // non-positive concurrency limit (ArgumentOutOfRangeException), bounds how
        // many items run at once to the requested limit, guards the shared totals
        // and completion order with the lock while aggregating each result, lets
        // processor faults and cancellation propagate to the caller, and reports
        // started/completed counts, total value, and completion order.
        throw new NotImplementedException("TODO: Coordinate bounded concurrent work and aggregate the results.");
    }
}
