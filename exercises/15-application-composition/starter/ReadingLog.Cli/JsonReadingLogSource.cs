using LearningCSharp.Exercises.ApplicationComposition.Application;
using LearningCSharp.Exercises.ApplicationComposition.Domain;

namespace LearningCSharp.Exercises.ApplicationComposition.Cli;

public sealed class JsonReadingLogSource : IReadingLogSource
{
    public Task<IReadOnlyList<ReadingEntry>> LoadAsync(string location, CancellationToken cancellationToken)
    {
        // TODO: Reject a blank location and a missing file.
        // TODO: Read and deserialize the JSON array asynchronously; treat malformed or empty content as invalid data.
        throw new NotImplementedException("TODO: Read the data file from disk and deserialize it.");
    }
}
