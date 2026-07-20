using System.Text.Json;

JsonSerializerOptions jsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
};

string dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDirectory);
string planPath = Path.Combine(dataDirectory, "plan.json");

WorkItem[] items =
[
    new("alpha", 250, 3),
    new("beta", 100, 5),
    new("gamma", 150, 7),
];

await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(items, jsonOptions), CancellationToken.None);
Console.WriteLine($"Saved work plan to {planPath}");

WorkItem[] loaded = await LoadAsync(planPath, jsonOptions, CancellationToken.None);
var coordinator = new WorkCoordinator();
WorkSummary summary = await coordinator.RunAsync(
    loaded,
    maxConcurrency: 2,
    async (item, cancellationToken) =>
    {
        await Task.Delay(item.DelayMilliseconds, cancellationToken);
        Console.WriteLine($"Completed {item.Id} with value {item.Value}");
        return item.Value;
    },
    CancellationToken.None);

Console.WriteLine();
Console.WriteLine($"Completed: {summary.CompletedCount}/{summary.StartedCount}");
Console.WriteLine($"Total value: {summary.TotalValue}");
Console.WriteLine($"Completion order: {string.Join(", ", summary.CompletionOrder)}");

static async Task<WorkItem[]> LoadAsync(string path, JsonSerializerOptions options, CancellationToken cancellationToken)
{
    await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
    WorkItem[]? loaded = await JsonSerializer.DeserializeAsync<WorkItem[]>(stream, options, cancellationToken);
    return loaded ?? throw new InvalidDataException("The sample plan was empty.");
}

internal sealed record WorkItem(string Id, int DelayMilliseconds, int Value);

internal sealed record WorkSummary(int StartedCount, int CompletedCount, int TotalValue, IReadOnlyList<string> CompletionOrder);

internal sealed class WorkCoordinator
{
    private readonly object _gate = new();

    public async Task<WorkSummary> RunAsync(
        IEnumerable<WorkItem> items,
        int maxConcurrency,
        Func<WorkItem, CancellationToken, Task<int>> processAsync,
        CancellationToken cancellationToken)
    {
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
