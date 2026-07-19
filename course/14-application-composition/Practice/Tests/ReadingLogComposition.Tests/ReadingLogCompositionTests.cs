using System.Text.Json;
using LearningCSharp.Course.Unit14.Practice.Application;
using LearningCSharp.Course.Unit14.Practice.Cli;
using LearningCSharp.Course.Unit14.Practice.Domain;

namespace LearningCSharp.Course.Unit14.Practice.Tests;

public sealed class ReadingLogCompositionTests
{
    [Fact]
    public void ReadingSummaryCalculatorCreatesAPureSummary()
    {
        ReadingSummary summary = ReadingSummaryCalculator.Create(
        [
            new ReadingEntry("Deep Work", 304, 5),
            new ReadingEntry("The Pragmatic Programmer", 352, 4),
            new ReadingEntry("Clean Architecture", 432, 3),
        ],
        minimumRating: 4);

        Assert.Equal(3, summary.TotalBooks);
        Assert.Equal(1088, summary.TotalPages);
        Assert.Equal(4, summary.AverageRating);
        Assert.Equal(["Deep Work", "The Pragmatic Programmer"], summary.RecommendedTitles);
    }

    [Fact]
    public async Task SummaryCommandWritesStdoutAndReturnsZeroForSuccess()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string workspace = CreateWorkspace();
        string configPath = await CreateConfigAndDataAsync(workspace, cancellationToken);
        var command = new SummaryCommand(new SummaryApplication(new JsonReadingLogSource()));
        using StringWriter stdout = new();
        using StringWriter stderr = new();

        int exitCode = await command.RunAsync(["summary", configPath], stdout, stderr, cancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Contains("Total books: 3", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("Recommended: Deep Work, Refactoring", stdout.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public async Task SummaryCommandReturnsTwoForUsageErrors()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var command = new SummaryCommand(new SummaryApplication(new JsonReadingLogSource()));
        using StringWriter stdout = new();
        using StringWriter stderr = new();

        int exitCode = await command.RunAsync([], stdout, stderr, cancellationToken);

        Assert.Equal(2, exitCode);
        Assert.Contains("Usage: summary <config-path>", stderr.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, stdout.ToString());
    }

    [Fact]
    public async Task SummaryCommandReturnsTwoForMissingConfiguration()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var command = new SummaryCommand(new SummaryApplication(new JsonReadingLogSource()));
        using StringWriter stdout = new();
        using StringWriter stderr = new();

        int exitCode = await command.RunAsync(["summary", "does-not-exist.json"], stdout, stderr, cancellationToken);

        Assert.Equal(2, exitCode);
        Assert.Contains("Configuration file not found", stderr.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, stdout.ToString());
    }

    [Fact]
    public async Task SummaryCommandReturnsThreeForMalformedData()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string workspace = CreateWorkspace();
        string entriesPath = Path.Combine(workspace, "entries.json");
        string configPath = Path.Combine(workspace, "config.json");
        await File.WriteAllTextAsync(entriesPath, "{ not valid json", cancellationToken);
        await File.WriteAllTextAsync(
            configPath,
            JsonSerializer.Serialize(new SummaryConfiguration { DataFile = "entries.json", MinimumRating = 4 }),
            cancellationToken);

        var command = new SummaryCommand(new SummaryApplication(new JsonReadingLogSource()));
        using StringWriter stdout = new();
        using StringWriter stderr = new();

        int exitCode = await command.RunAsync(["summary", configPath], stdout, stderr, cancellationToken);

        Assert.Equal(3, exitCode);
        Assert.Contains("malformed", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, stdout.ToString());
    }

    private static async Task<string> CreateConfigAndDataAsync(string workspace, CancellationToken cancellationToken)
    {
        string entriesPath = Path.Combine(workspace, "entries.json");
        string configPath = Path.Combine(workspace, "config.json");

        ReadingEntry[] entries =
        [
            new("Deep Work", 304, 5),
            new("Refactoring", 448, 5),
            new("Clean Architecture", 432, 3),
        ];

        await File.WriteAllTextAsync(entriesPath, JsonSerializer.Serialize(entries), cancellationToken);
        await File.WriteAllTextAsync(
            configPath,
            JsonSerializer.Serialize(new SummaryConfiguration { DataFile = "entries.json", MinimumRating = 5 }),
            cancellationToken);

        return configPath;
    }

    private static string CreateWorkspace()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "generated", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
