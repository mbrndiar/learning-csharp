using LearningCSharp.Course.Unit14.Practice.Application;
using LearningCSharp.Course.Unit14.Practice.Domain;

namespace LearningCSharp.Course.Unit14.Practice.Cli;

public sealed class JsonReadingLogSource : IReadingLogSource
{
    public Task<IReadOnlyList<ReadingEntry>> LoadAsync(string dataFile, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("TODO: Read the data file from disk and deserialize it.");
    }
}
