namespace LearningCSharp.Course.Unit12.Practice;

public sealed class WorkCoordinator
{
    private readonly object _gate = new();

    public async Task<WorkSummary> RunAsync(
        IEnumerable<WorkItem> items,
        int maxConcurrency,
        Func<WorkItem, CancellationToken, Task<int>> processAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(processAsync);

        if (maxConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Use a positive concurrency limit.");
        }

        WorkItem[] materialized = items.ToArray();
        using SemaphoreSlim semaphore = new(maxConcurrency, maxConcurrency);
        int completedCount = 0;
        int totalValue = 0;
        List<string> completionOrder = [];

        Task[] tasks = materialized.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                int value = await processAsync(item, cancellationToken);
                lock (_gate)
                {
                    completedCount++;
                    totalValue += value;
                    completionOrder.Add(item.Id);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }).ToArray();

        await Task.WhenAll(tasks);
        return new WorkSummary(materialized.Length, completedCount, totalValue, completionOrder);
    }
}
