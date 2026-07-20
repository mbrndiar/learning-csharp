namespace ReadingLog.Tests.TestInfrastructure;

internal sealed class InMemoryReadingLogRepository : IReadingLogRepository
{
    public InMemoryReadingLogRepository(ReadingLogSnapshot? snapshot = null)
    {
        Snapshot = snapshot ?? ReadingLogSnapshot.Empty;
    }

    public ReadingLogSnapshot Snapshot { get; private set; }

    public Task<ReadingLogSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Snapshot);
    }

    public Task SaveAsync(ReadingLogSnapshot snapshot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Snapshot = snapshot;
        return Task.CompletedTask;
    }
}
