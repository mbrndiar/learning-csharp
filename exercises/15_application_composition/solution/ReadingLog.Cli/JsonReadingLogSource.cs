using System.Text.Json;
using LearningCSharp.Exercises.ApplicationComposition.Application;
using LearningCSharp.Exercises.ApplicationComposition.Domain;

namespace LearningCSharp.Exercises.ApplicationComposition.Cli;

public sealed class JsonReadingLogSource : IReadingLogSource
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<IReadOnlyList<ReadingEntry>> LoadAsync(string location, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        if (!File.Exists(location))
        {
            throw new FileNotFoundException($"Reading log file not found: {location}", location);
        }

        await using FileStream stream = new(location, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

        try
        {
            ReadingEntry[]? entries = await JsonSerializer.DeserializeAsync<ReadingEntry[]>(stream, SerializerOptions, cancellationToken);
            return entries ?? throw new InvalidDataException("The reading log JSON was empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The reading log JSON is malformed.", exception);
        }
    }
}
