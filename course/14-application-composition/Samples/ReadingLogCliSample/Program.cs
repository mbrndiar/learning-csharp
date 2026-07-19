using System.Text.Json;

string dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDirectory);
string entriesPath = Path.Combine(dataDirectory, "entries.json");
string configPath = Path.Combine(dataDirectory, "config.json");

ReadingEntry[] entries =
[
    new("Deep Work", 304, 5),
    new("Refactoring", 448, 5),
    new("The Pragmatic Programmer", 352, 4),
];

await File.WriteAllTextAsync(entriesPath, JsonSerializer.Serialize(entries, SampleJson.Options), CancellationToken.None);
await File.WriteAllTextAsync(
    configPath,
    JsonSerializer.Serialize(new SummaryConfiguration { DataFile = "entries.json", MinimumRating = 5 }, SampleJson.Options),
    CancellationToken.None);

var command = new SummaryCommand(new SummaryApplication(new JsonReadingLogSource()));
int exitCode = await command.RunAsync(["summary", configPath], Console.Out, Console.Error, CancellationToken.None);
Console.WriteLine($"Exit code: {exitCode}");

internal static class SampleJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
}

internal sealed record ReadingEntry(string Title, int Pages, int Rating);
internal sealed record ReadingSummary(int TotalBooks, int TotalPages, double AverageRating, IReadOnlyList<string> RecommendedTitles);
internal sealed record SummaryConfiguration
{
    public string DataFile { get; init; } = string.Empty;
    public int MinimumRating { get; init; } = 4;
}

internal sealed record SummaryReport(ReadingSummary Summary, IReadOnlyList<string> OutputLines);

internal interface IReadingLogSource
{
    Task<IReadOnlyList<ReadingEntry>> LoadAsync(string dataFile, CancellationToken cancellationToken);
}

internal sealed class SummaryApplication(IReadingLogSource source)
{
    public async Task<SummaryReport> RunAsync(SummaryConfiguration configuration, CancellationToken cancellationToken)
    {
        IReadOnlyList<ReadingEntry> entries = await source.LoadAsync(configuration.DataFile, cancellationToken);
        ReadingSummary summary = ReadingSummaryCalculator.Create(entries, configuration.MinimumRating);
        string recommended = summary.RecommendedTitles.Count == 0 ? "(none)" : string.Join(", ", summary.RecommendedTitles);

        return new SummaryReport(
            summary,
            [
                $"Total books: {summary.TotalBooks}",
                $"Total pages: {summary.TotalPages}",
                $"Average rating: {summary.AverageRating:F2}",
                $"Recommended: {recommended}",
            ]);
    }
}

internal static class ReadingSummaryCalculator
{
    public static ReadingSummary Create(IEnumerable<ReadingEntry> entries, int minimumRating)
    {
        ReadingEntry[] materialized = entries.ToArray();
        int totalPages = materialized.Sum(entry => entry.Pages);
        double averageRating = materialized.Average(entry => entry.Rating);
        IReadOnlyList<string> recommended = materialized
            .Where(entry => entry.Rating >= minimumRating)
            .OrderByDescending(entry => entry.Rating)
            .ThenBy(entry => entry.Title, StringComparer.Ordinal)
            .Select(entry => entry.Title)
            .ToArray();

        return new ReadingSummary(materialized.Length, totalPages, Math.Round(averageRating, 2, MidpointRounding.AwayFromZero), recommended);
    }
}

internal sealed class JsonReadingLogSource : IReadingLogSource
{
    public async Task<IReadOnlyList<ReadingEntry>> LoadAsync(string dataFile, CancellationToken cancellationToken)
    {
        await using FileStream stream = new(dataFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        ReadingEntry[]? entries = await JsonSerializer.DeserializeAsync<ReadingEntry[]>(stream, SampleJson.Options, cancellationToken);
        return entries ?? throw new InvalidDataException("The sample reading log was empty.");
    }
}

internal sealed class SummaryCommand(SummaryApplication application)
{
    public async Task<int> RunAsync(string[] args, TextWriter stdout, TextWriter stderr, CancellationToken cancellationToken)
    {
        if (args.Length != 2 || !string.Equals(args[0], "summary", StringComparison.OrdinalIgnoreCase))
        {
            await stderr.WriteLineAsync("Usage: summary <config-path>");
            return 2;
        }

        SummaryConfiguration configuration = await LoadConfigurationAsync(args[1], cancellationToken);
        SummaryReport report = await application.RunAsync(configuration, cancellationToken);

        foreach (string line in report.OutputLines)
        {
            await stdout.WriteLineAsync(line);
        }

        return 0;
    }

    private static async Task<SummaryConfiguration> LoadConfigurationAsync(string configPath, CancellationToken cancellationToken)
    {
        await using FileStream stream = new(configPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        SummaryConfiguration? configuration = await JsonSerializer.DeserializeAsync<SummaryConfiguration>(stream, SampleJson.Options, cancellationToken);
        string directory = Path.GetDirectoryName(configPath) ?? AppContext.BaseDirectory;
        return configuration is null
            ? throw new InvalidDataException("The sample configuration was empty.")
            : configuration with { DataFile = Path.GetFullPath(Path.Combine(directory, configuration.DataFile)) };
    }
}
