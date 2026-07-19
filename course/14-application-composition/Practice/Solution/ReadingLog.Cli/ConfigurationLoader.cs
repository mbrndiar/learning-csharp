using System.Text.Json;
using LearningCSharp.Course.Unit14.Practice.Application;

namespace LearningCSharp.Course.Unit14.Practice.Cli;

public static class ConfigurationLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<SummaryConfiguration> LoadAsync(string configPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);

        if (!File.Exists(configPath))
        {
            throw new ConfigurationException($"Configuration file not found: {configPath}");
        }

        await using FileStream stream = new(configPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        SummaryConfiguration? configuration;

        try
        {
            configuration = await JsonSerializer.DeserializeAsync<SummaryConfiguration>(stream, SerializerOptions, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new ConfigurationException($"Configuration JSON is malformed: {exception.Message}");
        }

        if (configuration is null)
        {
            throw new ConfigurationException("Configuration JSON was empty.");
        }

        if (string.IsNullOrWhiteSpace(configuration.DataFile))
        {
            throw new ConfigurationException("Configuration must include a dataFile value.");
        }

        if (configuration.MinimumRating is < 1 or > 5)
        {
            throw new ConfigurationException("minimumRating must be between 1 and 5.");
        }

        string directory = Path.GetDirectoryName(Path.GetFullPath(configPath)) ?? AppContext.BaseDirectory;
        string resolvedDataFile = Path.IsPathRooted(configuration.DataFile)
            ? configuration.DataFile
            : Path.GetFullPath(Path.Combine(directory, configuration.DataFile));

        return configuration with { DataFile = resolvedDataFile };
    }
}
