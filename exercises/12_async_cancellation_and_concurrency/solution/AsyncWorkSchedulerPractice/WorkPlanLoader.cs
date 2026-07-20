using System.Text.Json;

namespace LearningCSharp.Course.Unit12.Practice;

public static class WorkPlanLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<IReadOnlyList<WorkItem>> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

        try
        {
            WorkItem[]? items = await JsonSerializer.DeserializeAsync<WorkItem[]>(stream, SerializerOptions, cancellationToken);
            return items ?? throw new InvalidDataException("The work plan did not contain any items.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The work plan JSON is malformed.", exception);
        }
    }
}
