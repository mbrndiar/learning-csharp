using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

// Composition root #1: a file adapter. Domain/Application code below never
// changes between this run and the HTTP run further down - only the
// composition root and the concrete IReadingLogSource differ.
Console.WriteLine("--- File adapter ---");
var fileCommand = new SummaryCommand(new SummaryApplication(new JsonReadingLogSource()));
int fileExitCode = await fileCommand.RunAsync(["summary", configPath], Console.Out, Console.Error, CancellationToken.None);
Console.WriteLine($"Exit code: {fileExitCode}");

// Composition root #2: the same contract, served over loopback HTTP instead
// of a local file, and consumed through an HttpClient-based adapter. This is
// the "server/client adapter" half of this lesson's composition-root
// objective, and it directly reuses Lesson 14's Minimal API + HttpClient ideas.
Console.WriteLine();
Console.WriteLine("--- HTTP adapter ---");
await using WebApplication entriesServer = BuildEntriesServer(entries);
await entriesServer.StartAsync();
try
{
    string baseAddress = GetServerAddress(entriesServer);
    using HttpClient httpClient = new() { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromSeconds(5) };
    var httpCommand = new SummaryCommand(
        new SummaryApplication(new HttpReadingLogSource(httpClient)),
        resolveDataFileAsFilePath: false);

    // The config file's dataFile value now means "/entries", a resource path
    // on the server above, not a local file path - the composition root
    // (via resolveDataFileAsFilePath: false) is what tells ConfigurationLoader
    // to stop treating it as one.
    string httpConfigPath = Path.Combine(dataDirectory, "config.http.json");
    await File.WriteAllTextAsync(
        httpConfigPath,
        JsonSerializer.Serialize(new SummaryConfiguration { DataFile = "/entries", MinimumRating = 5 }, SampleJson.Options),
        CancellationToken.None);

    int httpExitCode = await httpCommand.RunAsync(["summary", httpConfigPath], Console.Out, Console.Error, CancellationToken.None);
    Console.WriteLine($"Exit code: {httpExitCode}");
}
finally
{
    await entriesServer.StopAsync();
}

static WebApplication BuildEntriesServer(ReadingEntry[] entries)
{
    var builder = WebApplication.CreateBuilder();
    builder.Logging.ClearProviders();
    builder.WebHost.UseUrls("http://127.0.0.1:0");

    WebApplication app = builder.Build();
    app.MapGet("/entries", () => Results.Ok(entries));
    return app;
}

static string GetServerAddress(WebApplication app)
{
    IServer server = app.Services.GetRequiredService<IServer>();
    IServerAddressesFeature? addressesFeature = server.Features.Get<IServerAddressesFeature>();
    return addressesFeature?.Addresses.Single()
        ?? throw new InvalidOperationException("The sample entries server did not expose an address.");
}

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

/// <summary>
/// Application code depends only on this abstraction. <c>location</c> means a
/// file path for <see cref="JsonReadingLogSource"/> and a resource path for
/// <see cref="HttpReadingLogSource"/> - the composition root below is the only
/// code that knows which adapter, and therefore which meaning, is active.
/// </summary>
internal interface IReadingLogSource
{
    Task<IReadOnlyList<ReadingEntry>> LoadAsync(string location, CancellationToken cancellationToken);
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
    public async Task<IReadOnlyList<ReadingEntry>> LoadAsync(string location, CancellationToken cancellationToken)
    {
        await using FileStream stream = new(location, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        ReadingEntry[]? entries = await JsonSerializer.DeserializeAsync<ReadingEntry[]>(stream, SampleJson.Options, cancellationToken);
        return entries ?? throw new InvalidDataException("The sample reading log was empty.");
    }
}

/// <summary>
/// The server/client adapter counterpart to <see cref="JsonReadingLogSource"/>.
/// Only this class references <see cref="HttpClient"/>.
/// </summary>
internal sealed class HttpReadingLogSource(HttpClient httpClient) : IReadingLogSource
{
    public async Task<IReadOnlyList<ReadingEntry>> LoadAsync(string location, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(location, cancellationToken);
        response.EnsureSuccessStatusCode();

        ReadingEntry[]? entries = await response.Content.ReadFromJsonAsync<ReadingEntry[]>(SampleJson.Options, cancellationToken);
        return entries ?? throw new InvalidDataException("The sample entries server returned an empty response.");
    }
}

internal sealed class SummaryCommand(SummaryApplication application, bool resolveDataFileAsFilePath = true)
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

    private async Task<SummaryConfiguration> LoadConfigurationAsync(string configPath, CancellationToken cancellationToken)
    {
        await using FileStream stream = new(configPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        SummaryConfiguration? configuration = await JsonSerializer.DeserializeAsync<SummaryConfiguration>(stream, SampleJson.Options, cancellationToken);
        if (configuration is null)
        {
            throw new InvalidDataException("The sample configuration was empty.");
        }

        if (!resolveDataFileAsFilePath)
        {
            return configuration;
        }

        string directory = Path.GetDirectoryName(configPath) ?? AppContext.BaseDirectory;
        return configuration with { DataFile = Path.GetFullPath(Path.Combine(directory, configuration.DataFile)) };
    }
}
