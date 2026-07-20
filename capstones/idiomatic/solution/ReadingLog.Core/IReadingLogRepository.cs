namespace ReadingLog.Core;

public interface IReadingLogRepository
{
    Task<ReadingLogSnapshot> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(ReadingLogSnapshot snapshot, CancellationToken cancellationToken);
}
