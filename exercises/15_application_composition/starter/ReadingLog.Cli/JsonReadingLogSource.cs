using LearningCSharp.Exercises.ApplicationComposition.Application;
using LearningCSharp.Exercises.ApplicationComposition.Domain;

namespace LearningCSharp.Exercises.ApplicationComposition.Cli;

public sealed class JsonReadingLogSource : IReadingLogSource
{
    public Task<IReadOnlyList<ReadingEntry>> LoadAsync(string location, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("TODO: Read the data file from disk and deserialize it.");
    }
}
