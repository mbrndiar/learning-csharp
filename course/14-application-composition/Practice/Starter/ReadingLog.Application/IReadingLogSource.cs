using LearningCSharp.Course.Unit14.Practice.Domain;

namespace LearningCSharp.Course.Unit14.Practice.Application;

public interface IReadingLogSource
{
    Task<IReadOnlyList<ReadingEntry>> LoadAsync(string dataFile, CancellationToken cancellationToken);
}
