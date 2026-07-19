using System.Text.Json;
using LearningCSharp.Course.Unit14.Practice.Application;
using LearningCSharp.Course.Unit14.Practice.Domain;

namespace LearningCSharp.Course.Unit14.Practice.Cli;

public sealed class JsonReadingLogSource : IReadingLogSource
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<IReadOnlyList<ReadingEntry>> LoadAsync(string dataFile, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataFile);

        if (!File.Exists(dataFile))
        {
            throw new FileNotFoundException($"Reading log file not found: {dataFile}", dataFile);
        }

        await using FileStream stream = new(dataFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

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
