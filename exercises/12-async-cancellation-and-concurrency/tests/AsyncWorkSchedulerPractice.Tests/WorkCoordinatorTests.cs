using System.Text.Json;
using LearningCSharp.Course.Unit12.Practice;

namespace LearningCSharp.Course.Unit12.Practice.Tests;

public sealed class WorkCoordinatorTests
{
    [Fact]
    public async Task LoadAsyncReadsItemsFromJsonFile()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string workspace = CreateWorkspace();
        string path = Path.Combine(workspace, "plan.json");
        WorkItem[] expected =
        [
            new("alpha", 10, 3),
            new("beta", 20, 5),
        ];

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(expected), cancellationToken);

        IReadOnlyList<WorkItem> loaded = await WorkPlanLoader.LoadAsync(path, cancellationToken);

        Assert.Equal(expected, loaded);
    }

    [Fact]
    public async Task LoadAsyncThrowsInvalidDataForMalformedJson()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string workspace = CreateWorkspace();
        string path = Path.Combine(workspace, "plan.json");
        await File.WriteAllTextAsync(path, "{ not valid json", cancellationToken);

        await Assert.ThrowsAsync<InvalidDataException>(() => WorkPlanLoader.LoadAsync(path, cancellationToken));
    }

    [Fact]
    public async Task RunAsyncProcessesAllItemsAndAggregatesResults()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var coordinator = new WorkCoordinator();
        WorkItem[] items =
        [
            new("alpha", 30, 3),
            new("beta", 10, 5),
            new("gamma", 20, 7),
        ];

        WorkSummary summary = await coordinator.RunAsync(
            items,
            maxConcurrency: 2,
            async (item, cancellationToken) =>
            {
                await Task.Delay(item.DelayMilliseconds, cancellationToken);
                return item.Value;
            },
            cancellationToken);

        Assert.Equal(3, summary.StartedCount);
        Assert.Equal(3, summary.CompletedCount);
        Assert.Equal(15, summary.TotalValue);
        Assert.Equal(3, summary.CompletionOrder.Count);
    }

    [Fact]
    public async Task RunAsyncHonorsTheRequestedConcurrencyLimit()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var coordinator = new WorkCoordinator();
        WorkItem[] items = Enumerable.Range(1, 6).Select(index => new WorkItem($"item-{index}", 25, index)).ToArray();
        int currentConcurrency = 0;
        int maxObservedConcurrency = 0;

        WorkSummary summary = await coordinator.RunAsync(
            items,
            maxConcurrency: 2,
            async (item, cancellationToken) =>
            {
                int nowRunning = Interlocked.Increment(ref currentConcurrency);
                UpdateMax(ref maxObservedConcurrency, nowRunning);

                try
                {
                    await Task.Delay(item.DelayMilliseconds, cancellationToken);
                    return item.Value;
                }
                finally
                {
                    Interlocked.Decrement(ref currentConcurrency);
                }
            },
            cancellationToken);

        Assert.Equal(items.Length, summary.CompletedCount);
        Assert.Equal(2, maxObservedConcurrency);
    }

    [Fact]
    public async Task RunAsyncPropagatesProcessorExceptions()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var coordinator = new WorkCoordinator();
        WorkItem[] items = [new("alpha", 10, 3), new("beta", 10, 5)];

        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.RunAsync(
            items,
            maxConcurrency: 2,
            (item, cancellationToken) => item.Id == "beta"
                ? Task.FromException<int>(new InvalidOperationException("boom"))
                : Task.FromResult(item.Value),
            cancellationToken));
    }

    [Fact]
    public async Task RunAsyncHonorsCancellation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var coordinator = new WorkCoordinator();
        WorkItem[] items = Enumerable.Range(1, 4).Select(index => new WorkItem($"item-{index}", 50, index)).ToArray();
        using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(60));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.RunAsync(
            items,
            maxConcurrency: 2,
            async (item, cancellationToken) =>
            {
                await Task.Delay(item.DelayMilliseconds * 4, cancellationToken);
                return item.Value;
            },
            cancellationTokenSource.Token));
    }

    [Fact]
    public async Task RunAsyncRejectsNonPositiveConcurrency()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var coordinator = new WorkCoordinator();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => coordinator.RunAsync(
            [new WorkItem("alpha", 10, 1)],
            maxConcurrency: 0,
            (item, cancellationToken) => Task.FromResult(item.Value),
            cancellationToken));
    }

    private static void UpdateMax(ref int maxObservedConcurrency, int currentConcurrency)
    {
        while (true)
        {
            int snapshot = maxObservedConcurrency;
            if (currentConcurrency <= snapshot)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref maxObservedConcurrency, currentConcurrency, snapshot) == snapshot)
            {
                return;
            }
        }
    }

    private static string CreateWorkspace()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "generated", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
