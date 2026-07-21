using LearningCSharp.Exercises.ApplicationComposition.Application;
using LearningCSharp.Exercises.ApplicationComposition.Domain;

namespace LearningCSharp.Exercises.ApplicationComposition.Cli;

public sealed class JsonReadingLogSource : IReadingLogSource
{
    public Task<IReadOnlyList<ReadingEntry>> LoadAsync(string location, CancellationToken cancellationToken)
    {
        // TODO: Implement LoadAsync so it rejects a blank location and a missing
        // file, then reads and deserializes the JSON array asynchronously,
        // treating malformed or empty content as invalid data.
        throw new NotImplementedException("TODO: Read the data file from disk and deserialize it.");
    }
}
