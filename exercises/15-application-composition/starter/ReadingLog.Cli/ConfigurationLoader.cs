using LearningCSharp.Exercises.ApplicationComposition.Application;

namespace LearningCSharp.Exercises.ApplicationComposition.Cli;

public static class ConfigurationLoader
{
    public static Task<SummaryConfiguration> LoadAsync(
        string configPath,
        bool resolveDataFileAsFilePath = true,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TODO: Load configuration JSON and resolve relative data paths.");
    }
}
