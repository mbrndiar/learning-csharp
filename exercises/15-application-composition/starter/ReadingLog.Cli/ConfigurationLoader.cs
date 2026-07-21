using LearningCSharp.Exercises.ApplicationComposition.Application;

namespace LearningCSharp.Exercises.ApplicationComposition.Cli;

public static class ConfigurationLoader
{
    public static Task<SummaryConfiguration> LoadAsync(
        string configPath,
        bool resolveDataFileAsFilePath = true,
        CancellationToken cancellationToken = default)
    {
        // TODO: Reject a blank config path and a missing file (ConfigurationException).
        // TODO: Read and deserialize the JSON asynchronously; report malformed or empty config as ConfigurationException.
        // TODO: Validate the data file and the minimum rating (1..5).
        // TODO: Only resolve the data file against the config's directory when it names a local file path.
        throw new NotImplementedException("TODO: Load configuration JSON and resolve relative data paths.");
    }
}
