using LearningCSharp.Exercises.ApplicationComposition.Application;

namespace LearningCSharp.Exercises.ApplicationComposition.Cli;

public static class ConfigurationLoader
{
    public static Task<SummaryConfiguration> LoadAsync(
        string configPath,
        bool resolveDataFileAsFilePath = true,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement LoadAsync so it rejects a blank config path and a
        // missing file (ConfigurationException), reads and deserializes the JSON
        // asynchronously while reporting malformed or empty config as
        // ConfigurationException, validates the data file and the minimum rating
        // (1..5), and only resolves the data file against the config's directory
        // when it names a local file path.
        throw new NotImplementedException("TODO: Load configuration JSON and resolve relative data paths.");
    }
}
